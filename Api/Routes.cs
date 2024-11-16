namespace ItemFilterLibraryDatabase.Api;

public static class Routes
{
    public static class Auth
    {
        private const string Base = "/auth";

        public static string DiscordLogin => $"{Base}/discord/login";
        public static string Refresh => $"{Base}/refresh";

        public static class Requests
        {
            public static object CreateRefreshRequest(string refreshToken) =>
                new { refresh_token = refreshToken };
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

        public static class RequestBodies
        {
            public class CreateTemplateRequest
            {
                public string Name { get; set; }
                public object Content { get; set; }
                public bool IsPublic { get; set; }
            }

            public class UpdateTemplateRequest
            {
                public string Name { get; set; }
                public object Content { get; set; }
                public string ChangeNotes { get; set; }
                public bool? IsPublic { get; set; }
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
                public bool IsAdmin { get; set; }
            }
        }
    }

    public static class Types
    {
        public const string WheresMyCraftAt = "wheresmycraftat";
        public const string ItemFilterLibrary = "itemfilterlibrary";
    }
}