using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ItemFilterLibraryAPI.Models;

namespace ItemFilterLibraryAPI.Services;

public class AuthenticationService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly DatabaseService _databaseService;

    public AuthenticationService(IConfiguration configuration, DatabaseService databaseService)
    {
        _secretKey = configuration["JWT:SecretKey"] ?? "your-super-secret-key-change-in-production";
        _issuer = configuration["JWT:Issuer"] ?? "ItemFilterLibraryAPI";
        _audience = configuration["JWT:Audience"] ?? "ItemFilterLibraryAPI";
        _databaseService = databaseService;
    }

    public async Task<AuthData> LoginAsync(string username, string password)
    {
        // For simplicity, we'll use a basic authentication system
        // In production, you'd hash passwords and store them securely
        var user = await _databaseService.GetUserByUsernameAsync(username);
        
        if (user == null)
        {
            // Create user if it doesn't exist (simplified for local use)
            var userId = await _databaseService.CreateUserAsync(username, username, username == "admin");
            user = await _databaseService.GetUserAsync(userId);
        }

        if (user == null) throw new UnauthorizedAccessException("Invalid credentials");

        // Generate tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        
        var accessExpires = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var refreshExpires = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();

        // Store session
        await _databaseService.CreateSessionAsync(user.UserId, accessToken, refreshToken, accessExpires, refreshExpires);

        return new AuthData
        {
            User = new User
            {
                Id = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                IsAdmin = user.IsAdmin
            },
            Tokens = new AuthTokens
            {
                Access = new TokenInfo { Token = accessToken, Expires = accessExpires },
                Refresh = new TokenInfo { Token = refreshToken, Expires = refreshExpires }
            }
        };
    }

    public async Task<AuthData> RefreshTokenAsync(string refreshToken)
    {
        var session = await _databaseService.GetSessionByRefreshTokenAsync(refreshToken);
        if (session == null || session.RefreshTokenExpires < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _databaseService.GetUserAsync(session.UserId);
        if (user == null) throw new UnauthorizedAccessException("User not found");

        // Generate new tokens
        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();
        
        var accessExpires = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var refreshExpires = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();

        // Update session
        await _databaseService.UpdateSessionTokensAsync(session.SessionId, newAccessToken, newRefreshToken, accessExpires, refreshExpires);

        return new AuthData
        {
            User = new User
            {
                Id = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                IsAdmin = user.IsAdmin
            },
            Tokens = new AuthTokens
            {
                Access = new TokenInfo { Token = newAccessToken, Expires = accessExpires },
                Refresh = new TokenInfo { Token = newRefreshToken, Expires = refreshExpires }
            }
        };
    }

    public async Task<(bool IsValid, string UserId)> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return (false, string.Empty);

            // Verify session exists and is valid
            var session = await _databaseService.GetSessionByAccessTokenAsync(token);
            if (session == null || session.AccessTokenExpires < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return (false, string.Empty);
            }

            return (true, userId);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        var dbUser = await _databaseService.GetUserAsync(userId);
        if (dbUser == null) return null;

        return new User
        {
            Id = dbUser.UserId,
            Username = dbUser.Username,
            DisplayName = dbUser.DisplayName,
            IsAdmin = dbUser.IsAdmin
        };
    }

    public string GenerateLoginToken(User user)
    {
        // Generate a simple login token that matches the original API format
        var authData = new AuthData
        {
            User = user,
            Tokens = new AuthTokens
            {
                Access = new TokenInfo 
                { 
                    Token = GenerateAccessToken(new DbUser 
                    { 
                        UserId = user.Id, 
                        Username = user.Username, 
                        DisplayName = user.DisplayName, 
                        IsAdmin = user.IsAdmin 
                    }), 
                    Expires = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() 
                },
                Refresh = new TokenInfo 
                { 
                    Token = GenerateRefreshToken(), 
                    Expires = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds() 
                }
            }
        };

        var json = JsonSerializer.Serialize(authData);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private string GenerateAccessToken(DbUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.DisplayName),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "admin" : "user")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _issuer,
            Audience = _audience
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + 
               Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
} 