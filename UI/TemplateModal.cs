using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ItemFilterLibraryDatabase.UI;

public enum TemplateModalMode
{
    View,
    Edit,
    Create
}

public class TemplateModal(ItemFilterLibraryDatabase plugin, ApiClient apiClient)
{
    private string _changeNotes = string.Empty;
    private string _content = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isOpen;
    private bool _isPublic;
    private TemplateModalMode _mode;
    private string _name = string.Empty;
    private int _selectedVersionIndex = 0;
    private TemplateInfo _template;
    private List<TemplateVersion> _versions = [];

    public void Show(TemplateInfo template, TemplateModalMode mode)
    {
        try
        {
            _template = template;
            _mode = mode;
            _isOpen = true;
            _errorMessage = string.Empty;
            _changeNotes = string.Empty;
            _versions.Clear();
            _selectedVersionIndex = 0;

            if (mode == TemplateModalMode.Create)
            {
                _name = string.Empty;
                _content = string.Empty;
                _isPublic = true;
            }
            else
            {
                _name = template.Name;
                _isPublic = template.IsPublic;
                LoadTemplateContent();
            }
        }
        catch (Exception ex)
        {
            plugin.LogError($"Error in Show: {ex}", 30);
        }
    }

    public void Draw()
    {
        if (!_isOpen) return;

        try
        {
            var title = _mode switch
            {
                TemplateModalMode.View => "View Template",
                TemplateModalMode.Edit => "Edit Template",
                TemplateModalMode.Create => "Create Template",
                _ => throw new ArgumentOutOfRangeException()
            };

            ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);

            if (ImGui.Begin(title, ref _isOpen, ImGuiWindowFlags.Modal))
            {
                DrawHeader();
                DrawContent();
                DrawFooter();
            }

            ImGui.End();
        }
        catch (Exception ex)
        {
            plugin.LogError($"Error in Draw: {ex}", 30);
            _isOpen = false;
        }
    }

    private void DrawHeader()
    {
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), _errorMessage);
            ImGui.Separator();
        }

        if (_mode != TemplateModalMode.View)
        {
            ImGui.InputText("Name", ref _name, 100);

            if (_mode == TemplateModalMode.Edit)
            {
                ImGui.InputText("Change Notes", ref _changeNotes, 200);
            }
        }
        else
        {
            ImGui.Text($"Name: {_name}");
            ImGui.Text($"Author: {_template.CreatorName}");
            ImGui.Text($"Version: {_template.Version}");
            ImGui.Text($"Last Updated: {plugin.UnixTimeToString(_template.UpdatedAt)}");
        }

        // Show versions for both View and Edit modes
        if (_versions.Count > 0)
        {
            var items = _versions.Select(v =>
                $"Version {v.VersionNumber}: {(string.IsNullOrEmpty(v.ChangeNotes) ? "No change notes" : v.ChangeNotes)} ({plugin.UnixTimeToString(v.CreatedAt.ToString())})").ToArray();

            if (ImGui.Combo("Version History", ref _selectedVersionIndex, items, items.Length))
            {
                _content = _versions[_selectedVersionIndex].Content;
            }
        }

        ImGui.Separator();
    }

    private void DrawContent()
    {
        var flags = _mode == TemplateModalMode.View
            ? ImGuiInputTextFlags.ReadOnly
            : ImGuiInputTextFlags.None;

        ImGui.InputTextMultiline("##content", ref _content, 100000, new Vector2(-1, -50), flags);
    }

    private void DrawFooter()
    {
        ImGui.Separator();

        if (_mode == TemplateModalMode.View)
        {
            if (ImGui.Button("Copy to Clipboard"))
            {
                ImGui.SetClipboardText(_content);
            }

            ImGui.SameLine();
        }
        else
        {
            if (ImGui.Button("Save"))
            {
                SaveTemplate();
            }

            ImGui.SameLine();
        }

        if (ImGui.Button("Close"))
        {
            _isOpen = false;
        }
    }

    private async void LoadTemplateContent()
    {
        try
        {
            plugin.IsLoading = true;
            _errorMessage = string.Empty;

            var response = await apiClient.GetAsync<ApiResponse<TemplateInfo>>(Routes.Templates.GetTemplate(plugin.Settings.SelectedTemplateType,
                _template.TemplateId,
                true // Always include versions
            ));

            if (response?.Data != null)
            {
                _versions = response.Data.Versions ?? [];
                if (_versions.Count > 0)
                {
                    var latestVersion = _versions.OrderByDescending(v => v.VersionNumber).First();
                    _content = latestVersion.Content;
                    _selectedVersionIndex = 0;
                }

                _isPublic = response.Data.IsPublic;
            }
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: Failed to load template - {ex.Message}";
            _content = string.Empty;
        }
        catch (Exception ex)
        {
            plugin.LogError($"Error in LoadTemplateContent: {ex}", 30);
            _errorMessage = "An unexpected error occurred while loading the template";
            _content = string.Empty;
        }
        finally
        {
            plugin.IsLoading = false;
        }
    }

    private async void SaveTemplate()
    {
        try
        {
            plugin.IsLoading = true;
            _errorMessage = string.Empty;

            if (_mode == TemplateModalMode.Create)
            {
                var createRequest = new Routes.Templates.RequestBodies.CreateTemplateRequest
                {
                    Name = _name,
                    Content = _content,
                };

                await apiClient.PostAsync<ApiResponse<object>>(Routes.Templates.CreateTemplate(plugin.Settings.SelectedTemplateType), createRequest);
            }
            else
            {
                var updateRequest = new Routes.Templates.RequestBodies.UpdateTemplateRequest
                {
                    Name = _name,
                    Content = _content,
                    ChangeNotes = string.IsNullOrEmpty(_changeNotes)
                        ? "Updated via plugin"
                        : _changeNotes
                };

                await apiClient.PutAsync<ApiResponse<object>>(Routes.Templates.UpdateTemplate(plugin.Settings.SelectedTemplateType, _template.TemplateId), updateRequest);
            }

            _isOpen = false;
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: Failed to save template - {ex.Message}";
        }
        catch (Exception ex)
        {
            plugin.LogError($"Error in SaveTemplate: {ex}", 30);
            _errorMessage = "An unexpected error occurred while saving the template";
        }
        finally
        {
            plugin.IsLoading = false;
        }
    }

    public void Close()
    {
        _isOpen = false;
    }
}