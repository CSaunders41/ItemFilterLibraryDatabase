using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.UI;
using System.Collections.Generic;

namespace ItemFilterLibraryDatabase.Areas;

public class PublicTemplatesArea : BaseArea
{
    private const int ItemsPerPage = 20;
    private readonly TemplateModal _templateModal;
    private int _currentPage = 1;
    private List<TemplateInfo> _displayedTemplates = new(); // New field for display
    private string _errorMessage = string.Empty;
    private bool _hasNewData = false;
    private bool _isRefreshing = false;
    private List<TemplateInfo> _templates = new();

    public PublicTemplatesArea(ItemFilterLibraryDatabase plugin, ApiClient apiClient) : base(plugin, apiClient)
    {
        _templateModal = new TemplateModal(plugin, apiClient);
        if (Plugin.Initialized)
        {
            RefreshTemplates();
        }
    }
    public override void CloseModals()
    {
        _templateModal?.Close();
    }

    public override string Name => "Public Templates";

    public override void Draw()
    {
        // Update display data only when new data is ready
        if (_hasNewData)
        {
            _displayedTemplates = new List<TemplateInfo>(_templates);
            _hasNewData = false;
        }

        if (ImGui.Button("Refresh Templates") && !_isRefreshing)
        {
            RefreshTemplates();
        }

        ShowError(_errorMessage);

        if (_displayedTemplates.Count > 0)
        {
            DrawTemplatesTable();
            DrawPagination();
        }
        else
        {
            ImGui.Text($"No public templates found for {GetTemplateTypeDisplayName()}");
        }

        _templateModal.Draw();
    }

    private void DrawTemplatesTable()
    {
        if (ImGui.BeginTable("public_templates", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Author");
            ImGui.TableSetupColumn("Updated");
            ImGui.TableSetupColumn("Version");
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 160f); // Fixed width for actions
            ImGui.TableHeadersRow();

            foreach (var template in _templates)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(template.Name);

                ImGui.TableNextColumn();
                ImGui.Text(template.DiscordId);

                ImGui.TableNextColumn();
                ImGui.Text(Plugin.UnixTimeToString(template.UpdatedAt));

                ImGui.TableNextColumn();
                ImGui.Text(template.Version.ToString());

                ImGui.TableNextColumn();
                ImGui.PushID($"actions_{template.TemplateId}");

                if (ImGui.Button("View##"))
                {
                    _templateModal.Show(template, TemplateModalMode.View);
                }

                ImGui.SameLine();
                if (ImGui.Button("Copy##"))
                {
                    CopyTemplateContent(template.TemplateId);
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
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

    private void DrawPagination()
    {
        if (ImGui.Button("Previous") && _currentPage > 1)
        {
            _currentPage--;
            RefreshTemplates();
        }

        ImGui.SameLine();
        ImGui.Text($"Page {_currentPage}");

        ImGui.SameLine();
        if (ImGui.Button("Next") && _templates.Count == ItemsPerPage)
        {
            _currentPage++;
            RefreshTemplates();
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
        _currentPage = 1; // Reset to first page when refreshing
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

            var response = await ApiClient.GetAsync<ApiResponse<List<TemplateInfo>>>(Routes.Templates.PublicTemplates(Plugin.Settings.SelectedTemplateType.Value, _currentPage));

            _templates = response.Data;
            _hasNewData = true;
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: {ex.Message}";
            _templates.Clear();
            _hasNewData = true;
        }
        finally
        {
            _isRefreshing = false;
            Plugin.IsLoading = false;
        }
    }
}