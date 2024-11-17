using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api.Models;

public class TemplateDetailed
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