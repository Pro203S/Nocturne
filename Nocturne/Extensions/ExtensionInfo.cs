namespace Nocturne.Extensions
{
    public sealed record ExtensionInfo(
        string Name,
        string Description,
        string Version,
        string FilePath,
        bool IsLoaded,
        string? Error);
}
