using System.ComponentModel;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.SemanticKernel;

using Serilog;

namespace FableCraft.ProjectManagement.Plugins;

internal sealed class ProjectSearchPlugin
{
    private readonly IRagClientFactory _ragClientFactory;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger _logger;
    private Guid _projectId;

    public ProjectSearchPlugin(IRagClientFactory ragClientFactory, ApplicationDbContext dbContext, ILogger logger)
    {
        _ragClientFactory = ragClientFactory;
        _dbContext = dbContext;
        _logger = logger;
    }

    public void SetProjectId(Guid projectId) => _projectId = projectId;

    [KernelFunction("search_knowledge")]
    [Description("Search the indexed knowledge graph to recall established facts about the world. Only works after files have been indexed.")]
    public async Task<string> SearchKnowledgeAsync(
        [Description("List of search queries to look up in the knowledge graph")] string[] queries)
    {
        _logger.Information("Searching knowledge for project {ProjectId} with queries: {Queries}", _projectId, string.Join(", ", queries));

        var project = await _dbContext.Projects.FindAsync([_projectId]);
        if (project is null || project.IndexingStatus != IndexingStatus.Indexed)
        {
            return "Knowledge graph has not been indexed yet. Index the project files first before searching.";
        }

        try
        {
            var searchClient = await _ragClientFactory.CreateSearchClientForProject(_projectId, CancellationToken.None);
            var callerContext = new CallerContext(nameof(ProjectSearchPlugin), _projectId, null);
            var results = await searchClient.SearchAsync(callerContext, ["project"], queries);

            var responseParts = results.Select(r =>
            {
                var textResults = r.Response.Results.Select(rt => rt.Text).Where(t => !string.IsNullOrEmpty(t));
                return $"Query: {r.Query}\n{string.Join("\n", textResults)}";
            });

            return string.Join("\n\n---\n\n", responseParts);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Knowledge search failed for project {ProjectId}", _projectId);
            return $"Search failed: {ex.Message}";
        }
    }
}