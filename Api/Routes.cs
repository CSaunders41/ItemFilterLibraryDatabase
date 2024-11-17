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

        public static class RequestBodies
        {
            public class RefreshTokenRequest
            {
                [JsonPropertyName("refresh_token")]
                public string RefreshToken { get; set; }
            }
        }

        public static class ResponseBodies
        {
            public class TestAuthResponse
            {
                [JsonPropertyName("status")]
                public string Status { get; set; }

                [JsonPropertyName("user")]
                public UserInfo User { get; set; }

                [JsonPropertyName("token_expiry")]
                public long TokenExpiry { get; set; }

                public class UserInfo
                {
                    [JsonPropertyName("id")]
                    public string Id { get; set; }

                    [JsonPropertyName("is_admin")]
                    public bool IsAdmin { get; set; }
                }
            }
        }
    }

    public static class Health
    {
        private const string Base = "/health";

        public static string Ping => $"{Base}/ping";
    }

    public static class Templates
    {
        private const string Base = "/templates";

        public static string Types => $"{Base}/types";

        public static string CreateTemplate(string typeId) =>
            $"{Base}/{typeId}/create";

        public static string MyTemplates(string typeId) =>
            $"{Base}/{typeId}/my";

        public static string PublicTemplates(string typeId, int page = 1, int limit = 20) =>
            $"{Base}/{typeId}/public?page={page}&limit={limit}";

        public static string GetTemplate(string typeId, string templateId, bool includeAllVersions = false) =>
            $"{Base}/{typeId}/template/{templateId}{(includeAllVersions ? "?includeAllVersions=true" : "")}";

        public static string UpdateTemplate(string typeId, string templateId) =>
            $"{Base}/{typeId}/template/{templateId}";

        public static string DeleteTemplate(string typeId, string templateId) =>
            $"{Base}/{typeId}/template/{templateId}";
        public static string ToggleVisibility(string typeId, string templateId) =>
            $"/templates/{typeId}/template/{templateId}/visibility";

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

                [JsonPropertyName("change_notes")]
                public string ChangeNotes { get; set; }

                [JsonPropertyName("is_public")]
                public bool IsPublic { get; set; }
            }
        }

        public static class ResponseBodies
        {
            public class TemplateTypeResponse
            {
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("name")]
                public string Name { get; set; }

                [JsonPropertyName("description")]
                public string Description { get; set; }
            }
        }
    }

    public static class Users
    {
        private const string Base = "/users";

        public static string GetUser(string discordId) =>
            $"{Base}/{discordId}";

        public static string UpdateAdminStatus(string discordId) =>
            $"{Base}/{discordId}/admin";

        public static class RequestBodies
        {
            public class UpdateAdminRequest
            {
                [JsonPropertyName("is_admin")]
                public bool IsAdmin { get; set; }
            }
        }

        public static class ResponseBodies
        {
            public class UserDetailsResponse
            {
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("discord_id")]
                public string DiscordId { get; set; }

                [JsonPropertyName("username")]
                public string Username { get; set; }

                [JsonPropertyName("is_admin")]
                public bool IsAdmin { get; set; }

                [JsonPropertyName("created_at")]
                public long CreatedAt { get; set; }

                [JsonPropertyName("last_login")]
                public long? LastLogin { get; set; }
            }
        }
    }

    public static class Types
    {
        public const string WheresMyCraftAt = "wheresmycraftat";
        public const string ItemFilterLibrary = "itemfilterlibrary";
    }
}