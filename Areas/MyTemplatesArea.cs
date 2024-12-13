using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.Api.Models;
using ItemFilterLibraryDatabase.UI;
using ItemFilterLibraryDatabase.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ItemFilterLibraryDatabase.Areas;

public class MyTemplatesArea : BaseArea
{
    private const int ItemsPerPage = 20;
    private const float SearchDelayDuration = 0.5f;
    private readonly SortState _sortState = new() {Column = SortColumn.Updated, Ascending = false};
    private readonly TemplateModal _templateModal;
    private List<Template> _allTemplates = [];
    private int _currentPage = 1;
    private string _errorMessage = string.Empty;
    private List<Template> _filteredTemplates = [];
    private bool _isRefreshing = false;
    private float _searchDelay = 0;
    private string _searchText = string.Empty;

    public MyTemplatesArea(ItemFilterLibraryDatabase plugin, ApiClient apiClient) : base(plugin, apiClient)
    {
        _templateModal = new TemplateModal(plugin, apiClient);
        if (Plugin.Initialized)
        {
            RefreshTemplates();
        }
    }

    public override string Name => "My Templates";

    public override void CloseModals()
    {
        _templateModal?.Close();
    }

    public override void Draw()
{
    ImGui.PushItemWidth(300);
    if (ImGui.InputText("Search Templates", ref _searchText, 100))
    {
        _searchDelay = SearchDelayDuration;
    }

    ImGui.PopItemWidth();

    ImGui.SameLine();
    if (ImGui.Button("Refresh") && !_isRefreshing)
    {
        RefreshTemplates();
    }

    ImGui.SameLine();
    if (ImGui.Button("New Template [+]"))
    {
        _templateModal.Show(null, TemplateModalMode.Create);
    }

    ShowError(_errorMessage);

    if (_filteredTemplates.Count > 0)
    {
        var totalPages = (_filteredTemplates.Count + ItemsPerPage - 1) / ItemsPerPage;

        if (ImGui.Button("Previous") && _currentPage > 1)
        {
            _currentPage--;
        }

        ImGui.SameLine();
        ImGui.Text($"Page {_currentPage} of {totalPages}");

        ImGui.SameLine();
        if (ImGui.Button("Next") && _currentPage < totalPages)
        {
            _currentPage++;
        }

        ImGui.SameLine();
        var spacing = ImGui.GetStyle().ItemSpacing.X * 4;
        ImGui.Dummy(new Vector2(spacing, 0));
        ImGui.SameLine();

        ImGui.Text($"Total templates: {_filteredTemplates.Count}");
        ImGui.Separator();
    }

    if (_searchDelay > 0)
    {
        _searchDelay -= ImGui.GetIO().DeltaTime;
        if (_searchDelay <= 0)
        {
            FilterTemplates();
        }
    }

    if (_filteredTemplates.Count > 0)
    {
        DrawTemplatesTable();
    }
    else if (_isRefreshing)
    {
        ImGui.Text("Loading templates...");
    }
    else if (!string.IsNullOrEmpty(_searchText))
    {
        ImGui.Text("No templates found matching your search");
    }
    else
    {
        ImGui.Text($"No templates found for '{ItemFilterLibraryDatabase.Main.Settings.CurrentTemplateType?.Description ?? "selected type"}'");
    }

    _templateModal.Draw();
}

private void DrawTemplatesTable()
{
    var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable;

    if (ImGui.BeginTable("my_templates", 5, flags))
    {
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Public", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Updated", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Version", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthFixed, 240);
        ImGui.TableHeadersRow();

        var sortSpecs = ImGui.TableGetSortSpecs();
        if (sortSpecs.SpecsDirty)
        {
            var sortSpec = sortSpecs.Specs;
            _sortState.Column = (SortColumn)sortSpec.ColumnIndex;
            _sortState.Ascending = sortSpec.SortDirection == ImGuiSortDirection.Ascending;
            ApplySort();
            sortSpecs.SpecsDirty = false;
        }

        var startIndex = (_currentPage - 1) * ItemsPerPage;
        var pageTemplates = _filteredTemplates.Skip(startIndex).Take(ItemsPerPage).ToList();

        foreach (var template in pageTemplates)
        {
            ImGui.PushID($"actions_{template.TemplateId}");
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(template.Name);

            ImGui.TableNextColumn();
            if (ImGui.Button(template.IsPublic ? "Public" : "Private", new Vector2(-1, 0)))
            {
                ToggleTemplateVisibility(template.TemplateId, !template.IsPublic);
            }

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(FormatTimeAgo(template.UpdatedAt));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(template.Version.ToString());

            ImGui.TableNextColumn();

            var buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2) / 3;
            if (ImGui.Button("Edit", new Vector2(buttonWidth, 0)))
            {
                _templateModal.Show(template, TemplateModalMode.Edit);
            }

            ImGui.SameLine();
            if (ImGui.Button("Copy", new Vector2(buttonWidth, 0)))
            {
                CopyTemplateContent(template.TemplateId);
            }

            ImGui.SameLine();
            if (ImGui.Button("Delete", new Vector2(buttonWidth, 0)))
            {
                DeleteTemplate(template.TemplateId);
            }

            ImGui.PopID();
        }

        ImGui.EndTable();
    }
}

    private void FilterTemplates()
    {
        if (string.IsNullOrEmpty(_searchText))
        {
            _filteredTemplates = _allTemplates.ToList();
        }
        else
        {
            _filteredTemplates = _allTemplates.Where(template => FuzzyMatcher.FuzzyMatch(_searchText, template.Name))
                .OrderByDescending(template => FuzzyMatcher.GetMatchScore(_searchText, template.Name)).ToList();
        }

        ApplySort();
        _currentPage = 1;
    }

    private void ApplySort()
    {
        var query = _filteredTemplates.AsQueryable();

        query = _sortState.Column switch
        {
            SortColumn.Name => _sortState.Ascending
                ? query.OrderBy(t => t.Name)
                : query.OrderByDescending(t => t.Name),
            SortColumn.Version => _sortState.Ascending
                ? query.OrderBy(t => t.Version)
                : query.OrderByDescending(t => t.Version),
            SortColumn.Updated => _sortState.Ascending
                ? query.OrderBy(t => t.UpdatedAt)
                : query.OrderByDescending(t => t.UpdatedAt),
            SortColumn.Public => _sortState.Ascending
                ? query.OrderBy(t => t.IsPublic)
                : query.OrderByDescending(t => t.IsPublic),
            _ => query
        };

        _filteredTemplates = query.ToList();
    }

    private string FormatTimeAgo(string unixTimeStr)
    {
        if (!long.TryParse(unixTimeStr, out var unixTime))
        {
            return "Invalid date";
        }

        var timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTime);
        var now = DateTimeOffset.UtcNow;
        var diff = now - timestamp;

        return diff.TotalDays switch
        {
            >= 365 => $"{(int)(diff.TotalDays / 365)}y ago",
            >= 30 => $"{(int)(diff.TotalDays / 30)}mo ago",
            >= 7 => $"{(int)(diff.TotalDays / 7)}w ago",
            >= 1 => $"{(int)diff.TotalDays}d ago",
            _ => diff.TotalHours switch
            {
                >= 1 => $"{(int)diff.TotalHours}h ago",
                _ => diff.TotalMinutes switch
                {
                    >= 1 => $"{(int)diff.TotalMinutes}m ago",
                    _ => "just now"
                }
            }
        };
    }

    public override void RefreshData()
    {
        RefreshTemplates();
    }

    protected async void RefreshTemplates()
    {
        if (_isRefreshing) return;

        try
        {
            _isRefreshing = true;
            Plugin.IsLoading = true;
            _errorMessage = string.Empty;

            var currentType = Plugin.Settings.CurrentTemplateType;
            if (currentType == null)
            {
                _errorMessage = "No template type selected";
                return;
            }

            var response = await ApiClient.GetAsync<ApiResponse<List<Template>>>(Routes.Templates.GetMyTemplates(currentType.TypeId));

            _allTemplates = response.Data ?? [];
            FilterTemplates();
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: {ex.Message}";
            _allTemplates.Clear();
            _filteredTemplates.Clear();
        }
        finally
        {
            _isRefreshing = false;
            Plugin.IsLoading = false;
        }
    }

    private async void CopyTemplateContent(string templateId)
    {
        try
        {
            Plugin.IsLoading = true;
            _errorMessage = string.Empty;

            var currentType = Plugin.Settings.CurrentTemplateType;
            if (currentType == null)
            {
                _errorMessage = "No template type selected";
                return;
            }

            var response = await ApiClient.GetAsync<ApiResponse<TemplateDetailed>>(Routes.Templates.GetTemplate(currentType.TypeId, templateId, true));

            if (response?.Data?.LatestVersion?.Content != null)
            {
                ImGui.SetClipboardText(response.Data.LatestVersion.Content.ToString());
            }
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: Failed to copy Template - {ex.Message}";
        }
        finally
        {
            Plugin.IsLoading = false;
        }
    }

    private async void ToggleTemplateVisibility(string templateId, bool isPublic)
    {
        try
        {
            Plugin.IsLoading = true;
            _errorMessage = string.Empty;

            var currentType = Plugin.Settings.CurrentTemplateType;
            if (currentType == null)
            {
                _errorMessage = "No template type selected";
                return;
            }

            await ApiClient.PatchAsync<ApiResponse<object>>(Routes.Templates.ToggleVisibility(currentType.TypeId, templateId), new {is_public = isPublic});

            RefreshTemplates();
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: Failed to update Template visibility - {ex.Message}";
        }
        finally
        {
            Plugin.IsLoading = false;
        }
    }

    private async void DeleteTemplate(string templateId)
    {
        try
        {
            Plugin.IsLoading = true;
            _errorMessage = string.Empty;

            var currentType = Plugin.Settings.CurrentTemplateType;
            if (currentType == null)
            {
                _errorMessage = "No template type selected";
                return;
            }

            await ApiClient.DeleteAsync<ApiResponse<object>>(Routes.Templates.DeleteTemplate(currentType.TypeId, templateId));

            RefreshTemplates();
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: Failed to delete Template - {ex.Message}";
        }
        finally
        {
            Plugin.IsLoading = false;
        }
    }

    private enum SortColumn
    {
        Name,
        Version,
        Updated,
        Public
    }

    private class SortState
    {
        public SortColumn Column { get; set; }
        public bool Ascending { get; set; }
    }
}