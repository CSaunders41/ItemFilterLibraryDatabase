using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api;

public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}

public class TemplateInfo
{
    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; }

    [JsonPropertyName("type_id")]
    public string TypeId { get; set; }

    [JsonPropertyName("discord_id")]
    public string DiscordId { get; set; }

    [JsonPropertyName("creator_name")]
    public string CreatorName { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("is_active")]
    public bool ?IsActive { get; set; }

    [JsonPropertyName("versions")]
    public List<TemplateVersion> Versions { get; set; }
}

public class TemplateVersion
{
    [JsonPropertyName("version_number")]
    public int VersionNumber { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("change_notes")]
    public string ChangeNotes { get; set; }

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
}