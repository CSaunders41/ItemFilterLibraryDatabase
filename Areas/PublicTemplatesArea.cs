using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.UI;
using ItemFilterLibraryDatabase.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ItemFilterLibraryDatabase.Areas;

public class PublicTemplatesArea : BaseArea
{
    private const float SearchDelayDuration = 0.5f;
    private const int ItemsPerPage = 40;
    private readonly List<TemplateInfo> _allTemplates = [];
    private readonly SortState _sortState = new() { Column = SortColumn.Updated, Ascending = false };
    private readonly TemplateModal _templateModal;
    private int _currentPage = 1;
    private string _errorMessage = string.Empty;
    private List<TemplateInfo> _filteredTemplates = [];
    private bool _initialLoadComplete = false;
    private bool _isLoadingBackground = false;
    private int _totalPages = 1;
    private float _searchDelay = 0;
    private string _searchText = string.Empty;

    public PublicTemplatesArea(ItemFilterLibraryDatabase plugin, ApiClient apiClient) : base(plugin, apiClient)
    {
        _templateModal = new TemplateModal(plugin, apiClient);
        if (Plugin.Initialized && ApiClient.IsInitialized)
        {
            LoadTemplates();
        }
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
        if (ImGui.InputText("Search Templates", ref _searchText, 100))
        {
            _searchDelay = SearchDelayDuration;
        }

        ImGui.PopItemWidth();

        ImGui.SameLine();
        if (ImGui.Button("Refresh") && !_isLoadingBackground)
        {
            LoadTemplates();
        }

        if (_isLoadingBackground)
        {
            ImGui.SameLine();
            ImGui.Text($"Loading... ({_currentPage}/{_totalPages} pages)");
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
        else if (!_initialLoadComplete || _isLoadingBackground)
        {
            ImGui.Text("Loading templates...");
        }
        else if (!string.IsNullOrEmpty(_searchText))
        {
            ImGui.Text("No templates found matching your search");
        }
        else
        {
            ImGui.Text($"No public templates found for {GetTemplateTypeDisplayName()}");
        }

        _templateModal.Draw();
    }

    private void DrawTemplatesTable()
    {
        var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollY;

        if (ImGui.BeginTable("public_templates", 6, flags))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Author", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 130);
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
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(template.Name);

                ImGui.TableNextColumn();
                ImGui.Text(template.CreatorName);

                ImGui.TableNextColumn();
                ImGui.Text(template.IsPublic ? "Yes" : "No");

                ImGui.TableNextColumn();
                ImGui.Text(FormatTimeAgo(template.UpdatedAt));

                ImGui.TableNextColumn();
                ImGui.Text(template.Version.ToString());

                ImGui.TableNextColumn();
                ImGui.PushID($"actions_{template.TemplateId}");

                var buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
                if (ImGui.Button("View##", new Vector2(buttonWidth, 0)))
                {
                    _templateModal.Show(template, TemplateModalMode.View);
                }

                ImGui.SameLine();
                if (ImGui.Button("Copy##", new Vector2(buttonWidth, 0)))
                {
                    CopyTemplateContent(template.TemplateId);
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
            SortColumn.Author => _sortState.Ascending
                ? query.OrderBy(t => t.CreatorName)
                : query.OrderByDescending(t => t.CreatorName),
            SortColumn.Updated => _sortState.Ascending
                ? query.OrderBy(t => t.UpdatedAt)
                : query.OrderByDescending(t => t.UpdatedAt),
            SortColumn.Version => _sortState.Ascending
                ? query.OrderBy(t => t.Version)
                : query.OrderByDescending(t => t.Version),
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
        return Plugin.Settings.SelectedTemplateType switch
        {
            Routes.Types.ItemFilterLibrary => "Item Filter Library",
            Routes.Types.WheresMyCraftAt => "Where's My Craft At",
            _ => Plugin.Settings.SelectedTemplateType
        };
    }

    public override void RefreshData()
    {
        if (ApiClient.IsInitialized)
        {
            LoadTemplates();
        }
    }

    private async void LoadTemplates(bool resetCache = true)
    {
        if (_isLoadingBackground) return;

        try
        {
            _isLoadingBackground = true;
            _errorMessage = string.Empty;

            if (resetCache)
            {
                _allTemplates.Clear();
                _currentPage = 1;
            }

            var response = await ApiClient.GetAsync<ApiResponse<TemplateListResponse>>(
                Routes.Templates.GetAllTemplates(
                    Plugin.Settings.SelectedTemplateType,
                    _currentPage,
                    ItemsPerPage
                )
            );

            if (response?.Data?.Templates != null)
            {
                _allTemplates.AddRange(response.Data.Templates);
                _totalPages = response.Data.TotalPages;
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

            var response = await ApiClient.GetAsync<ApiResponse<TemplateDetailInfo>>(
                Routes.Templates.GetTemplate(Plugin.Settings.SelectedTemplateType, templateId, true)
            );

            if (response?.Data?.LatestVersion?.Content != null)
            {
                ImGui.SetClipboardText(response.Data.LatestVersion.Content.ToString());
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

    public override void CloseModals()
    {
        _templateModal?.Close();
    }

    private enum SortColumn
    {
        Name,
        Author,
        Updated,
        Version
    }

    private class SortState
    {
        public SortColumn Column { get; set; }
        public bool Ascending { get; set; }
    }
}