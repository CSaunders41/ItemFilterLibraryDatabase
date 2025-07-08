using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ItemFilterLibraryAPI.Models;
using ItemFilterLibraryAPI.Services;
using System.Text.Json;

namespace ItemFilterLibraryAPI.Controllers;

[ApiController]
[Route("templates")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly DatabaseService _databaseService;

    public TemplatesController(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [HttpGet("types/list")]
    public async Task<IActionResult> GetTypes()
    {
        try
        {
            var types = await _databaseService.GetTemplateTypesAsync();
            return Ok(new ApiResponse<List<TemplateType>> { Data = types.ToList(), Success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{typeId}/templates")]
    public async Task<IActionResult> GetAllTemplates(string typeId, [FromQuery] int page = 1, [FromQuery] int perPage = 20)
    {
        try
        {
            var templates = await _databaseService.GetPublicTemplatesAsync(typeId, page, perPage);
            var totalCount = await _databaseService.GetPublicTemplatesCountAsync(typeId);
            
            var templateList = templates.Select(MapDbTemplateToTemplate).ToList();
            var totalPages = (int)Math.Ceiling((double)totalCount / perPage);

            var paginationInfo = new PaginationInfo
            {
                CurrentPage = page,
                LastPage = totalPages,
                PerPage = perPage,
                Total = totalCount,
                From = (page - 1) * perPage + 1,
                To = Math.Min(page * perPage, totalCount),
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1,
                NextPage = page < totalPages ? page + 1 : totalPages,
                PreviousPage = page > 1 ? page - 1 : 1
            };

            var response = new
            {
                data = templateList,
                pagination = paginationInfo
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{typeId}/my")]
    public async Task<IActionResult> GetMyTemplates(string typeId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var templates = await _databaseService.GetMyTemplatesAsync(userId, typeId);
            var templateList = templates.Select(MapDbTemplateToTemplate).ToList();
            
            return Ok(new ApiResponse<List<Template>> { Data = templateList, Success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{typeId}/Template/{templateId}")]
    public async Task<IActionResult> GetTemplate(string typeId, string templateId, [FromQuery] bool includeAllVersions = false)
    {
        try
        {
            var template = await _databaseService.GetTemplateAsync(templateId, includeAllVersions);
            if (template == null)
            {
                return NotFound(new { error = "Template not found" });
            }

            var detailedTemplate = MapDbTemplateToDetailedTemplate(template);
            
            if (includeAllVersions)
            {
                var versions = await _databaseService.GetTemplateVersionsAsync(templateId);
                detailedTemplate.Versions = versions.Select(v => new TemplateVersion
                {
                    VersionNumber = v.VersionNumber,
                    Content = JsonSerializer.Deserialize<object>(v.Content),
                    CreatedAt = v.CreatedAt
                }).ToList();
            }

            return Ok(new ApiResponse<DetailedTemplate> { Data = detailedTemplate, Success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{typeId}/create")]
    public async Task<IActionResult> CreateTemplate(string typeId, [FromBody] CreateTemplateRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var templateId = await _databaseService.CreateTemplateAsync(userId, typeId, request.Name, request.Content ?? new object());
            
            return Ok(new ApiResponse<object> { Data = new { template_id = templateId }, Success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{typeId}/Template/{templateId}")]
    public async Task<IActionResult> UpdateTemplate(string typeId, string templateId, [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _databaseService.UpdateTemplateAsync(templateId, userId, request.Name, request.Content ?? new object());
            
            if (!success)
            {
                return NotFound(new { error = "Template not found or you don't have permission to edit it" });
            }

            return Ok(new ApiResponse<object> { Data = new { success = true }, Success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("{typeId}/Template/{templateId}/visibility")]
    public async Task<IActionResult> ToggleVisibility(string typeId, string templateId, [FromBody] VisibilityRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _databaseService.ToggleTemplateVisibilityAsync(templateId, userId, request.IsPublic);
            
            if (!success)
            {
                return NotFound(new { error = "Template not found or you don't have permission to edit it" });
            }

            return Ok(new ApiResponse<object> { Data = new { success = true }, Success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{typeId}/Template/{templateId}")]
    public async Task<IActionResult> DeleteTemplate(string typeId, string templateId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _databaseService.DeleteTemplateAsync(templateId, userId);
            
            if (!success)
            {
                return NotFound(new { error = "Template not found or you don't have permission to delete it" });
            }

            return Ok(new ApiResponse<object> { Data = new { success = true }, Success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{typeId}/Template/{templateId}/hard")]
    public async Task<IActionResult> HardDeleteTemplate(string typeId, string templateId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _databaseService.HardDeleteTemplateAsync(templateId, userId);
            
            if (!success)
            {
                return NotFound(new { error = "Template not found or you don't have permission to delete it" });
            }

            return Ok(new ApiResponse<object> { Data = new { success = true }, Success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private static Template MapDbTemplateToTemplate(DbTemplate dbTemplate)
    {
        return new Template
        {
            TemplateId = dbTemplate.TemplateId,
            TypeId = dbTemplate.TypeId,
            DiscordId = dbTemplate.UserId, // Map UserId to DiscordId for compatibility
            Name = dbTemplate.Name,
            IsPublic = dbTemplate.IsPublic,
            IsActive = dbTemplate.IsActive,
            Version = dbTemplate.Version,
            ContentType = dbTemplate.ContentType,
            CreatorName = dbTemplate.CreatorName,
            VersionCount = dbTemplate.VersionCount,
            UpdatedAt = dbTemplate.UpdatedAt.ToString(),
            CreatedAt = dbTemplate.CreatedAt.ToString(),
            LatestVersion = dbTemplate.Version
        };
    }

    private static DetailedTemplate MapDbTemplateToDetailedTemplate(DbTemplate dbTemplate)
    {
        var template = new DetailedTemplate
        {
            TemplateId = dbTemplate.TemplateId,
            TypeId = dbTemplate.TypeId,
            DiscordId = dbTemplate.UserId,
            Name = dbTemplate.Name,
            IsPublic = dbTemplate.IsPublic,
            IsActive = dbTemplate.IsActive,
            Version = dbTemplate.Version,
            ContentType = dbTemplate.ContentType,
            CreatorName = dbTemplate.CreatorName,
            VersionCount = dbTemplate.VersionCount,
            UpdatedAt = dbTemplate.UpdatedAt.ToString(),
            CreatedAt = dbTemplate.CreatedAt.ToString(),
            LatestVersion = dbTemplate.Version
        };

        if (!string.IsNullOrEmpty(dbTemplate.LatestContent))
        {
            template.LatestVersionData = new TemplateVersion
            {
                VersionNumber = dbTemplate.Version,
                Content = JsonSerializer.Deserialize<object>(dbTemplate.LatestContent),
                CreatedAt = dbTemplate.UpdatedAt
            };
        }

        return template;
    }
} 