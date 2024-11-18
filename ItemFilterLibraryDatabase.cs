using ExileCore;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.Areas;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace ItemFilterLibraryDatabase;

public class ItemFilterLibraryDatabase : BaseSettingsPlugin<ItemFilterLibraryDatabaseSettings>
{
    private readonly List<IArea> _areas = [];
    private readonly object _lockObject = new();
    private ApiClient _apiClient;
    private TaskCompletionSource<bool> _initializationSource;
    private Task<bool> _initTask;
    private bool _isAuthenticating;
    private bool _isInitialized;
    private string _newAuthToken = string.Empty;
    private int _previousAreaIndex = -1;
    private int _selectedAreaIndex;
    private string _statusMessage = string.Empty;

    public ItemFilterLibraryDatabase()
    {
        Name = "Filter Database";
    }

    public bool IsLoading { get; set; }
    public static ItemFilterLibraryDatabase Main { get; private set; }

    public override bool Initialise()
    {
        Main = this;

        try
        {
            DebugLog("Starting plugin initialization");
            _apiClient = new ApiClient(Settings.HostUrl.Value);
            _initializationSource = new TaskCompletionSource<bool>();

            // Start the initialization process asynchronously
            // Explicitly specify the type for the task
            _initTask = Task.Run(InitializePluginAsync);

            return true;
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize plugin: {ex}", 30f);
            return false;
        }
    }

    private async Task<bool> InitializePluginAsync()
    {
        if (_isInitialized)
        {
            DebugLog("Plugin already initialized");
            return true;
        }

        try
        {
            DebugLog("Starting plugin initialization");
            IsLoading = true;

            // Initialize API client first
            var initialized = await _apiClient.InitializeAsync();
            if (!initialized)
            {
                DebugLog("API client initialization failed");
                _statusMessage = "Please authenticate to use the plugin";
                return false;
            }

            DebugLog("API client initialized successfully, setting up areas");
            await InitializeAreas();

            _isInitialized = true;
            _statusMessage = "Plugin initialized successfully";
            _initializationSource.TrySetResult(true);
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize plugin: {ex}", 30f);
            _statusMessage = "Failed to initialize plugin. Please try restarting.";
            _initializationSource.TrySetException(ex);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task InitializeAreas()
    {
        try
        {
            DebugLog("Initializing plugin areas");
            _areas.Clear();
            _areas.Add(new PublicTemplatesArea(this, _apiClient));
            _areas.Add(new MyTemplatesArea(this, _apiClient));

            DebugLog("Areas initialized successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize areas: {ex}", 30f);
            throw;
        }
    }

    public override void DrawSettings()
    {
        base.DrawSettings();
        DrawAuthenticationSection();
        ImGui.Separator();

        if (IsLoading)
        {
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "Loading...");
            return;
        }

        if (!_apiClient.IsInitialized)
        {
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "Please authenticate to use the plugin");
            return;
        }

        if (!_isInitialized)
        {
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "Initializing plugin...");
            return;
        }

        DrawTemplateTypeSelector();
        ImGui.Separator();
        DrawAreaTabs();
    }

    private void DrawTemplateTypeSelector()
    {
        var currentType = Settings.SelectedTemplateType;
        if (ImGui.BeginCombo("Template Type", GetTemplateTypeDisplayName(currentType)))
        {
            if (ImGui.Selectable(GetTemplateTypeDisplayName(Routes.Types.ItemFilterLibrary), currentType == Routes.Types.ItemFilterLibrary))
            {
                Settings.SelectedTemplateType = Routes.Types.ItemFilterLibrary;
                RefreshCurrentArea();
            }

            if (ImGui.Selectable(GetTemplateTypeDisplayName(Routes.Types.WheresMyCraftAt), currentType == Routes.Types.WheresMyCraftAt))
            {
                Settings.SelectedTemplateType = Routes.Types.WheresMyCraftAt;
                RefreshCurrentArea();
            }

            if (ImGui.Selectable(GetTemplateTypeDisplayName(Routes.Types.ReAgent), currentType == Routes.Types.ReAgent))
            {
                Settings.SelectedTemplateType = Routes.Types.ReAgent;
                RefreshCurrentArea();
            }

            ImGui.EndCombo();
        }
    }

    private void DrawAuthenticationSection()
    {
        const float buttonWidth = 200f;
        const float inputWidth = 400f;

        var isAuthOpen = ImGui.CollapsingHeader("Authentication Status", ImGuiTreeNodeFlags.DefaultOpen);
        if (!isAuthOpen) return;

        ImGui.Indent();

        // Status Display
        var statusColor = _apiClient.IsInitialized
            ? new Vector4(0, 1, 0, 1)
            : new Vector4(1, 0, 0, 1);

        ImGui.TextColored(statusColor,
            _apiClient.IsInitialized
                ? "Authenticated"
                : "Not Authenticated");

        if (_apiClient.IsInitialized)
        {
            ImGui.Text($"User ID: {Settings.UserId}");
            ImGui.Text($"Admin: {Settings.IsAdmin}");


            if (DateTimeOffset.FromUnixTimeSeconds(Settings.AccessTokenExpiry) < DateTimeOffset.Now)
            {
                ImGui.TextColored(Color.Red.ToImguiVec4(), $"Auth Token Expired: {DateTimeOffset.FromUnixTimeSeconds(Settings.AccessTokenExpiry).LocalDateTime}");
            }
            else
            {
                ImGui.Text($"Auth Token Expires: {DateTimeOffset.FromUnixTimeSeconds(Settings.AccessTokenExpiry).LocalDateTime}");
            }

            if (DateTimeOffset.FromUnixTimeSeconds(Settings.RefreshTokenExpiry) < DateTimeOffset.Now)
            {
                ImGui.TextColored(Color.Red.ToImguiVec4(), $"Refresh Token Expired: {DateTimeOffset.FromUnixTimeSeconds(Settings.RefreshTokenExpiry).LocalDateTime}");
            }
            else
            {
                ImGui.Text($"Refresh Token Expires: {DateTimeOffset.FromUnixTimeSeconds(Settings.RefreshTokenExpiry).LocalDateTime}");
            }
        }

        // Authentication Controls
        if (ImGui.TreeNode("Authentication Controls"))
        {
            if (!_isAuthenticating)
            {
                if (ImGui.Button("Open Login Page", new Vector2(buttonWidth, 24)))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = $"{Settings.HostUrl.Value}{Routes.Auth.DiscordLogin}",
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        _statusMessage = $"Failed to open login page: {ex.Message}";
                    }
                }

                ImGui.PushItemWidth(inputWidth);
                ImGui.InputText("Auth Token", ref _newAuthToken, 2048);
                ImGui.PopItemWidth();

                if (ImGui.Button("Use Auth Token", new Vector2(buttonWidth, 24)))
                {
                    Task.Run(HandleAuthTokenSubmission);
                }
            }
            else
            {
                ImGui.TextColored(new Vector4(1, 1, 0, 1), "Authentication in progress...");
            }

            ImGui.TreePop();
        }

        // Status Message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            var messageColor = _statusMessage.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) || _statusMessage.StartsWith("Failed", StringComparison.OrdinalIgnoreCase) ||
                               _statusMessage.StartsWith("Authentication failed:", StringComparison.OrdinalIgnoreCase)
                ? new Vector4(1, 0, 0, 1)
                : new Vector4(0, 1, 0, 1);

            ImGui.TextColored(messageColor, _statusMessage);
        }

        ImGui.Unindent();
    }

    private void DrawAreaTabs()
    {
        if (!ImGui.BeginTabBar("AreaTabs")) return;

        for (var i = 0; i < _areas.Count; i++)
        {
            var area = _areas[i];
            if (!ImGui.BeginTabItem(area.Name)) continue;

            if (_selectedAreaIndex != i)
            {
                HandleAreaChange(i);
            }

            area.Draw();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private async Task HandleAuthTokenSubmission()
    {
        if (_isAuthenticating) return;

        try
        {
            lock (_lockObject)
            {
                if (_isAuthenticating) return;
                _isAuthenticating = true;
            }

            _statusMessage = "Authenticating...";
            IsLoading = true;

            await _apiClient.LoginAsync(_newAuthToken);
            _newAuthToken = string.Empty;
            _statusMessage = "Authentication successful";

            if (!_isInitialized)
            {
                await InitializePluginAsync();
            }
            else
            {
                RefreshCurrentArea();
            }
        }
        catch (Exception ex)
        {
            _statusMessage = $"Authentication failed: {ex.Message}";
            LogError($"Authentication failed: {ex}", 30f);
        }
        finally
        {
            _isAuthenticating = false;
            IsLoading = false;
        }
    }

    private void HandleAreaChange(int newIndex)
    {
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
        _selectedAreaIndex = newIndex;
    }

    private void RefreshCurrentArea()
    {
        if (_selectedAreaIndex < _areas.Count)
        {
            _areas[_selectedAreaIndex].RefreshData();
        }
    }

    public string GetTemplateTypeDisplayName(string type) => type switch
    {
        Routes.Types.ItemFilterLibrary => "Item Filter Library",
        Routes.Types.WheresMyCraftAt => "Where's My Craft At",
        Routes.Types.ReAgent => "ReAgent",
        _ => type
    };

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

    private void DebugLog(string message)
    {
        if (Settings.Debug)
        {
            LogMessage($"[ItemFilterLibraryDatabase] {message}");
        }
    }

    public override void Dispose()
    {
        DebugLog("Disposing plugin");
        _apiClient?.Dispose();
        base.Dispose();
    }
}