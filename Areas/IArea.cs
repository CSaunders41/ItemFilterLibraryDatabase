namespace ItemFilterLibraryDatabase.Areas;

public interface IArea
{
    string Name { get; }
    void Draw();
    void RefreshData();
}