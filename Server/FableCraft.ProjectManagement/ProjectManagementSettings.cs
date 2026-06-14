namespace FableCraft.ProjectManagement;

internal sealed class ProjectManagementSettings
{
    public const string SectionName = "ProjectManagement";

    /// <summary>
    /// Container-internal path for project file storage (e.g., "/app/project-files" in Docker).
    /// Used for file I/O inside the server container.
    /// </summary>
    public string FilesPath { get; set; } = "./project-files";

    /// <summary>
    /// Host-side absolute path for Docker volume mount sources.
    /// Only used for volume mount configuration in GraphContainerRegistry.
    /// NOT used for file I/O — that always uses FilesPath.
    /// </summary>
    public string? FilesHostPath { get; set; }

    /// <summary>
    /// Gets the host-side files path for Docker volume mount sources.
    /// Returns the absolute host path if configured, otherwise falls back to the relative path.
    /// </summary>
    public string GetEffectiveFilesPath() => FilesHostPath ?? FilesPath;

    /// <summary>
    /// Gets the container-internal files path for file I/O.
    /// Always returns FilesPath, never the host path.
    /// </summary>
    public string GetFilesContainerPath() => FilesPath;
}