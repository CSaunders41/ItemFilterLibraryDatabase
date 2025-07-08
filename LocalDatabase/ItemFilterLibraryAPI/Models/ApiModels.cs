using System.Text.Json.Serialization;

namespace ItemFilterLibraryAPI.Models;

// Response wrapper models
public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class PaginationInfo
{
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("from")]
    public int From { get; set; }

    [JsonPropertyName("to")]
    public int To { get; set; }

    [JsonPropertyName("has_next_page")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("has_previous_page")]
    public bool HasPreviousPage { get; set; }

    [JsonPropertyName("next_page")]
    public int NextPage { get; set; }

    [JsonPropertyName("previous_page")]
    public int PreviousPage { get; set; }
}

// Template models
public class Template
{
    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; } = string.Empty;

    [JsonPropertyName("type_id")]
    public string TypeId { get; set; } = string.Empty;

    [JsonPropertyName("discord_id")]
    public string DiscordId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("creator_name")]
    public string CreatorName { get; set; } = string.Empty;

    [JsonPropertyName("version_count")]
    public int VersionCount { get; set; }

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("latest_version")]
    public int LatestVersion { get; set; }
}

public class DetailedTemplate : Template
{
    [JsonPropertyName("latest_version")]
    public TemplateVersion? LatestVersionData { get; set; }

    [JsonPropertyName("versions")]
    public List<TemplateVersion>? Versions { get; set; }
}

public class TemplateVersion
{
    [JsonPropertyName("version_number")]
    public int VersionNumber { get; set; }

    [JsonPropertyName("content")]
    public object? Content { get; set; }

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
}

public class TemplateType
{
    [JsonPropertyName("type_id")]
    public string TypeId { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("max_versions")]
    public int MaxVersions { get; set; }

    [JsonPropertyName("max_size_bytes")]
    public int MaxSizeBytes { get; set; }
}

// Request models
public class CreateTemplateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public object? Content { get; set; }
}

public class UpdateTemplateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public object? Content { get; set; }
}

public class VisibilityRequest
{
    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }
}

// Authentication models
public class User
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }
}

public class AuthTokens
{
    [JsonPropertyName("access")]
    public TokenInfo? Access { get; set; }

    [JsonPropertyName("refresh")]
    public TokenInfo? Refresh { get; set; }
}

public class TokenInfo
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("expires")]
    public long Expires { get; set; }
}

public class AuthData
{
    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("tokens")]
    public AuthTokens? Tokens { get; set; }
}

public class TestAuthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public User? User { get; set; }
}

public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

// Database models
public class DbTemplate
{
    public string TemplateId { get; set; } = string.Empty;
    public string TypeId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int VersionCount { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string TypeDescription { get; set; } = string.Empty;
    public string? LatestContent { get; set; }
}

public class DbTemplateVersion
{
    public string VersionId { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public long CreatedAt { get; set; }
}

public class DbUser
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
}

public class DbUserSession
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public long AccessTokenExpires { get; set; }
    public long RefreshTokenExpires { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }
} 