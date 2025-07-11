﻿using ItemFilterLibraryDatabase.Api.Models;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ItemFilterLibraryDatabase.Api;

public class ApiClient : IDisposable
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
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

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        DebugLog($"ApiClient initialized with base URL: {_baseUrl}");

        if (ItemFilterLibraryDatabase.Main.Settings.HasValidAccessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ItemFilterLibraryDatabase.Main.Settings.AccessToken);
            DebugLog("Set initial auth header");
        }
    }

    public bool IsInitialized { get; private set; }

    public void Dispose()
    {
        if (_disposed) return;
        DebugLog("Disposing ApiClient");
        _httpClient?.Dispose();
        _refreshLock?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            DebugLog("Starting ApiClient initialization");

            if (ItemFilterLibraryDatabase.Main.Settings.HasValidAccessToken)
            {
                DebugLog("Valid access token found, testing authentication");
                var isValid = await TestAuthAsync();
                if (isValid)
                {
                    IsInitialized = true;
                    DebugLog("Access token validated successfully");
                    return true;
                }

                DebugLog("Access token failed validation");
            }
            else
            {
                DebugLog("No valid access token found");
            }

            if (ItemFilterLibraryDatabase.Main.Settings.HasValidRefreshToken)
            {
                DebugLog("Valid refresh token found, attempting to refresh access token");
                var refreshSuccess = await RefreshTokenAsync();
                if (refreshSuccess)
                {
                    IsInitialized = true;
                    DebugLog("Successfully refreshed token and initialized");
                    return true;
                }

                DebugLog("Failed to refresh token");
            }
            else
            {
                DebugLog("No valid refresh token found");
            }

            DebugLog("All authentication attempts failed");
            IsInitialized = false;
            return false;
        }
        catch (Exception ex)
        {
            DebugLog($"Initialization failed with exception: {ex}");
            IsInitialized = false;
            return false;
        }
    }

    public async Task LoginAsync(string encodedAuthData)
    {
        DebugLog("Starting LoginAsync");
        try
        {
            DebugLog("Decoding auth data");
            var jsonString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedAuthData));
            var authData = JsonSerializer.Deserialize<AuthData>(jsonString);

            if (string.IsNullOrEmpty(authData?.Tokens?.Access?.Token) || string.IsNullOrEmpty(authData?.Tokens?.Refresh?.Token))
            {
                DebugLog("Invalid token data - tokens are null or empty");
                throw new ApiException("Invalid token data");
            }

            DebugLog("Storing auth data");
            StoreAuthData(authData);

            DebugLog("Updating HTTP client auth header");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ItemFilterLibraryDatabase.Main.Settings.AccessToken);

            DebugLog("Testing authentication");
            var isValid = await TestAuthAsync();
            if (!isValid)
            {
                DebugLog("Authentication test failed");
                throw new ApiException("Failed to validate authentication");
            }

            IsInitialized = true;
            DebugLog("Login completed successfully");
        }
        catch (Exception ex)
        {
            DebugLog($"Login failed: {ex}");
            ClearAllTokens();
            IsInitialized = false;
            throw new ApiException("Login failed", ex);
        }
    }

    private async Task<bool> TestAuthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            DebugLog("Testing authentication status");
            var response = await GetAsync<TestAuthApiResponse>(Routes.Auth.Test, cancellationToken);

            if (response?.Data == null)
            {
                DebugLog("Test auth response was null");
                return false;
            }

            if (response.Data.User != null)
            {
                ItemFilterLibraryDatabase.Main.Settings.UserId = response.Data.User.Id;
                ItemFilterLibraryDatabase.Main.Settings.IsAdmin = response.Data.User.IsAdmin;
                DebugLog($"Updated user info - ID: {response.Data.User.Id}");
            }

            var templateTypesResponse = await GetAsync<TemplateTypesApiResponse>(Routes.Templates.GetTypes, cancellationToken);

            if (templateTypesResponse?.Data != null)
            {
                ItemFilterLibraryDatabase.Main.Settings.AvailableTemplateTypes = templateTypesResponse.Data;

                if (ItemFilterLibraryDatabase.Main.Settings.CurrentTemplateType == null && templateTypesResponse.Data.Any())
                {
                    ItemFilterLibraryDatabase.Main.Settings.CurrentTemplateType = templateTypesResponse.Data[0];
                }

                DebugLog($"Updated template types - Count: {templateTypesResponse.Data.Count}");
            }

            return response.Data.Status == "connected";
        }
        catch (Exception ex)
        {
            DebugLog($"Auth test failed due to possible connection issue: {ex}");
            return false;
        }
    }

    public async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        DebugLog($"GET request to: {endpoint}");
        return await SendRequestAsync<T>(HttpMethod.Get, endpoint, null, cancellationToken);
    }

    public async Task<T> PostAsync<T>(string endpoint, object data = null, CancellationToken cancellationToken = default)
    {
        DebugLog($"POST request to: {endpoint}");
        return await SendRequestAsync<T>(HttpMethod.Post, endpoint, data, cancellationToken);
    }

    public async Task<T> PutAsync<T>(string endpoint, object data = null, CancellationToken cancellationToken = default)
    {
        DebugLog($"PUT request to: {endpoint}");
        return await SendRequestAsync<T>(HttpMethod.Put, endpoint, data, cancellationToken);
    }

    public async Task<T> DeleteAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        DebugLog($"DELETE request to: {endpoint}");
        return await SendRequestAsync<T>(HttpMethod.Delete, endpoint, null, cancellationToken);
    }

    private async Task<T> SendRequestAsync<T>(HttpMethod method, string endpoint, object data = null, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            DebugLog("Attempted to send request with disposed client");
            throw new ObjectDisposedException(nameof(ApiClient));
        }

        try
        {
            var response = await ExecuteRequestAsync(method, endpoint, data, false, cancellationToken);
            DebugLog($"Received response with status code: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var result = await DeserializeResponseAsync<T>(response, cancellationToken);
                DebugLog("Successfully deserialized response");
                return result;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var error = await DeserializeResponseAsync<ApiErrorResponse>(response, cancellationToken);

                switch (error?.Error?.Code)
                {
                    case ErrorCodes.AUTH_REQUIRED:
                        DebugLog("Received AUTH_REQUIRED, attempting refresh");
                        if (await RefreshTokenAsync(cancellationToken))
                        {
                            response = await ExecuteRequestAsync(method, endpoint, data, true, cancellationToken);
                            if (response.IsSuccessStatusCode)
                            {
                                return await DeserializeResponseAsync<T>(response, cancellationToken);
                            }
                        }
                        break;

                    case ErrorCodes.AUTH_REFRESH_ERROR:
                        DebugLog("Received AUTH_REFRESH_ERROR, clearing all tokens");
                        ClearAllTokens();
                        break;

                    default:
                        DebugLog($"Received unknown auth error: {error?.Error?.Code} - not clearing tokens");
                        break;
                }
            }

            throw await CreateExceptionFromResponseAsync(response, cancellationToken);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            DebugLog($"Request failed with unexpected error: {ex}");
            throw new ApiException($"Request failed: {endpoint}", ex);
        }
    }
    private async Task<HttpResponseMessage> ExecuteRequestAsync(HttpMethod method, string endpoint, object data, bool isRetry, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        if (data != null)
        {
            var jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
            DebugLog($"Request body: {jsonContent}");

            if (jsonContent.Length > 1024)
            {
                var compressedContent = CompressContent(jsonContent);
                var content = new ByteArrayContent(compressedContent);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Headers.ContentEncoding.Add("gzip");
                request.Content = content;
            }
            else
            {
                request.Content = JsonContent.Create(data, options: _jsonOptions);
            }
        }

        try
        {
            var attempt = isRetry
                ? "retry"
                : "initial";

            DebugLog($"Sending {attempt} request to {endpoint}");
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            DebugLog($"HTTP request failed for {endpoint}: {ex}");
            throw new ApiException($"Failed to {(isRetry ? "retry" : "send")} request: {endpoint}", ex);
        }
    }

    private async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!ItemFilterLibraryDatabase.Main.Settings.HasValidRefreshToken)
        {
            DebugLog("No valid refresh token available");
            return false;
        }

        try
        {
            return await Task.Run(async () =>
            {
                await _refreshLock.WaitAsync(cancellationToken);
                try
                {
                    var currentAuth = _httpClient.DefaultRequestHeaders.Authorization;
                    _httpClient.DefaultRequestHeaders.Authorization = null;

                    while (true)
                    {
                        try
                        {
                            DebugLog("Sending refresh token request");
                            using var request = new HttpRequestMessage(HttpMethod.Post, Routes.Auth.Refresh);
                            request.Content = JsonContent.Create(new { refresh_token = ItemFilterLibraryDatabase.Main.Settings.RefreshToken },
                                options: new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                            using var response = await _httpClient.SendAsync(request, cancellationToken);

                            if (response.IsSuccessStatusCode)
                            {
                                var content = await GetResponseContentAsync(response, cancellationToken);
                                var refreshResponse = JsonSerializer.Deserialize<ApiResponse<AuthData>>(content);

                                if (refreshResponse?.Data == null)
                                {
                                    DebugLog("Refresh response contained no data");
                                    return false;
                                }

                                DebugLog("Storing refreshed auth data");
                                StoreAuthData(refreshResponse.Data);
                                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ItemFilterLibraryDatabase.Main.Settings.AccessToken);
                                DebugLog("Token refresh completed successfully");
                                return true;
                            }

                            if (response.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                var error = await DeserializeResponseAsync<ApiErrorResponse>(response, cancellationToken);
                                if (error?.Error?.Code == ErrorCodes.AUTH_REFRESH_ERROR)
                                {
                                    DebugLog("Received explicit AUTH_REFRESH_ERROR, clearing tokens");
                                    ClearAllTokens();
                                    return false;
                                }

                                DebugLog($"Received auth error: {error?.Error?.Code} - not clearing tokens");
                                return false;
                            }

                            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                if (response.Headers.TryGetValues("retry-after", out var retryValues) &&
                                    int.TryParse(retryValues.FirstOrDefault(), out var retrySeconds))
                                {
                                    retrySeconds += 1;
                                    DebugLog($"Rate limit hit. Waiting {retrySeconds} seconds before retry");

                                    try
                                    {
                                        await Task.Delay(TimeSpan.FromSeconds(retrySeconds), cancellationToken);
                                        continue;
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        DebugLog("Refresh token retry was cancelled");
                                        return false;
                                    }
                                }
                            }

                            DebugLog($"Refresh failed with status {response.StatusCode} - not clearing tokens");
                            return false;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            DebugLog($"Token refresh failed due to possible connection issue: {ex}");
                            _httpClient.DefaultRequestHeaders.Authorization = currentAuth;
                            return false;
                        }
                    }
                }
                finally
                {
                    _refreshLock.Release();
                    DebugLog("Released refresh lock");
                }
            },
            cancellationToken);
        }
        catch (OperationCanceledException)
        {
            DebugLog("Token refresh operation was cancelled");
            return false;
        }
    }
    private void StoreAuthData(AuthData authData)
    {
        DebugLog("Starting to store new auth data");

        ItemFilterLibraryDatabase.Main.Settings.AccessToken = authData.Tokens?.Access?.Token ?? string.Empty;
        DebugLog($"Access Token: {ItemFilterLibraryDatabase.Main.Settings.AccessToken}");

        ItemFilterLibraryDatabase.Main.Settings.RefreshToken = authData.Tokens?.Refresh?.Token ?? string.Empty;
        DebugLog($"Refresh Token: {ItemFilterLibraryDatabase.Main.Settings.RefreshToken}");

        var accessExpiry = authData.Tokens?.Access?.ExpiresAt ?? 0;
        ItemFilterLibraryDatabase.Main.Settings.AccessTokenExpiry = accessExpiry;
        DebugLog($"Access Token Expiry: {DateTimeOffset.FromUnixTimeSeconds(accessExpiry).LocalDateTime}");

        var refreshExpiry = authData.Tokens?.Refresh?.ExpiresAt ?? 0;
        ItemFilterLibraryDatabase.Main.Settings.RefreshTokenExpiry = refreshExpiry;
        DebugLog($"Refresh Token Expiry: {DateTimeOffset.FromUnixTimeSeconds(refreshExpiry).LocalDateTime}");

        ItemFilterLibraryDatabase.Main.Settings.UserId = authData.User?.Id ?? string.Empty;
        DebugLog($"User ID: {ItemFilterLibraryDatabase.Main.Settings.UserId}");

        ItemFilterLibraryDatabase.Main.Settings.IsAdmin = false;

        DebugLog("Auth data storage completed");
    }

    private void ClearAccessToken()
    {
        DebugLog("Clearing access token");
        ItemFilterLibraryDatabase.Main.Settings.AccessToken = string.Empty;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        IsInitialized = false;
    }

    private void ClearAllTokens()
    {
        DebugLog("Clearing all tokens");
        ItemFilterLibraryDatabase.Main.Settings.ClearTokens();
        _httpClient.DefaultRequestHeaders.Authorization = null;
        IsInitialized = false;
    }

    private async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var content = await GetResponseContentAsync(response, cancellationToken);
            DebugLog($"Raw response content: {content}");
            return JsonSerializer.Deserialize<T>(content);
        }
        catch (JsonException ex)
        {
            DebugLog($"Failed to deserialize response: {ex}");
            throw new ApiException("Failed to deserialize response", ex);
        }
    }

    private async Task<ApiException> CreateExceptionFromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var error = await DeserializeResponseAsync<ApiErrorResponse>(response, cancellationToken);
            var message = error?.Error?.Message ?? $"Request failed with status code {response.StatusCode}";
            var code = error?.Error?.Code;
            DebugLog($"Created exception from response - Message: {message}, Code: {code}");
            return new ApiException(message, code);
        }
        catch (Exception ex)
        {
            DebugLog($"Failed to parse error response: {ex}");
            return new ApiException($"Request failed with status code {response.StatusCode}");
        }
    }

    private async Task<string> GetResponseContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentEncoding = response.Content.Headers.ContentEncoding;

        if (contentEncoding.Contains("gzip"))
        {
            using var compressedStream = new MemoryStream(contentBytes);
            using var resultStream = new MemoryStream();
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);

            await gzipStream.CopyToAsync(resultStream, cancellationToken);

            var decompressedBytes = resultStream.ToArray();
            DebugLog($"Decompressed response: {contentBytes.Length:N0} -> {decompressedBytes.Length:N0} bytes");
            return Encoding.UTF8.GetString(decompressedBytes);
        }

        return Encoding.UTF8.GetString(contentBytes);
    }

    public async Task<T> PatchAsync<T>(string endpoint, object data = null, CancellationToken cancellationToken = default)
    {
        DebugLog($"PATCH request to: {endpoint}");
        return await SendRequestAsync<T>(HttpMethod.Patch, endpoint, data, cancellationToken);
    }

    private byte[] CompressContent(string content)
    {
        var contentBytes = Encoding.UTF8.GetBytes(content);

        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
        {
            gzipStream.Write(contentBytes, 0, contentBytes.Length);
        }

        var compressedBytes = memoryStream.ToArray();
        DebugLog($"Compressed request: {contentBytes.Length:N0} -> {compressedBytes.Length:N0} bytes " + $"({(float)compressedBytes.Length / contentBytes.Length:P1})");

        return compressedBytes;
    }

    private void DebugLog(string message)
    {
        if (ItemFilterLibraryDatabase.Main.Settings.Debug)
        {
            ItemFilterLibraryDatabase.Main.LogMessage($"[ApiClient] {message}");
        }
    }
}