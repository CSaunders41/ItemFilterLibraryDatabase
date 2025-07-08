using Microsoft.Data.Sqlite;
using Dapper;
using ItemFilterLibraryAPI.Models;
using System.Text.Json;

namespace ItemFilterLibraryAPI.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        // Read and execute the schema SQL file
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Database.sql");
        if (File.Exists(schemaPath))
        {
            var schema = File.ReadAllText(schemaPath);
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            connection.Execute(schema);
        }
    }

    // Template operations
    public async Task<IEnumerable<DbTemplate>> GetPublicTemplatesAsync(string typeId, int page = 1, int perPage = 20)
    {
        using var connection = new SqliteConnection(_connectionString);
        var offset = (page - 1) * perPage;
        
        var query = @"
            SELECT * FROM templates_with_details 
            WHERE type_id = @TypeId AND is_public = 1 
            ORDER BY updated_at DESC 
            LIMIT @Limit OFFSET @Offset";

        return await connection.QueryAsync<DbTemplate>(query, new { TypeId = typeId, Limit = perPage, Offset = offset });
    }

    public async Task<int> GetPublicTemplatesCountAsync(string typeId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT COUNT(*) FROM templates_with_details WHERE type_id = @TypeId AND is_public = 1";
        return await connection.QuerySingleAsync<int>(query, new { TypeId = typeId });
    }

    public async Task<IEnumerable<DbTemplate>> GetMyTemplatesAsync(string userId, string typeId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            SELECT * FROM templates_with_details 
            WHERE user_id = @UserId AND type_id = @TypeId 
            ORDER BY updated_at DESC";

        return await connection.QueryAsync<DbTemplate>(query, new { UserId = userId, TypeId = typeId });
    }

    public async Task<DbTemplate?> GetTemplateAsync(string templateId, bool includeAllVersions = false)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT * FROM templates_with_details WHERE template_id = @TemplateId";
        return await connection.QueryFirstOrDefaultAsync<DbTemplate>(query, new { TemplateId = templateId });
    }

    public async Task<IEnumerable<DbTemplateVersion>> GetTemplateVersionsAsync(string templateId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT * FROM template_versions WHERE template_id = @TemplateId ORDER BY version_number DESC";
        return await connection.QueryAsync<DbTemplateVersion>(query, new { TemplateId = templateId });
    }

    public async Task<string> CreateTemplateAsync(string userId, string typeId, string name, object content)
    {
        using var connection = new SqliteConnection(_connectionString);
        using var transaction = connection.BeginTransaction();

        try
        {
            var templateId = Guid.NewGuid().ToString();
            var versionId = Guid.NewGuid().ToString();
            var contentJson = JsonSerializer.Serialize(content);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Insert template
            var insertTemplateQuery = @"
                INSERT INTO templates (template_id, type_id, user_id, name, content_type, created_at, updated_at)
                VALUES (@TemplateId, @TypeId, @UserId, @Name, @ContentType, @CreatedAt, @UpdatedAt)";

            await connection.ExecuteAsync(insertTemplateQuery, new
            {
                TemplateId = templateId,
                TypeId = typeId,
                UserId = userId,
                Name = name,
                ContentType = "application/json",
                CreatedAt = now,
                UpdatedAt = now
            }, transaction);

            // Insert version
            var insertVersionQuery = @"
                INSERT INTO template_versions (version_id, template_id, version_number, content, created_at)
                VALUES (@VersionId, @TemplateId, @VersionNumber, @Content, @CreatedAt)";

            await connection.ExecuteAsync(insertVersionQuery, new
            {
                VersionId = versionId,
                TemplateId = templateId,
                VersionNumber = 1,
                Content = contentJson,
                CreatedAt = now
            }, transaction);

            transaction.Commit();
            return templateId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateTemplateAsync(string templateId, string userId, string name, object content)
    {
        using var connection = new SqliteConnection(_connectionString);
        using var transaction = connection.BeginTransaction();

        try
        {
            // Check if user owns the template
            var template = await connection.QueryFirstOrDefaultAsync<DbTemplate>(
                "SELECT * FROM templates WHERE template_id = @TemplateId AND user_id = @UserId",
                new { TemplateId = templateId, UserId = userId }, transaction);

            if (template == null) return false;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var newVersion = template.Version + 1;
            var versionId = Guid.NewGuid().ToString();
            var contentJson = JsonSerializer.Serialize(content);

            // Update template
            var updateTemplateQuery = @"
                UPDATE templates 
                SET name = @Name, version = @Version, version_count = @VersionCount, updated_at = @UpdatedAt
                WHERE template_id = @TemplateId";

            await connection.ExecuteAsync(updateTemplateQuery, new
            {
                Name = name,
                Version = newVersion,
                VersionCount = template.VersionCount + 1,
                UpdatedAt = now,
                TemplateId = templateId
            }, transaction);

            // Insert new version
            var insertVersionQuery = @"
                INSERT INTO template_versions (version_id, template_id, version_number, content, created_at)
                VALUES (@VersionId, @TemplateId, @VersionNumber, @Content, @CreatedAt)";

            await connection.ExecuteAsync(insertVersionQuery, new
            {
                VersionId = versionId,
                TemplateId = templateId,
                VersionNumber = newVersion,
                Content = contentJson,
                CreatedAt = now
            }, transaction);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ToggleTemplateVisibilityAsync(string templateId, string userId, bool isPublic)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            UPDATE templates 
            SET is_public = @IsPublic, updated_at = @UpdatedAt
            WHERE template_id = @TemplateId AND user_id = @UserId";

        var rowsAffected = await connection.ExecuteAsync(query, new
        {
            IsPublic = isPublic,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            TemplateId = templateId,
            UserId = userId
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteTemplateAsync(string templateId, string userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            UPDATE templates 
            SET is_active = 0, updated_at = @UpdatedAt
            WHERE template_id = @TemplateId AND user_id = @UserId";

        var rowsAffected = await connection.ExecuteAsync(query, new
        {
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            TemplateId = templateId,
            UserId = userId
        });

        return rowsAffected > 0;
    }

    public async Task<bool> HardDeleteTemplateAsync(string templateId, string userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        using var transaction = connection.BeginTransaction();

        try
        {
            // Check if user owns the template
            var template = await connection.QueryFirstOrDefaultAsync<DbTemplate>(
                "SELECT * FROM templates WHERE template_id = @TemplateId AND user_id = @UserId",
                new { TemplateId = templateId, UserId = userId }, transaction);

            if (template == null) return false;

            // Delete versions (cascade will handle this, but being explicit)
            await connection.ExecuteAsync(
                "DELETE FROM template_versions WHERE template_id = @TemplateId",
                new { TemplateId = templateId }, transaction);

            // Delete template
            await connection.ExecuteAsync(
                "DELETE FROM templates WHERE template_id = @TemplateId",
                new { TemplateId = templateId }, transaction);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // Template Types
    public async Task<IEnumerable<TemplateType>> GetTemplateTypesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT * FROM template_types ORDER BY description";
        var dbTypes = await connection.QueryAsync<dynamic>(query);
        
        return dbTypes.Select(t => new TemplateType
        {
            TypeId = t.type_id,
            Description = t.description,
            ContentType = t.content_type,
            MaxVersions = t.max_versions,
            MaxSizeBytes = t.max_size_bytes
        });
    }

    // User management
    public async Task<DbUser?> GetUserAsync(string userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT * FROM users WHERE user_id = @UserId";
        return await connection.QueryFirstOrDefaultAsync<DbUser>(query, new { UserId = userId });
    }

    public async Task<DbUser?> GetUserByUsernameAsync(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT * FROM users WHERE username = @Username";
        return await connection.QueryFirstOrDefaultAsync<DbUser>(query, new { Username = username });
    }

    public async Task<string> CreateUserAsync(string username, string displayName, bool isAdmin = false)
    {
        using var connection = new SqliteConnection(_connectionString);
        var userId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var query = @"
            INSERT INTO users (user_id, username, display_name, is_admin, created_at, updated_at)
            VALUES (@UserId, @Username, @DisplayName, @IsAdmin, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(query, new
        {
            UserId = userId,
            Username = username,
            DisplayName = displayName,
            IsAdmin = isAdmin,
            CreatedAt = now,
            UpdatedAt = now
        });

        return userId;
    }

    // Session management
    public async Task<string> CreateSessionAsync(string userId, string accessToken, string refreshToken, long accessExpires, long refreshExpires)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sessionId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var query = @"
            INSERT INTO user_sessions (session_id, user_id, access_token, refresh_token, access_token_expires, refresh_token_expires, created_at, updated_at)
            VALUES (@SessionId, @UserId, @AccessToken, @RefreshToken, @AccessTokenExpires, @RefreshTokenExpires, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(query, new
        {
            SessionId = sessionId,
            UserId = userId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpires = accessExpires,
            RefreshTokenExpires = refreshExpires,
            CreatedAt = now,
            UpdatedAt = now
        });

        return sessionId;
    }

    public async Task<DbUserSession?> GetSessionByAccessTokenAsync(string accessToken)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT * FROM user_sessions WHERE access_token = @AccessToken";
        return await connection.QueryFirstOrDefaultAsync<DbUserSession>(query, new { AccessToken = accessToken });
    }

    public async Task<DbUserSession?> GetSessionByRefreshTokenAsync(string refreshToken)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "SELECT * FROM user_sessions WHERE refresh_token = @RefreshToken";
        return await connection.QueryFirstOrDefaultAsync<DbUserSession>(query, new { RefreshToken = refreshToken });
    }

    public async Task<bool> UpdateSessionTokensAsync(string sessionId, string newAccessToken, string newRefreshToken, long accessExpires, long refreshExpires)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = @"
            UPDATE user_sessions 
            SET access_token = @AccessToken, refresh_token = @RefreshToken, 
                access_token_expires = @AccessTokenExpires, refresh_token_expires = @RefreshTokenExpires, 
                updated_at = @UpdatedAt
            WHERE session_id = @SessionId";

        var rowsAffected = await connection.ExecuteAsync(query, new
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpires = accessExpires,
            RefreshTokenExpires = refreshExpires,
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            SessionId = sessionId
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteSessionAsync(string sessionId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var query = "DELETE FROM user_sessions WHERE session_id = @SessionId";
        var rowsAffected = await connection.ExecuteAsync(query, new { SessionId = sessionId });
        return rowsAffected > 0;
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var query = "DELETE FROM user_sessions WHERE refresh_token_expires < @Now";
        await connection.ExecuteAsync(query, new { Now = now });
    }
} 