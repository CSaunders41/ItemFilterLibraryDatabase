using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ItemFilterLibraryDatabase.Areas;

public class FuzzyMatcher
{
    public static int LevenshteinDistance(string s1, string s2)
    {
        var costs = new int[s2.Length + 1];
        for (var i = 0; i <= s1.Length; i++)
        {
            var lastValue = i;
            for (var j = 0; j <= s2.Length; j++)
            {
                if (i == 0)
                {
                    costs[j] = j;
                }
                else if (j > 0)
                {
                    var newValue = costs[j - 1];
                    if (s1[i - 1] != s2[j - 1])
                    {
                        newValue = Math.Min(Math.Min(newValue, lastValue), costs[j]) + 1;
                    }

                    costs[j - 1] = lastValue;
                    lastValue = newValue;
                }
            }

            if (i > 0) costs[s2.Length] = lastValue;
        }

        return costs[s2.Length];
    }

    public static bool FuzzyMatch(string pattern, string input, double threshold = 0.7)
    {
        if (string.IsNullOrEmpty(pattern)) return true;
        if (string.IsNullOrEmpty(input)) return false;

        pattern = pattern.ToLower();
        input = input.ToLower();

        // Direct substring match gets highest priority
        if (input.Contains(pattern)) return true;

        // Calculate Levenshtein distance
        var distance = LevenshteinDistance(pattern, input);
        var similarity = 1 - (double)distance / Math.Max(pattern.Length, input.Length);

        return similarity >= threshold;
    }
}

public class PaginationInfo
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasMore { get; set; }
}

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; }
    public PaginationInfo Pagination { get; set; }
}

public class PublicTemplatesArea : BaseArea
{
    private const float SearchDelayDuration = 0.5f;
    private const int ItemsPerPage = 20;
    private const int PageSize = 100;
    private readonly List<TemplateInfo> _allTemplates = [];
    private readonly SortState _sortState = new() {Column = SortColumn.Updated, Ascending = false};

    private readonly TemplateModal _templateModal;
    private int _currentPage = 1;
    private string _errorMessage = string.Empty;
    private List<TemplateInfo> _filteredTemplates = [];
    private bool _initialLoadComplete = false;
    private bool _isLoadingBackground = false;
    private int _loadedPages = 0;
    private float _searchDelay = 0;
    private string _searchText = string.Empty;
    private int _totalPages = 1;

    public PublicTemplatesArea(ItemFilterLibraryDatabase plugin, ApiClient apiClient) : base(plugin, apiClient)
    {
        _templateModal = new TemplateModal(plugin, apiClient);
        StartBackgroundLoad();
    }

    public override string Name => "Public Templates";

    public override void Draw()
    {
        // Top controls section
        ImGui.PushItemWidth(300);
        if (ImGui.InputText("Search Templates", ref _searchText, 100))
        {
            _searchDelay = SearchDelayDuration;
        }

        ImGui.PopItemWidth();

        ImGui.SameLine();
        if (ImGui.Button("Refresh") && !_isLoadingBackground)
        {
            StartBackgroundLoad();
        }

        // Show loading progress
        if (_isLoadingBackground)
        {
            ImGui.SameLine();
            ImGui.Text($"Loading... ({_loadedPages}/{_totalPages} pages)");
        }

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
        else if (!_initialLoadComplete)
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

        if (ImGui.BeginTable("public_templates", 5, flags))
        {
            // Setup columns
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Author", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 130);
            ImGui.TableSetupColumn("Updated", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Version", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthFixed, 160);
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
                ImGui.Text(template.DiscordId);

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
            SortColumn.Author => _sortState.Ascending
                ? query.OrderBy(t => t.DiscordId)
                : query.OrderByDescending(t => t.DiscordId),
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

    private async void StartBackgroundLoad()
    {
        if (_isLoadingBackground) return;

        try
        {
            _isLoadingBackground = true;
            _errorMessage = string.Empty;
            _allTemplates.Clear();
            _loadedPages = 0;
            _initialLoadComplete = false;

            var currentPage = 1;
            bool hasMore;

            do
            {
                var response = await ApiClient.GetAsync<PaginatedResponse<TemplateInfo>>(Routes.Templates.PublicTemplates(Plugin.Settings.SelectedTemplateType.Value, currentPage, PageSize));

                if (response?.Data == null || response.Pagination == null)
                {
                    break;
                }

                _allTemplates.AddRange(response.Data);
                _loadedPages = currentPage;
                _totalPages = response.Pagination.TotalPages;

                // Set initial load complete after first page
                if (currentPage == 1)
                {
                    _initialLoadComplete = true;
                }

                FilterTemplates(); // Update results as new data comes in

                hasMore = response.Pagination.HasMore;
                currentPage++;
            } while (hasMore);
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: {ex.Message}";
            _allTemplates.Clear();
            _filteredTemplates.Clear();
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
        StartBackgroundLoad();
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