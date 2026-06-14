using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.ProjectManagement.Models;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.ProjectManagement.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectResponseDto>> GetAllProjectsAsync(CancellationToken cancellationToken);
    Task<ProjectResponseDto?> GetProjectAsync(Guid id, CancellationToken cancellationToken);
    Task<ProjectResponseDto> CreateProjectAsync(ProjectDto dto, CancellationToken cancellationToken);
    Task<ProjectResponseDto?> UpdateProjectAsync(Guid id, ProjectUpdateDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteProjectAsync(Guid id, CancellationToken cancellationToken);
}

internal sealed class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _dbContext;

    public ProjectService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetAllProjectsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Projects
            .Include(p => p.GraphRagSettings)
            .Include(p => p.LlmPreset)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Select(p => MapToResponse(p))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectResponseDto?> GetProjectAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects
            .Include(p => p.GraphRagSettings)
            .Include(p => p.LlmPreset)
            .Include(p => p.Files)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null) return null;

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            GraphRagSettingsId = project.GraphRagSettingsId,
            GraphRagSettingsName = project.GraphRagSettings?.Name,
            LlmPresetId = project.LlmPresetId,
            LlmPresetName = project.LlmPreset?.Name,
            IndexingStatus = (IndexingStatusDto)project.IndexingStatus,
            LastIndexedAt = project.LastIndexedAt,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Files = project.Files.Select(f => new ProjectFileSummaryDto
            {
                Id = f.Id,
                Name = f.Name,
                Category = f.Category,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            }).ToList()
        };
    }

    public async Task<ProjectResponseDto> CreateProjectAsync(ProjectDto dto, CancellationToken cancellationToken)
    {
        GraphRagSettings? graphRagSettings = null;
        LlmPreset? llmPreset = null;

        if (dto.GraphRagSettingsId.HasValue)
        {
            graphRagSettings = await _dbContext.GraphRagSettings.FindAsync([dto.GraphRagSettingsId.Value], cancellationToken)
                               ?? throw new InvalidOperationException($"GraphRagSettings {dto.GraphRagSettingsId.Value} not found");
        }

        if (dto.LlmPresetId.HasValue)
        {
            llmPreset = await _dbContext.LlmPresets.FindAsync([dto.LlmPresetId.Value], cancellationToken)
                         ?? throw new InvalidOperationException($"LlmPreset {dto.LlmPresetId.Value} not found");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            GraphRagSettingsId = dto.GraphRagSettingsId,
            LlmPresetId = dto.LlmPresetId,
            IndexingStatus = IndexingStatus.NotIndexed,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            GraphRagSettingsId = project.GraphRagSettingsId,
            GraphRagSettingsName = graphRagSettings?.Name,
            LlmPresetId = project.LlmPresetId,
            LlmPresetName = llmPreset?.Name,
            IndexingStatus = (IndexingStatusDto)project.IndexingStatus,
            LastIndexedAt = project.LastIndexedAt,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    public async Task<ProjectResponseDto?> UpdateProjectAsync(Guid id, ProjectUpdateDto dto, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects
            .Include(p => p.GraphRagSettings)
            .Include(p => p.LlmPreset)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null) return null;

        if (dto.Name is not null) project.Name = dto.Name;
        if (dto.Description is not null) project.Description = dto.Description;

        if (dto.GraphRagSettingsId.HasValue)
        {
            project.GraphRagSettingsId = dto.GraphRagSettingsId.Value;
            project.GraphRagSettings = await _dbContext.GraphRagSettings.FindAsync([dto.GraphRagSettingsId.Value], cancellationToken)
                                       ?? throw new InvalidOperationException($"GraphRagSettings {dto.GraphRagSettingsId.Value} not found");
        }

        if (dto.LlmPresetId.HasValue)
        {
            project.LlmPresetId = dto.LlmPresetId.Value;
            project.LlmPreset = await _dbContext.LlmPresets.FindAsync([dto.LlmPresetId.Value], cancellationToken)
                                ?? throw new InvalidOperationException($"LlmPreset {dto.LlmPresetId.Value} not found");
        }

        project.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(project);
    }

    public async Task<bool> DeleteProjectAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.FindAsync([id], cancellationToken);
        if (project is null) return false;

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProjectResponseDto MapToResponse(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        GraphRagSettingsId = p.GraphRagSettingsId,
        GraphRagSettingsName = p.GraphRagSettings?.Name,
        LlmPresetId = p.LlmPresetId,
        LlmPresetName = p.LlmPreset?.Name,
        IndexingStatus = (IndexingStatusDto)p.IndexingStatus,
        LastIndexedAt = p.LastIndexedAt,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}