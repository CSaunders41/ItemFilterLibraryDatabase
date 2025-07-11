﻿using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.Api.Models;
using System;
using System.Collections.Generic;

namespace ItemFilterLibraryDatabase;

public class ItemFilterLibraryDatabaseSettings : ISettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    public long AccessTokenExpiry { get; set; } = 0;
    public long RefreshTokenExpiry { get; set; } = 0;

    public string UserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false;

    public TemplateType CurrentTemplateType { get; set; }
    public List<TemplateType> AvailableTemplateTypes { get; set; } = new();

    public bool HasValidAccessToken =>
        !string.IsNullOrEmpty(AccessToken) && DateTimeOffset.FromUnixTimeSeconds(AccessTokenExpiry) > DateTimeOffset.UtcNow;

    public bool HasValidRefreshToken =>
        !string.IsNullOrEmpty(RefreshToken) && DateTimeOffset.FromUnixTimeSeconds(RefreshTokenExpiry) > DateTimeOffset.UtcNow;

    public ToggleNode Debug { get; set; } = new(false);
    public TextNode HostUrl { get; set; } = new TextNode("http://localhost:5000");
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