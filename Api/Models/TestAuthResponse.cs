using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api.Models;

public class TemplateTypesApiResponse
{
    [JsonPropertyName("data")]
    public List<TemplateType> Data { get; set; }
}

public class TemplateType
{
    [JsonPropertyName("type_id")]
    public string TypeId { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}