namespace FableCraft.Infrastructure.Docker;

/// <summary>
/// Manages Docker volumes for knowledge graph isolation.
/// </summary>
internal interface IVolumeManager
{
    /// <summary>
    /// Creates a new Docker volume.
    /// </summary>
    /// <param name="volumeName">Name of the volume to create.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateAsync(string volumeName, CancellationToken ct = default);

    /// <summary>
    /// Copies contents from one volume to another.
    /// </summary>
    /// <param name="source">Source volume name.</param>
    /// <param name="destination">Destination volume name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CopyAsync(string source, string destination, CancellationToken ct = default);

    /// <summary>
    /// Deletes a Docker volume.
    /// </summary>
    /// <param name="volumeName">Name of the volume to delete.</param>
    /// <param name="force">Force deletion even if in use.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string volumeName, bool force = false, CancellationToken ct = default);

    /// <summary>
    /// Checks if a volume exists.
    /// </summary>
    /// <param name="volumeName">Name of the volume to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the volume exists.</returns>
    Task<bool> ExistsAsync(string volumeName, CancellationToken ct = default);

    /// <summary>
    /// Exports a volume to a tar file.
    /// </summary>
    /// <param name="volumeName">Name of the volume to export.</param>
    /// <param name="outputDirectory">Directory to write the tar file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Path to the created tar file.</returns>
    Task<string> ExportAsync(string volumeName, string outputDirectory, CancellationToken ct = default);

    /// <summary>
    /// Imports a tar file into a volume.
    /// </summary>
    /// <param name="tarPath">Path to the tar file.</param>
    /// <param name="volumeName">Name of the volume to import into.</param>
    /// <param name="overwrite">If true, clears existing volume contents.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ImportAsync(string tarPath, string volumeName, bool overwrite = false, CancellationToken ct = default);
}
