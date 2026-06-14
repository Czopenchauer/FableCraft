using System.ComponentModel;

using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.ProjectManagement.Plugins;

internal sealed class ProjectFilePlugin
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private readonly string _basePath;
    private Guid _projectId;

    public ProjectFilePlugin(ApplicationDbContext dbContext, ILogger logger, Microsoft.Extensions.Options.IOptions<ProjectManagementSettings> settings)
    {
        _dbContext = dbContext;
        _logger = logger;
        _basePath = settings.Value.GetFilesContainerPath();
    }

    public void SetProjectId(Guid projectId) => _projectId = projectId;

    [KernelFunction("create_file")]
    [Description("Creates a new file in the project. Use this to store worldbuilding content like lore, characters, locations, events, or items.")]
    public async Task<string> CreateFileAsync(
        [Description("The name of the file, e.g. 'elven-history.md'")] string name,
        [Description("The full content of the file")] string content,
        [Description("The category of the file: lore, characters, locations, events, or items")] string category)
    {
        _logger.Information("Creating file {FileName} in project {ProjectId}", name, _projectId);

        var contentHash = ComputeHash(content);

        var file = new ProjectFile
        {
            Id = Guid.NewGuid(),
            ProjectId = _projectId,
            Name = name,
            Category = category,
            ContentHash = contentHash,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ProjectFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        await WriteFileToDiskAsync(file.Id, content);

        return $"File '{name}' created successfully with ID {file.Id}.";
    }

    [KernelFunction("read_file")]
    [Description("Reads the full content of a file. Always read a file before editing it to get the exact current text.")]
    public async Task<string> ReadFileAsync(
        [Description("The ID of the file to read")] string file_id)
    {
        if (!Guid.TryParse(file_id, out var fileId))
        {
            return $"Invalid file ID: {file_id}";
        }

        var file = await _dbContext.ProjectFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.ProjectId == _projectId);

        if (file is null)
        {
            return $"File with ID {file_id} not found.";
        }

        var content = await ReadFileFromDiskAsync(file.Id);

        return $"--- {file.Name} ({file.Category ?? "uncategorized"}) ---\n\n{content}";
    }

    [KernelFunction("edit_file")]
    [Description("Edits a file by replacing exact text. Always read the file first to get the exact text, then specify the old_text to find and new_text to replace it with.")]
    public async Task<string> EditFileAsync(
        [Description("The ID of the file to edit")] string file_id,
        [Description("The exact text to find in the file content")] string old_text,
        [Description("The text to replace the old_text with")] string new_text)
    {
        if (!Guid.TryParse(file_id, out var fileId))
        {
            return $"Invalid file ID: {file_id}";
        }

        var file = await _dbContext.ProjectFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.ProjectId == _projectId);

        if (file is null)
        {
            return $"File with ID {file_id} not found.";
        }

        var content = await ReadFileFromDiskAsync(file.Id);
        var occurrences = content.Split(old_text).Length - 1;

        if (occurrences == 0)
        {
            return "Text not found. Re-read the file and try with the exact text.";
        }

        if (occurrences > 1)
        {
            return $"Found {occurrences} matches. Provide more surrounding context to make it unique.";
        }

        var newContent = content.Replace(old_text, new_text);
        file.ContentHash = ComputeHash(newContent);
        file.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        await WriteFileToDiskAsync(file.Id, newContent);

        return $"File '{file.Name}' updated successfully.";
    }

    [KernelFunction("delete_file")]
    [Description("Deletes a file from the project.")]
    public async Task<string> DeleteFileAsync(
        [Description("The ID of the file to delete")] string file_id)
    {
        if (!Guid.TryParse(file_id, out var fileId))
        {
            return $"Invalid file ID: {file_id}";
        }

        var file = await _dbContext.ProjectFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.ProjectId == _projectId);

        if (file is null)
        {
            return $"File with ID {file_id} not found.";
        }

        _dbContext.ProjectFiles.Remove(file);
        await _dbContext.SaveChangesAsync();
        DeleteFileFromDisk(file.Id);

        return $"File '{file.Name}' deleted successfully.";
    }

    [KernelFunction("list_files")]
    [Description("Lists all files in the project, optionally filtered by category.")]
    public async Task<string> ListFilesAsync(
        [Description("Optional category filter: lore, characters, locations, events, or items")] string? category = null)
    {
        var query = _dbContext.ProjectFiles
            .Where(f => f.ProjectId == _projectId);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(f => f.Category == category);
        }

        var files = await query
            .OrderBy(f => f.Name)
            .Select(f => new { f.Id, f.Name, f.Category })
            .ToListAsync();

        if (files.Count == 0)
        {
            return category is null
                ? "No files in this project yet."
                : $"No files found in category '{category}'.";
        }

        var lines = files.Select(f =>
            string.IsNullOrEmpty(f.Category)
                ? $"- {f.Name} (ID: {f.Id})"
                : $"- {f.Name} [{f.Category}] (ID: {f.Id})");

        return string.Join("\n", lines);
    }

    private string GetProjectDir() => Path.Combine(_basePath, _projectId.ToString());

    private string GetFilePath(Guid fileId) => Path.Combine(GetProjectDir(), $"{fileId}.md");

    private async Task<string> ReadFileFromDiskAsync(Guid fileId)
    {
        var filePath = GetFilePath(fileId);
        if (!File.Exists(filePath)) return string.Empty;
        return await File.ReadAllTextAsync(filePath);
    }

    private async Task WriteFileToDiskAsync(Guid fileId, string content)
    {
        Directory.CreateDirectory(GetProjectDir());
        await File.WriteAllTextAsync(GetFilePath(fileId), content);
    }

    private void DeleteFileFromDisk(Guid fileId)
    {
        try
        {
            var filePath = GetFilePath(fileId);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to delete file {FileId} from disk", fileId);
        }
    }

    private static string ComputeHash(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = System.IO.Hashing.XxHash64.Hash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}