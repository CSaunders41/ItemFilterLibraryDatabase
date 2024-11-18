using System.Text.Json.Serialization;

namespace ItemFilterLibraryDatabase.Api;

public class AuthData
{
    [JsonPropertyName("tokens")]
    public TokenInfo Tokens { get; set; }

    [JsonPropertyName("user")]
    public UserInfo User { get; set; }

    public class TokenInfo
    {
        [JsonPropertyName("access")]
        public AccessTokenDetails Access { get; set; }

        [JsonPropertyName("refresh")]
        public RefreshTokenDetails Refresh { get; set; }
    }

    public class AccessTokenDetails
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; set; }
    }

    public class RefreshTokenDetails
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; set; }
    }

    public class UserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}