using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static ItemFilterLibraryDatabase.Api.AuthData;

namespace ItemFilterLibraryDatabase.Api;

public class ApiClient : IDisposable
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _disposed;

    public ApiClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Set auth header if we have a valid token
        if (ItemFilterLibraryDatabase.Main.Settings.HasValidAccessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ItemFilterLibraryDatabase.Main.Settings.AccessToken);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _httpClient?.Dispose();
        _refreshLock?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    public async Task LoginAsync(string encodedAuthData)
    {
        try
        {
            var authData = DecodeAuthData(encodedAuthData);
            await ValidateAndSetTokens(authData);
        }
        catch (Exception ex)
        {
            throw new ApiException("Failed to process login data", ex);
        }
    }

    private AuthData DecodeAuthData(string encodedData)
    {
        try
        {
            var jsonString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedData));
            return JsonSerializer.Deserialize<AuthData>(jsonString);
        }
        catch (Exception ex)
        {
            throw new ApiException("Invalid auth data format", ex);
        }
    }

    public async Task<bool> ValidateTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!ItemFilterLibraryDatabase.Main.Settings.HasValidAccessToken)
        {
            return false;
        }

        try
        {
            // Try to fetch user profile as validation
            var response = await GetAsync<ApiResponse<UserInfo>>(Routes.Users.GetUser(ItemFilterLibraryDatabase.Main.Settings.UserId), cancellationToken);

            // Update user info if successful
            if (response?.Data != null)
            {
                ItemFilterLibraryDatabase.Main.Settings.IsAdmin = response.Data.IsAdmin;
                return true;
            }

            return false;
        }
        catch (ApiAuthenticationException)
        {
            // If validation fails, try refresh
            try
            {
                return await RefreshTokenAsync(cancellationToken);
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private async Task ValidateAndSetTokens(AuthData authData)
    {
        if (string.IsNullOrEmpty(authData?.Tokens?.Access?.Token) || string.IsNullOrEmpty(authData?.Tokens?.Refresh?.Token))
        {
            throw new ApiException("Invalid token data");
        }

        // Store tokens in settings
        ItemFilterLibraryDatabase.Main.Settings.AccessToken = authData.Tokens.Access.Token;
        ItemFilterLibraryDatabase.Main.Settings.RefreshToken = authData.Tokens.Refresh.Token;
        ItemFilterLibraryDatabase.Main.Settings.AccessTokenExpiry = authData.Tokens.Access.ExpiresAt;
        ItemFilterLibraryDatabase.Main.Settings.RefreshTokenExpiry = authData.Tokens.Refresh.ExpiresAt;
        ItemFilterLibraryDatabase.Main.Settings.UserId = authData.User.Id;
        ItemFilterLibraryDatabase.Main.Settings.IsAdmin = authData.User.IsAdmin;

        // Update HTTP client auth header
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authData.Tokens.Access.Type, ItemFilterLibraryDatabase.Main.Settings.AccessToken);
    }

    public async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default) => await SendRequestAsync<T>(HttpMethod.Get, endpoint, null, cancellationToken);

    public async Task<T> PostAsync<T>(string endpoint, object data = null, CancellationToken cancellationToken = default) =>
        await SendRequestAsync<T>(HttpMethod.Post, endpoint, data, cancellationToken);

    public async Task<T> PutAsync<T>(string endpoint, object data = null, CancellationToken cancellationToken = default) =>
        await SendRequestAsync<T>(HttpMethod.Put, endpoint, data, cancellationToken);

    public async Task<T> DeleteAsync<T>(string endpoint, CancellationToken cancellationToken = default) => await SendRequestAsync<T>(HttpMethod.Delete, endpoint, null, cancellationToken);

    private async Task<T> SendRequestAsync<T>(HttpMethod method, string endpoint, object data = null, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ApiClient));

        // Check if token needs refresh based on settings
        if (ItemFilterLibraryDatabase.Main.Settings.HasValidAccessToken &&
            DateTimeOffset.FromUnixTimeSeconds(ItemFilterLibraryDatabase.Main.Settings.AccessTokenExpiry).AddMinutes(-5) <= DateTimeOffset.UtcNow)
        {
            await RefreshTokenAsync(cancellationToken);
        }

        try
        {
            using var request = CreateRequest(method, endpoint, data);
            using var response = await SendWithRetryAsync(request, cancellationToken);
            return await HandleResponseAsync<T>(response);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException($"Request failed: {endpoint}", ex);
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, object data)
    {
        var request = new HttpRequestMessage(method, endpoint);

        if (data != null)
        {
            request.Content = JsonContent.Create(data,
                options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
        }

        return request;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var retryCount = 0;

        while (true)
        {
            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                var error = await DeserializeErrorResponse(response);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        switch (error?.Code)
                        {
                            case ErrorCodes.AuthInvalidToken:
                                // If it's the first retry and token refresh succeeds, retry the request
                                if (retryCount == 0 && await RefreshTokenAsync(cancellationToken))
                                {
                                    retryCount++;
                                    // Update the request with the new token
                                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ItemFilterLibraryDatabase.Main.Settings.AccessToken);
                                    continue;
                                }

                                throw new ApiAuthenticationException("Invalid token", error.Message);

                            case ErrorCodes.AuthError when error.Message.Contains("Invalid or expired refresh token"):
                                ItemFilterLibraryDatabase.Main.Settings.ClearTokens();
                                throw new ApiAuthenticationException("Token invalidated by server", error.Message);

                            case ErrorCodes.AuthRequired:
                                throw new ApiAuthenticationException("Authentication required", error.Message);

                            default:
                                throw new ApiAuthenticationException(error?.Message ?? "Authentication failed");
                        }

                    case HttpStatusCode.TooManyRequests:
                        if (retryCount < maxRetries)
                        {
                            await Task.Delay(GetRetryDelay(retryCount), cancellationToken);
                            retryCount++;
                            continue;
                        }

                        throw new ApiRateLimitException("Rate limit exceeded");

                    default:
                        throw new ApiException(error?.Message ?? $"Request failed with status {response.StatusCode}", error?.Code);
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                await Task.Delay(GetRetryDelay(retryCount), cancellationToken);
            }
        }
    }

    private async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        if (!ItemFilterLibraryDatabase.Main.Settings.HasValidRefreshToken) return false;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Double check if we still need refresh after acquiring lock
            if (!ItemFilterLibraryDatabase.Main.Settings.HasValidAccessToken || !ItemFilterLibraryDatabase.Main.Settings.HasValidRefreshToken)
            {
                return false;
            }

            try
            {
                var response = await PostAsync<ApiResponse<AuthData>>(Routes.Auth.Refresh,
                    Routes.Auth.Requests.CreateRefreshRequest(ItemFilterLibraryDatabase.Main.Settings.RefreshToken),
                    cancellationToken);

                await ValidateAndSetTokens(response.Data);
                return true;
            }
            catch (ApiAuthenticationException authEx) when (authEx.Message.Contains("Invalid or expired refresh token"))
            {
                // Token has been invalidated
                ItemFilterLibraryDatabase.Main.Settings.ClearTokens();
                _httpClient.DefaultRequestHeaders.Authorization = null;
                throw new ApiAuthenticationException("Session expired - new login required");
            }
            catch (Exception)
            {
                ItemFilterLibraryDatabase.Main.Settings.ClearTokens();
                _httpClient.DefaultRequestHeaders.Authorization = null;
                return false;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadFromJsonAsync<T>();
        return content;
    }

    private TimeSpan GetRetryDelay(int retryCount) => TimeSpan.FromSeconds(Math.Pow(2, retryCount));

    private async Task<ErrorResponse> DeserializeErrorResponse(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return error?.Error ?? new ErrorResponse {Message = "Unknown error occurred"};
        }
        catch
        {
            return new ErrorResponse {Message = "Failed to parse error response"};
        }
    }

    private class ApiErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorResponse Error { get; }
    }

    private class ErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; }

        [JsonPropertyName("status")]
        public int Status { get; set; }
    }
}