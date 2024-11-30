using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using ItemFilterLibraryDatabase.Api.Models;
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

public class TemplateModal
{
    private readonly ApiClient _apiClient;
    private readonly ItemFilterLibraryDatabase _plugin;
    private string _content = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isOpen;
    private bool _isPublic;
    private TemplateModalMode _mode;
    private string _name = string.Empty;
    private int _selectedVersionIndex = 0;
    private Template _template;
    private List<TemplateVersion> _versions = [];

    public TemplateModal(ItemFilterLibraryDatabase plugin, ApiClient apiClient)
    {
        _plugin = plugin;
        _apiClient = apiClient;
    }

    public void Show(Template template, TemplateModalMode mode)
    {
        try
        {
            _template = template;
            _mode = mode;
            _isOpen = true;
            _errorMessage = string.Empty;
            _versions.Clear();
            _selectedVersionIndex = 0;

            if (mode == TemplateModalMode.Create)
            {
                _name = string.Empty;
                _content = string.Empty;
                _isPublic = false;
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
            _plugin.LogError($"Error in Show: {ex}", 30);
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
            _plugin.LogError($"Error in Draw: {ex}", 30);
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
        }
        else
        {
            ImGui.Text($"Name: {_name}");
            ImGui.Text($"Author: {_template.CreatorName}");
            ImGui.Text($"Version: {_template.Version}");
            ImGui.Text($"Last Updated: {_plugin.UnixTimeToString(_template.UpdatedAt)}");
        }

        if (_versions.Count > 0)
        {
            var items = _versions.Select(v => $"Version {v.VersionNumber} ({_plugin.UnixTimeToString(v.CreatedAt.ToString())})").ToArray();

            if (ImGui.Combo("Version History", ref _selectedVersionIndex, items, items.Length))
            {
                _content = _versions[_selectedVersionIndex].Content.ToString();
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
            _plugin.IsLoading = true;
            _errorMessage = string.Empty;

            var currentType = _plugin.Settings.CurrentTemplateType;
            if (currentType == null)
            {
                _errorMessage = "No template type selected";
                return;
            }

            var response = await _apiClient.GetAsync<ApiResponse<TemplateDetailed>>(Routes.Templates.GetTemplate(currentType.TypeId, _template.TemplateId, true));

            if (response?.Data != null)
            {
                _versions = response.Data.Versions ?? [];
                if (_versions.Count > 0)
                {
                    _content = _versions[0].Content.ToString();
                    _selectedVersionIndex = 0;
                }

                _isPublic = response.Data.IsPublic;
            }
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: Failed to load Template - {ex.Message}";
            _content = string.Empty;
        }
        finally
        {
            _plugin.IsLoading = false;
        }
    }

    private async void SaveTemplate()
    {
        try
        {
            _plugin.IsLoading = true;
            _errorMessage = string.Empty;

            var currentType = _plugin.Settings.CurrentTemplateType;
            if (currentType == null)
            {
                _errorMessage = "No template type selected";
                return;
            }

            if (_mode == TemplateModalMode.Create)
            {
                var createRequest = new Routes.Templates.RequestBodies.CreateTemplateRequest
                {
                    Name = _name,
                    Content = _content
                };

                await _apiClient.PostAsync<ApiResponse<TemplateDetailed>>(Routes.Templates.CreateTemplate(currentType.TypeId), createRequest);
            }
            else
            {
                var updateRequest = new Routes.Templates.RequestBodies.UpdateTemplateRequest
                {
                    Name = _name,
                    Content = _content
                };

                await _apiClient.PutAsync<ApiResponse<TemplateDetailed>>(Routes.Templates.UpdateTemplate(currentType.TypeId, _template.TemplateId), updateRequest);
            }

            _isOpen = false;
        }
        catch (ApiException ex)
        {
            _errorMessage = $"Error: Failed to save Template - {ex.Message}";
        }
        finally
        {
            _plugin.IsLoading = false;
        }
    }

    public void Close()
    {
        _isOpen = false;
    }
}