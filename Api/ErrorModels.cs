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
    // Authentication (401)
    public const string AuthRequired = "AUTH_REQUIRED";
    public const string AuthError = "AUTH_ERROR";
    public const string AuthInvalidToken = "AUTH_INVALID_TOKEN";

    // Authorization (403)
    public const string AuthAdminOnly = "AUTH_ADMIN_ONLY";
    public const string AuthForbidden = "AUTH_FORBIDDEN";

    // Validation (400)
    public const string ValidationError = "VALIDATION_ERROR";

    // Rate Limiting (429)
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";

    // Server (500)
    public const string DatabaseError = "DATABASE_ERROR";
    public const string InternalError = "INTERNAL_ERROR";

    // Not Found (404)
    public const string RouteNotFound = "ROUTE_NOT_FOUND";
}