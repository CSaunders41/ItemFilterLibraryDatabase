using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api.Models
{
    namespace ItemFilterLibraryDatabase.Api.Models
    {
        public class PublicTemplateListResponse
        {
            [JsonPropertyName("data")]
            public List<Template> Templates { get; set; }

            [JsonPropertyName("pagination")]
            public PaginationInfo Pagination { get; set; }
        }

        public class PaginationInfo
        {
            [JsonPropertyName("currentPage")]
            public int CurrentPage { get; set; }

            [JsonPropertyName("itemsPerPage")]
            public int ItemsPerPage { get; set; }

            [JsonPropertyName("totalItems")]
            public int TotalItems { get; set; }

            [JsonPropertyName("hasNextPage")]
            public bool HasNextPage { get; set; }

            [JsonPropertyName("hasPreviousPage")]
            public bool HasPreviousPage { get; set; }

            [JsonPropertyName("nextPage")]
            public int NextPage { get; set; }

            [JsonPropertyName("previousPage")]
            public int PreviousPage { get; set; }

            [JsonPropertyName("lastPage")]
            public int LastPage { get; set; }
        }
    }
}