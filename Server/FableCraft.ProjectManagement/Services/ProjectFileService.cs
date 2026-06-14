using System.IO.Hashing;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Docker.Configuration;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.ProjectManagement.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Serilog;

namespace FableCraft.ProjectManagement.Services;

public interface IProjectFileService
{
    Task<IEnumerable<ProjectFileSummaryDto>> ListFilesAsync(Guid projectId, string? category, CancellationToken cancellationToken);
    Task<ProjectFileResponseDto?> GetFileAsync(Guid projectId, Guid fileId, CancellationToken cancellationToken);
    Task<ProjectFileResponseDto> CreateFileAsync(Guid projectId, ProjectFileDto dto, CancellationToken cancellationToken);
    Task<ProjectFileResponseDto?> UpdateFileAsync(Guid projectId, Guid fileId, ProjectFileUpdateDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteFileAsync(Guid projectId, Guid fileId, CancellationToken cancellationToken);
    Task IndexProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IndexingStatusResponse> GetIndexingStatusAsync(Guid projectId, CancellationToken cancellationToken);
}

internal sealed class ProjectFileService : IProjectFileService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRagClientFactory _ragClientFactory;
    private readonly GraphServiceSettings _graphServiceSettings;
    private readonly ProjectManagementSettings _settings;
    private readonly ILogger _logger;

    public ProjectFileService(
        ApplicationDbContext dbContext,
        IRagClientFactory ragClientFactory,
        IOptions<GraphServiceSettings> graphServiceSettings,
        IOptions<ProjectManagementSettings> settings,
        ILogger logger)
    {
        _dbContext = dbContext;
        _ragClientFactory = ragClientFactory;
        _graphServiceSettings = graphServiceSettings.Value;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<ProjectFileSummaryDto>> ListFilesAsync(Guid projectId, string? category, CancellationToken cancellationToken)
    {
        var query = _dbContext.ProjectFiles
            .Where(f => f.ProjectId == projectId);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(f => f.Category == category);
        }

        return await query
            .OrderBy(f => f.Name)
            .Select(f => new ProjectFileSummaryDto
            {
                Id = f.Id,
                Name = f.Name,
                Category = f.Category,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectFileResponseDto?> GetFileAsync(Guid projectId, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await _dbContext.ProjectFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.ProjectId == projectId, cancellationToken);

        if (file is null) return null;

        var content = await ReadFileContentAsync(file.Id, projectId, cancellationToken);

        return new ProjectFileResponseDto
        {
            Id = file.Id,
            ProjectId = file.ProjectId,
            Name = file.Name,
            Content = content,
            Category = file.Category,
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt
        };
    }

    public async Task<ProjectFileResponseDto> CreateFileAsync(Guid projectId, ProjectFileDto dto, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.FindAsync([projectId], cancellationToken)
                      ?? throw new InvalidOperationException($"Project {projectId} not found");

        var contentHash = ComputeHash(dto.Content);

        var file = new ProjectFile
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = dto.Name,
            Category = dto.Category,
            ContentHash = contentHash,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ProjectFiles.Add(file);
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await WriteFileContentAsync(file.Id, projectId, dto.Content, cancellationToken);

        return new ProjectFileResponseDto
        {
            Id = file.Id,
            ProjectId = file.ProjectId,
            Name = file.Name,
            Content = dto.Content,
            Category = file.Category,
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt
        };
    }

    public async Task<ProjectFileResponseDto?> UpdateFileAsync(Guid projectId, Guid fileId, ProjectFileUpdateDto dto, CancellationToken cancellationToken)
    {
        var file = await _dbContext.ProjectFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.ProjectId == projectId, cancellationToken);

        if (file is null) return null;

        file.ContentHash = ComputeHash(dto.Content);
        if (dto.Category is not null) file.Category = dto.Category;
        file.UpdatedAt = DateTimeOffset.UtcNow;

        var project = await _dbContext.Projects.FindAsync([projectId], cancellationToken);
        if (project is not null) project.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteFileContentAsync(file.Id, projectId, dto.Content, cancellationToken);

        return new ProjectFileResponseDto
        {
            Id = file.Id,
            ProjectId = file.ProjectId,
            Name = file.Name,
            Content = dto.Content,
            Category = file.Category,
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt
        };
    }

    public async Task<bool> DeleteFileAsync(Guid projectId, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await _dbContext.ProjectFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.ProjectId == projectId, cancellationToken);

        if (file is null) return false;

        _dbContext.ProjectFiles.Remove(file);
        await _dbContext.SaveChangesAsync(cancellationToken);

        DeleteFileFromDisk(fileId, projectId);

        return true;
    }

    public async Task IndexProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.FindAsync([projectId], cancellationToken)
                      ?? throw new InvalidOperationException($"Project {projectId} not found");

        if (project.IndexingStatus == IndexingStatus.Indexing)
        {
            throw new InvalidOperationException("Project is already being indexed");
        }

        project.IndexingStatus = IndexingStatus.Indexing;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var ragBuilder = await _ragClientFactory.CreateBuildClientForProject(projectId, cancellationToken);
            var dataStorePath = _graphServiceSettings.GetDataStoreContainerPath();
            var projectDir = Path.Combine(dataStorePath, projectId.ToString());
            Directory.CreateDirectory(projectDir);

            var files = await _dbContext.ProjectFiles
                .Where(f => f.ProjectId == projectId)
                .ToListAsync(cancellationToken);

            foreach (var file in files)
            {
                if (file.ContentHash == file.IndexedContentHash) continue;

                var content = await ReadFileContentAsync(file.Id, projectId, cancellationToken);
                var filePath = Path.Combine(projectDir, $"{file.ContentHash}.md");
                await File.WriteAllTextAsync(filePath, content, cancellationToken);

                var result = await ragBuilder.AddDataAsync([filePath], ["project"], cancellationToken);

                if (result.TryGetValue("data_id_map", out var idMap) && idMap.TryGetValue(filePath, out var nodeId))
                {
                    file.KnowledgeGraphNodeId = nodeId;
                }

                file.IndexedContentHash = file.ContentHash;
            }

            await ragBuilder.CognifyAsync(["project"], cancellationToken: cancellationToken);

            project.IndexingStatus = IndexingStatus.Indexed;
            project.LastIndexedAt = DateTimeOffset.UtcNow;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Indexing failed for project {ProjectId}", projectId);
            project.IndexingStatus = IndexingStatus.NeedsReindexing;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IndexingStatusResponse> GetIndexingStatusAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.FindAsync([projectId], cancellationToken)
                      ?? throw new InvalidOperationException($"Project {projectId} not found");

        var files = await _dbContext.ProjectFiles
            .Where(f => f.ProjectId == projectId)
            .Select(f => new { f.Id, f.Name, f.ContentHash, f.IndexedContentHash })
            .ToListAsync(cancellationToken);

        var pendingChanges = files.Select(f => new IndexingFileStatus
        {
            FileId = f.Id,
            FileName = f.Name,
            IsNew = f.IndexedContentHash is null,
            IsModified = f.IndexedContentHash is not null && f.ContentHash != f.IndexedContentHash
        }).Where(f => f.IsNew || f.IsModified).ToList();

        return new IndexingStatusResponse
        {
            Status = project.IndexingStatus,
            LastIndexedAt = project.LastIndexedAt,
            PendingChanges = pendingChanges
        };
    }

    private string GetProjectFilesPath(Guid projectId)
    {
        var basePath = _settings.GetFilesContainerPath();
        return Path.Combine(basePath, projectId.ToString());
    }

    private string GetFilePath(Guid fileId, Guid projectId)
    {
        return Path.Combine(GetProjectFilesPath(projectId), $"{fileId}.md");
    }

    private async Task<string> ReadFileContentAsync(Guid fileId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(fileId, projectId);
        if (!File.Exists(filePath)) return string.Empty;
        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }

    private async Task WriteFileContentAsync(Guid fileId, Guid projectId, string content, CancellationToken cancellationToken = default)
    {
        var projectDir = GetProjectFilesPath(projectId);
        Directory.CreateDirectory(projectDir);
        var filePath = Path.Combine(projectDir, $"{fileId}.md");
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
    }

    private void DeleteFileFromDisk(Guid fileId, Guid projectId)
    {
        try
        {
            var filePath = GetFilePath(fileId, projectId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to delete file {FileId} from disk for project {ProjectId}", fileId, projectId);
        }
    }

    private static string ComputeHash(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = XxHash64.Hash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}