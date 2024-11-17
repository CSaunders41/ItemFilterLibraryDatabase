using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api.Models;

public class Template
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
            if (int.TryParse(reader.GetString(), out var value))
            {
                return value;
            }

            return 0;
        }

        if (reader.TokenType == JsonTokenType.Number)
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