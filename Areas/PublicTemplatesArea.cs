using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.Api.Models;
using ItemFilterLibraryDatabase.Api.Models.ItemFilterLibraryDatabase.Api.Models;
using ItemFilterLibraryDatabase.UI;
using ItemFilterLibraryDatabase.Utilities;

namespace ItemFilterLibraryDatabase.Areas;

public class PublicTemplatesArea : BaseArea
{
    private const float SearchDelayDuration = 0.5f;
    private readonly List<Template> _allTemplates = [];
    private readonly SortState _sortState = new() { Column = SortColumn.Updated, Ascending = false };
    private readonly TemplateModal _templateModal;
    private int _currentPage = 1;
    private string _errorMessage = string.Empty;
    private List<Template> _filteredTemplates = [];
    private bool _initialLoadComplete;
    private bool _isLoadingBackground;
    private PaginationInfo _paginationInfo = new();
    private float _searchDelay;
    private string _searchText = string.Empty;

    public PublicTemplatesArea(ItemFilterLibraryDatabase plugin, ApiClient apiClient) : base(plugin, apiClient)
    {
        _templateModal = new TemplateModal(plugin, apiClient);
        if (Plugin.Initialized && ApiClient.IsInitialized) LoadTemplates();
    }

    public override string Name => "Public Templates";

    public override void Draw()
    {
        if (!ApiClient.IsInitialized)
        {
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "Please authenticate to view public templates");
            return;
        }

        ImGui.PushItemWidth(300);
        if (ImGui.InputText("Search Templates", ref _searchText, 100)) _searchDelay = SearchDelayDuration;

        ImGui.PopItemWidth();

        ImGui.SameLine();
        if (ImGui.Button("Refresh") && !_isLoadingBackground) LoadTemplates();

        if (_isLoadingBackground)
        {
            ImGui.SameLine();
            ImGui.Text($"Loading... (Page {_currentPage})");
        }

        ShowError(_errorMessage);

        if (_filteredTemplates.Count > 0)
        {
            if (ImGui.Button("Previous") && _paginationInfo.HasPreviousPage)
            {
                _currentPage = _paginationInfo.PreviousPage;
                LoadTemplates(false);
            }

            ImGui.SameLine();
            ImGui.Text($"Page {_currentPage} of {_paginationInfo.LastPage}");

            ImGui.SameLine();
            if (ImGui.Button("Next") && _paginationInfo.HasNextPage)
            {
                _currentPage = _paginationInfo.NextPage;
                LoadTemplates(false);
            }

            ImGui.SameLine();
            var spacing = ImGui.GetStyle().ItemSpacing.X * 4;
            ImGui.Dummy(new Vector2(spacing, 0));
            ImGui.SameLine();

            ImGui.Text($"Total templates: {_paginationInfo.TotalItems}");
            ImGui.Separator();
        }

        if (_searchDelay > 0)
        {
            _searchDelay -= ImGui.GetIO().DeltaTime;
            if (_searchDelay <= 0) FilterTemplates();
        }

        if (_filteredTemplates.Count > 0)
            DrawTemplatesTable();
        else if (!_initialLoadComplete || _isLoadingBackground)
            ImGui.Text("Loading templates...");
        else if (!string.IsNullOrEmpty(_searchText))
            ImGui.Text("No templates found matching your search");
        else
            ImGui.Text(
                $"No public templates found for '{ItemFilterLibraryDatabase.Main.Settings.CurrentTemplateType?.Description ?? "selected type"}'");

        _templateModal.Draw();
    }

    private void DrawTemplatesTable()
    {
        var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable |
                    ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable;

        if (ImGui.BeginTable("public_templates", 6, flags))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Author", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 130);
            ImGui.TableSetupColumn("Public", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("Updated", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed,
                100);
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

            foreach (var template in _filteredTemplates)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(template.Name);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(template.CreatorName);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(template.IsPublic ? "Yes" : "No");

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(FormatTimeAgo(template.UpdatedAt));

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(template.Version.ToString());

                ImGui.TableNextColumn();
                ImGui.PushID($"actions_{template.TemplateId}");

                var buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
                if (ImGui.Button("View##", new Vector2(buttonWidth, 0)))
                    _templateModal.Show(template, TemplateModalMode.View);

                ImGui.SameLine();
                if (ImGui.Button("Copy##", new Vector2(buttonWidth, 0))) CopyTemplateContent(template.TemplateId);

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    private void FilterTemplates()
    {
        if (string.IsNullOrEmpty(_searchText))
            _filteredTemplates = _allTemplates.ToList();
        else
            _filteredTemplates = _allTemplates.Where(template => FuzzyMatcher.FuzzyMatch(_searchText, template.Name))
                .OrderByDescending(template => FuzzyMatcher.GetMatchScore(_searchText, template.Name)).ToList();

        ApplySort();
    }

    private void ApplySort()
    {
        var query = _filteredTemplates.AsQueryable();
        query = _sortState.Column switch
        {
            SortColumn.Name => _sortState.Ascending
                ? query.OrderBy(t => t.Name)
                : query.OrderByDescending(t => t.Name),

            SortColumn.Author => _sortState.Ascending
                ? query.OrderBy(t => t.CreatorName)
                : query.OrderByDescending(t => t.CreatorName),

            SortColumn.Public => _sortState.Ascending
                ? query.OrderBy(t => t.IsPublic)
                : query.OrderByDescending(t => t.IsPublic),

            SortColumn.Updated => _sortState.Ascending
                ? query.OrderBy(t => long.Parse(t.UpdatedAt))
                : query.OrderByDescending(t => long.Parse(t.UpdatedAt)),

            SortColumn.Version => _sortState.Ascending
                ? query.OrderBy(t => t.Version)
                : query.OrderByDescending(t => t.Version),

            _ => query
        };
        _filteredTemplates = query.ToList();
    }

    private string FormatTimeAgo(string unixTimeStr)
    {
        if (!long.TryParse(unixTimeStr, out var unixTime)) return "Invalid date";

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
        if (ApiClient.IsInitialized) LoadTemplates();
    }

    private async void LoadTemplates(bool resetCache = true)
    {
        if (_isLoadingBackground) return;

        try
        {
            _isLoadingBackground = true;
            _errorMessage = string.Empty;

            var currentType = Plugin.Settings.CurrentTemplateType;
            if (currentType == null)
            {
                _errorMessage = "No template type selected";
                return;
            }

            if (resetCache)
            {
                _allTemplates.Clear();
                _currentPage = 1;
            }

            var response =
                await ApiClient.GetAsync<PublicTemplateListResponse>(
                    Routes.Templates.GetAllTemplates(currentType.TypeId, _currentPage));

            if (response?.Templates != null)
            {
                _allTemplates.Clear();
                _allTemplates.AddRange(response.Templates);
                _paginationInfo = response.Pagination;
                _initialLoadComplete = true;
                FilterTemplates();
            }
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: {ex.Message}";
            if (resetCache)
            {
                _allTemplates.Clear();
                _filteredTemplates.Clear();
            }
        }
        finally
        {
            _isLoadingBackground = false;
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

            var response =
                await ApiClient.GetAsync<ApiResponse<TemplateDetailed>>(
                    Routes.Templates.GetTemplate(currentType.TypeId, templateId, true));

            if (response?.Data?.LatestVersion?.Content != null)
                ImGui.SetClipboardText(response.Data.LatestVersion.Content.ToString());
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

    public override void CloseModals()
    {
        _templateModal?.Close();
    }

    private enum SortColumn
    {
        Name = 0,
        Author = 1,
        Public = 2,
        Updated = 3,
        Version = 4
    }

    private class SortState
    {
        public SortColumn Column { get; set; }
        public bool Ascending { get; set; }
    }
}