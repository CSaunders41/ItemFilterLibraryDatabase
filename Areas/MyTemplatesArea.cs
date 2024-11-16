using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.UI;
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
    private List<TemplateInfo> _allTemplates = [];
    private int _currentPage = 1;
    private string _errorMessage = string.Empty;
    private List<TemplateInfo> _filteredTemplates = [];
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
        // Top controls section
        var buttonSize = new Vector2(120, 24);

        if (ImGui.Button("New Template", buttonSize))
        {
            _templateModal.Show(null, TemplateModalMode.Create);
        }

        ImGui.SameLine();
        if (ImGui.Button("Refresh", buttonSize) && !_isRefreshing)
        {
            RefreshTemplates();
        }

        ImGui.SameLine();
        ImGui.PushItemWidth(300);
        if (ImGui.InputText("Search Templates", ref _searchText, 100))
        {
            _searchDelay = SearchDelayDuration;
        }

        ImGui.PopItemWidth();

        ShowError(_errorMessage);

        // Pagination controls
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

        // Handle search delay
        if (_searchDelay > 0)
        {
            _searchDelay -= ImGui.GetIO().DeltaTime;
            if (_searchDelay <= 0)
            {
                FilterTemplates();
            }
        }

        // Table section
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
            ImGui.Text($"No templates found for {GetTemplateTypeDisplayName()}");
        }

        _templateModal.Draw();
    }

    private void DrawTemplatesTable()
    {
        var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollY;

        if (ImGui.BeginTable("my_templates", 5, flags))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Version", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("Updated", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Public", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthFixed, 240);
            ImGui.TableHeadersRow();

            // Handle sorting
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
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(template.Name);

                ImGui.TableNextColumn();
                ImGui.Text(template.Version.ToString());

                ImGui.TableNextColumn();
                ImGui.Text(FormatTimeAgo(template.UpdatedAt));

                ImGui.TableNextColumn();
                ImGui.Text(template.IsPublic
                    ? "Yes"
                    : "No");

                ImGui.TableNextColumn();
                ImGui.PushID($"actions_{template.TemplateId}");

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
        _filteredTemplates = _allTemplates.Where(template => FuzzyMatcher.FuzzyMatch(_searchText, template.Name)).ToList();

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
                ? query.OrderBy(t => long.Parse(t.UpdatedAt))
                : query.OrderByDescending(t => long.Parse(t.UpdatedAt)),
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

    private string GetTemplateTypeDisplayName()
    {
        return Plugin.Settings.SelectedTemplateType.Value switch
        {
            Routes.Types.ItemFilterLibrary => "Item Filter Library",
            Routes.Types.WheresMyCraftAt => "Where's My Craft At",
            _ => Plugin.Settings.SelectedTemplateType.Value
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

            var response = await ApiClient.GetAsync<ApiResponse<List<TemplateInfo>>>(Routes.Templates.MyTemplates(Plugin.Settings.SelectedTemplateType.Value));

            _allTemplates = response.Data;
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

            var response = await ApiClient.GetAsync<ApiResponse<TemplateInfo>>(Routes.Templates.GetTemplate(Plugin.Settings.SelectedTemplateType.Value, templateId, true));

            if (response?.Data?.Versions is {Count: > 0})
            {
                var latestVersion = response.Data.Versions[0];
                ImGui.SetClipboardText(latestVersion.Content);
            }
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: Failed to copy template - {ex.Message}";
        }
        finally
        {
            Plugin.IsLoading = false;
        }
    }

    protected async void DeleteTemplate(string templateId)
    {
        try
        {
            Plugin.IsLoading = true;
            _errorMessage = string.Empty;

            await ApiClient.DeleteAsync<ApiResponse<object>>(Routes.Templates.DeleteTemplate(Plugin.Settings.SelectedTemplateType.Value, templateId));

            RefreshTemplates();
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: Failed to delete template - {ex.Message}";
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