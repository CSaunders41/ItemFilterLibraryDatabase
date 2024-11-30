using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api.Models;

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

    [JsonPropertyName("isAdmin")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    public bool IsAdmin { get; set; } // Added setter
}