using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api;

public class ApiErrorResponse
{
    [JsonPropertyName("error")]
    public ApiError Error { get; set; }
}

public class ApiError
{
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("details")]
    public object Details { get; set; }
}

public static class ErrorCodes
{
    public const string AUTH_REQUIRED = "AUTH_REQUIRED";
    public const string AUTH_REFRESH_ERROR = "AUTH_REFRESH_ERROR";
    public const string AUTH_INVALID_TOKEN = "AUTH_INVALID_TOKEN";

    public const string AUTH_ADMIN_ONLY = "AUTH_ADMIN_ONLY";
    public const string AUTH_FORBIDDEN = "AUTH_FORBIDDEN";

    public const string VALIDATION_ERROR = "VALIDATION_ERROR";

    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";

    public const string DATABASE_ERROR = "DATABASE_ERROR";
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";

    public const string ROUTE_NOT_FOUND = "ROUTE_NOT_FOUND";
}