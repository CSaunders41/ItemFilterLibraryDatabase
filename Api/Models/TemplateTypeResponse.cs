using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api.Models;

public class TestAuthApiResponse
{
    [JsonPropertyName("data")]
    public TestAuthResponseData Data { get; set; }
}

public class TestAuthResponseData
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("user")]
    public TestAuthUserInfo User { get; set; }

    [JsonPropertyName("tokenExpiry")]
    public long TokenExpiry { get; set; }
}

public class TestAuthUserInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }
}