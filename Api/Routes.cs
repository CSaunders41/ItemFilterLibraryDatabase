using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api;

public static class Routes
{
    public static class Auth
    {
        private const string Base = "/auth";
        public static string DiscordLogin => $"{Base}/discord/login";
        public static string Refresh => $"{Base}/refresh";
        public static string Test => $"{Base}/test";
    }

    public static class Templates
    {
        private const string Base = "/templates";

        public static string GetTypes => $"{Base}/types";

        public static string GetAllTemplates(string typeId, int page = 1, int limit = 20) =>
            $"{Base}/{typeId}/templates?page={page}&limit={limit}";

        public static string GetMyTemplates(string typeId) =>
            $"{Base}/{typeId}/my";

        public static string GetTemplate(string typeId, string templateId, bool includeAllVersions = false) =>
            $"{Base}/{typeId}/template/{templateId}{(includeAllVersions ? "?includeAllVersions=true" : "")}";

        public static string CreateTemplate(string typeId) =>
            $"{Base}/{typeId}/create";

        public static string UpdateTemplate(string typeId, string templateId) =>
            $"{Base}/{typeId}/template/{templateId}";

        public static string ToggleVisibility(string typeId, string templateId) =>
            $"{Base}/{typeId}/template/{templateId}/visibility";

        public static string DeleteTemplate(string typeId, string templateId) =>
            $"{Base}/{typeId}/template/{templateId}";

        public static string HardDeleteTemplate(string typeId, string templateId) =>
            $"{Base}/{typeId}/template/{templateId}/hard";

        public static class RequestBodies
        {
            public class CreateTemplateRequest
            {
                [JsonPropertyName("name")]
                public string Name { get; set; }

                [JsonPropertyName("content")]
                public object Content { get; set; }
            }

            public class UpdateTemplateRequest
            {
                [JsonPropertyName("name")]
                public string Name { get; set; }

                [JsonPropertyName("content")]
                public object Content { get; set; }
            }

            public class VisibilityRequest
            {
                [JsonPropertyName("is_public")]
                public bool IsPublic { get; set; }
            }
        }

        public static class ResponseBodies
        {
            public class TemplateTypeResponse
            {
                [JsonPropertyName("type_id")]
                public string TypeId { get; set; }

                [JsonPropertyName("description")]
                public string Description { get; set; }

                [JsonPropertyName("content_type")]
                public string ContentType { get; set; }

                [JsonPropertyName("max_versions")]
                public int MaxVersions { get; set; }

                [JsonPropertyName("max_size_bytes")]
                public int MaxSizeBytes { get; set; }
            }

            public class TemplateResponse
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
                public int VersionCount { get; set; }

                [JsonPropertyName("latest_version")]
                public TemplateVersionResponse LatestVersion { get; set; }

                [JsonPropertyName("versions")]
                public List<TemplateVersionResponse> Versions { get; set; }
            }

            public class TemplateVersionResponse
            {
                [JsonPropertyName("version_number")]
                public int VersionNumber { get; set; }

                [JsonPropertyName("content")]
                public object Content { get; set; }

                [JsonPropertyName("created_at")]
                public long CreatedAt { get; set; }
            }
        }
    }

    public static class Types
    {
        public const string WheresMyCraftAt = "wheresmycraftat";
        public const string ItemFilterLibrary = "itemfilterlibrary";
    }
}