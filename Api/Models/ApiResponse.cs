using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api.Models;

public class ApiResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
}