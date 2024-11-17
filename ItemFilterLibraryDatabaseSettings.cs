using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ItemFilterLibraryDatabase.Api;
using System;

namespace ItemFilterLibraryDatabase;

public class ItemFilterLibraryDatabaseSettings : ISettings
{
    // Tokens
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    // Expiry times (Unix timestamps)
    public long AccessTokenExpiry { get; set; } = 0;
    public long RefreshTokenExpiry { get; set; } = 0;

    // User info
    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false;

    // Template type selection
    public string SelectedTemplateType { get; set; } = new(Routes.Types.ItemFilterLibrary);

    public bool HasValidAccessToken =>
        !string.IsNullOrEmpty(AccessToken) && DateTimeOffset.FromUnixTimeSeconds(AccessTokenExpiry) > DateTimeOffset.UtcNow;

    public bool HasValidRefreshToken =>
        !string.IsNullOrEmpty(RefreshToken) && DateTimeOffset.FromUnixTimeSeconds(RefreshTokenExpiry) > DateTimeOffset.UtcNow;

    public ToggleNode Debug { get; set; } = new(false);

    public ToggleNode Enable { get; set; } = new(false);

    public void ClearTokens()
    {
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        AccessTokenExpiry = 0;
        RefreshTokenExpiry = 0;
        UserId = string.Empty;
        IsAdmin = false;
    }
}