using ImGuiNET;
using ItemFilterLibraryDatabase.Api;
using System.Numerics;

namespace ItemFilterLibraryDatabase.Areas;

public abstract class BaseArea(ItemFilterLibraryDatabase plugin, ApiClient apiClient) : IArea
{
    protected readonly ApiClient ApiClient = apiClient;
    protected readonly ItemFilterLibraryDatabase Plugin = plugin;

    public abstract string Name { get; }

    public abstract void Draw();
    public abstract void RefreshData(); // Added abstract method

    public virtual void CloseModals()
    {
    }

    protected void ShowError(string error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), error);
        }
    }
}