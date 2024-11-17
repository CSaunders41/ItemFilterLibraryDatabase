using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api.Models;

public class SuccessResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}