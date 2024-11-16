using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.UI;
using System.Collections.Generic;
using System.Numerics;

namespace ItemFilterLibraryDatabase.Areas;

public class MyTemplatesArea : BaseArea
{
    private readonly TemplateModal _templateModal;
    private List<TemplateInfo> _displayedTemplates = new(); // New field for display
    private string _errorMessage = string.Empty;
    private bool _hasNewData = false;
    private bool _isRefreshing = false;
    private List<TemplateInfo> _templates = new();

    public MyTemplatesArea(ItemFilterLibraryDatabase plugin, ApiClient apiClient) : base(plugin, apiClient)
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

    public override string Name => "My Templates";

    public override void Draw()
    {
        // Update display data only when new data is ready
        if (_hasNewData)
        {
            _displayedTemplates = new List<TemplateInfo>(_templates);
            _hasNewData = false;
        }

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

        ShowError(_errorMessage);

        if (_displayedTemplates.Count > 0)
        {
            DrawTemplatesTable();
        }
        else
        {
            ImGui.Text($"No templates found for {GetTemplateTypeDisplayName()}");
        }

        _templateModal.Draw();
    }

    private void DrawTemplatesTable()
    {
        if (ImGui.BeginTable("my_templates", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Version");
            ImGui.TableSetupColumn("Updated");
            ImGui.TableSetupColumn("Public");
            ImGui.TableSetupColumn("Status");
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 200f); // Fixed width for actions
            ImGui.TableHeadersRow();

            foreach (var template in _templates)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(template.Name);

                ImGui.TableNextColumn();
                ImGui.Text(template.Version.ToString());

                ImGui.TableNextColumn();
                ImGui.Text(Plugin.UnixTimeToString(template.UpdatedAt));

                ImGui.TableNextColumn();
                ImGui.Text(template.IsPublic
                    ? "Yes"
                    : "No");

                ImGui.TableNextColumn();
                ImGui.Text(template.IsActive
                    ? "Active"
                    : "Inactive");

                ImGui.TableNextColumn();
                ImGui.PushID($"actions_{template.TemplateId}");

                if (ImGui.Button("Edit##"))
                {
                    _templateModal.Show(template, TemplateModalMode.Edit);
                }

                ImGui.SameLine();
                if (ImGui.Button("Copy##"))
                {
                    CopyTemplateContent(template.TemplateId);
                }

                ImGui.SameLine();
                if (ImGui.Button("Delete##"))
                {
                    // TODO: Add confirmation dialog
                    DeleteTemplate(template.TemplateId);
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

            if (response?.Data?.Versions != null && response.Data.Versions.Count > 0)
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
}