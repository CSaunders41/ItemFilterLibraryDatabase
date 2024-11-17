using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api;

public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}

public class PublicTemplateListResponse
{
    [JsonPropertyName("data")]
    public List<TemplateInfo> Data { get; set; }

    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; }
}

public class PaginationInfo
{
    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("itemsPerPage")]
    public int ItemsPerPage { get; set; }

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }

    [JsonPropertyName("nextPage")]
    public int NextPage { get; set; }

    [JsonPropertyName("previousPage")]
    public int PreviousPage { get; set; }

    [JsonPropertyName("lastPage")]
    public int LastPage { get; set; }
}

public class TemplateType
{
    [JsonPropertyName("type_id")]
    public string TypeId { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } // 'json' or 'text'

    [JsonPropertyName("max_versions")]
    public int MaxVersions { get; set; }

    [JsonPropertyName("max_size_bytes")]
    public int MaxSizeBytes { get; set; }
}

public class TemplateListResponse
{
    [JsonPropertyName("templates")]
    public List<TemplateInfo> Templates { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}

public class TemplateInfo
{
    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; }

    [JsonPropertyName("type_id")]
    public string TypeId { get; set; }

    [JsonPropertyName("discord_id")]
    public string DiscordId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("creator_name")]
    public string CreatorName { get; set; }

    [JsonPropertyName("version_count")]
    [JsonConverter(typeof(StringToIntConverter))]
    public int VersionCount { get; set; }

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; }

    [JsonPropertyName("latest_version")]
    public int LatestVersion { get; set; }
}

public class StringToIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (int.TryParse(reader.GetString(), out int value))
            {
                return value;
            }
            return 0;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class TemplateDetailInfo
{
    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; }

    [JsonPropertyName("type_id")]
    public string TypeId { get; set; }

    [JsonPropertyName("discord_id")]
    public string DiscordId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("creator_name")]
    public string CreatorName { get; set; }

    [JsonPropertyName("latest_version")]
    public TemplateVersion LatestVersion { get; set; }

    [JsonPropertyName("versions")]
    public List<TemplateVersion> Versions { get; set; }
}

public class TemplateVersion
{
    [JsonPropertyName("version_number")]
    public int VersionNumber { get; set; }

    [JsonPropertyName("content")]
    public object Content { get; set; }

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
}

public class SuccessResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class TestAuthApiResponse
{
    [JsonPropertyName("data")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    public TestAuthResponseData Data { get; set; } // Added setter
}

public class TestAuthResponseData
{
    [JsonPropertyName("status")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    public string Status { get; set; } // Added setter

    [JsonPropertyName("user")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    public TestAuthUserInfo User { get; set; } // Added setter

    [JsonPropertyName("tokenExpiry")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    public long TokenExpiry { get; set; }
}

public class TestAuthUserInfo
{
    [JsonPropertyName("id")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    public string Id { get; set; } // Added setter
}