using ExileCore;
using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.Areas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace ItemFilterLibraryDatabase;

public class ItemFilterLibraryDatabase : BaseSettingsPlugin<ItemFilterLibraryDatabaseSettings>
{
    private const string API_URL = "https://itemfilterlib.squirrelguff.xyz";
    private readonly List<IArea> _areas = [];
    private ApiClient _apiClient;
    private bool _isAuthenticated;
    private bool _isInitialized = false;
    private string _newAuthToken = string.Empty;
    private int _previousAreaIndex = -1;
    private int _selectedAreaIndex;
    private string _statusMessage = string.Empty;

    public bool IsLoading { get; set; }

    public static ItemFilterLibraryDatabase Main { get; private set; }

    public override bool Initialise()
    {
        Main = this;
        Name = "Item Filter Library Database";
        _apiClient = new ApiClient(API_URL);

        // Don't initialize areas yet - wait for auth check
        _ = InitializePluginAsync();

        return true;
    }

    private async Task InitializePluginAsync()
    {
        if (_isInitialized) return;

        try
        {
            // Validate authentication first
            await ValidateExistingAuthAsync();

            // Now initialize areas
            _areas.Clear(); // Clear any existing areas
            _areas.Add(new PublicTemplatesArea(this, _apiClient));
            _areas.Add(new MyTemplatesArea(this, _apiClient));

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize plugin: {ex}", 30f);
            _statusMessage = "Failed to initialize plugin. Please try restarting.";
        }
    }

    public override void DrawSettings()
    {
        DrawAuthenticationSection();

        ImGui.Separator();

        if (!_isInitialized)
        {
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "Initializing plugin...");
            return;
        }

        if (_isAuthenticated)
        {
            DrawAreaTabs();
        }
    }

    private string GetTemplateTypeDisplayName(string type) => type switch
    {
        Routes.Types.ItemFilterLibrary => "Item Filter Library",
        Routes.Types.WheresMyCraftAt => "Where's My Craft At",
        _ => type
    };

    private void RefreshCurrentArea()
    {
        if (_selectedAreaIndex < _areas.Count)
        {
            _areas[_selectedAreaIndex].RefreshData();
        }
    }

    private void DrawAuthenticationSection()
    {
        const float buttonWidth = 200f;
        const float inputWidth = 400f;

        // Authentication Status with collapsible header
        var isAuthOpen = ImGui.CollapsingHeader("Authentication Status", ImGuiTreeNodeFlags.DefaultOpen);

        if (isAuthOpen)
        {
            ImGui.Indent();

            // Status color indicator
            var statusColor = _isAuthenticated
                ? new Vector4(0, 1, 0, 1)
                : new Vector4(1, 0, 0, 1);

            ImGui.TextColored(statusColor,
                _isAuthenticated
                    ? "Authenticated"
                    : "Not Authenticated");

            if (_isAuthenticated)
            {
                ImGui.Text($"User ID: {Settings.UserId}");
                ImGui.Text($"Admin: {Settings.IsAdmin}");
                ImGui.Text($"Token Expires: {DateTimeOffset.FromUnixTimeSeconds(Settings.AccessTokenExpiry).LocalDateTime}");
            }

            // Authentication controls in a sub-section
            if (ImGui.TreeNode("Authentication Controls"))
            {
                if (ImGui.Button("Open Login Page", new Vector2(buttonWidth, 24)))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = $"{API_URL}{Routes.Auth.DiscordLogin}",
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        _statusMessage = $"Failed to open login page: {ex.Message}";
                    }
                }

                ImGui.PushItemWidth(inputWidth);
                if (ImGui.InputText("New Auth Token", ref _newAuthToken, 2048))
                {
                    // Token input changed
                }

                ImGui.PopItemWidth();

                if (ImGui.Button("Use New Auth Token", new Vector2(buttonWidth, 24)))
                {
                    _ = LoginWithNewTokenAsync();
                }

                ImGui.TreePop();
            }

            // Display status message
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                ImGui.TextColored(_statusMessage.StartsWith("Error:")
                        ? new Vector4(1, 0, 0, 1)
                        : new Vector4(0, 1, 0, 1),
                    _statusMessage);
            }

            ImGui.Unindent();
        }

        ImGui.Separator();

        // Template Type Selector (outside the auth section)
        var currentType = Settings.SelectedTemplateType.Value;
        if (ImGui.BeginCombo("Template Type", GetTemplateTypeDisplayName(currentType)))
        {
            if (ImGui.Selectable(GetTemplateTypeDisplayName(Routes.Types.ItemFilterLibrary), currentType == Routes.Types.ItemFilterLibrary))
            {
                Settings.SelectedTemplateType.Value = Routes.Types.ItemFilterLibrary;
                RefreshCurrentArea();
            }

            if (ImGui.Selectable(GetTemplateTypeDisplayName(Routes.Types.WheresMyCraftAt), currentType == Routes.Types.WheresMyCraftAt))
            {
                Settings.SelectedTemplateType.Value = Routes.Types.WheresMyCraftAt;
                RefreshCurrentArea();
            }

            ImGui.EndCombo();
        }
    }

    private void DrawAreaTabs()
    {
        if (ImGui.BeginTabBar("AreaTabs"))
        {
            for (var i = 0; i < _areas.Count; i++)
            {
                var area = _areas[i];
                if (ImGui.BeginTabItem(area.Name))
                {
                    // Check if we switched tabs
                    if (_selectedAreaIndex != i)
                    {
                        // Clear any open modals from the previous area
                        if (_previousAreaIndex >= 0 && _previousAreaIndex < _areas.Count)
                        {
                            if (_areas[_previousAreaIndex] is MyTemplatesArea myTemplates)
                            {
                                myTemplates.CloseModals();
                            }
                            else if (_areas[_previousAreaIndex] is PublicTemplatesArea publicTemplates)
                            {
                                publicTemplates.CloseModals();
                            }
                        }

                        _previousAreaIndex = _selectedAreaIndex;
                        _selectedAreaIndex = i;
                    }

                    area.Draw();
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }
    }

    public string UnixTimeToString(string unixTimeStr)
    {
        try
        {
            if (long.TryParse(unixTimeStr, out var unixTime))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime.ToString("yyyy-MM-dd HH:mm");
            }

            return "Invalid Date";
        }
        catch
        {
            return "Invalid Date";
        }
    }

    // Modified auth methods to update areas when needed
    private async Task ValidateExistingAuthAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;

            if (!Settings.HasValidAccessToken)
            {
                SetUnauthenticatedState("No valid token found");
                return;
            }

            _statusMessage = "Validating stored credentials...";

            var isValid = await _apiClient.ValidateTokenAsync();
            if (!isValid)
            {
                SetUnauthenticatedState("Invalid or expired credentials");
                return;
            }

            // Successfully validated
            _isAuthenticated = true;
            _statusMessage = "Successfully authenticated";

            // Refresh the current area if authenticated
            if (_selectedAreaIndex < _areas.Count)
            {
                _areas[_selectedAreaIndex].RefreshData(); // Now we can call this directly without casting
            }
        }
        catch (ApiAuthenticationException authEx)
        {
            HandleAuthenticationError(authEx);
        }
        catch (ApiException apiEx)
        {
            SetUnauthenticatedState($"API Error: {apiEx.Message}");
        }
        catch (Exception ex)
        {
            SetUnauthenticatedState($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoginWithNewTokenAsync()
    {
        try
        {
            IsLoading = true;
            _statusMessage = "Logging in with new token...";

            await _apiClient.LoginAsync(_newAuthToken);
            _isAuthenticated = true;
            _newAuthToken = string.Empty; // Clear the input

            // Refresh the current area after successful login
            if (_selectedAreaIndex < _areas.Count)
            {
                (_areas[_selectedAreaIndex] as BaseArea)?.RefreshData();
            }
        }
        catch (ApiAuthenticationException authEx)
        {
            HandleAuthenticationError(authEx);
        }
        catch (Exception ex)
        {
            SetUnauthenticatedState($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void HandleAuthenticationError(ApiAuthenticationException authEx)
    {
        SetUnauthenticatedState(authEx.ErrorCode switch
        {
            ErrorCodes.AuthInvalidToken => "Stored token is no longer valid - please log in again",
            ErrorCodes.AuthError when authEx.DetailedMessage?.Contains("Invalid or expired refresh token") == true => "Token has been invalidated by server - please log in again",
            ErrorCodes.AuthRequired => "Authentication required - please log in again",
            _ => $"Authentication failed: {authEx.Message}"
        });
    }

    private void SetUnauthenticatedState(string message)
    {
        _isAuthenticated = false;
        Settings.ClearTokens();
        _statusMessage = message;
    }

    public override void Dispose()
    {
        _apiClient?.Dispose();
        base.Dispose();
    }
}