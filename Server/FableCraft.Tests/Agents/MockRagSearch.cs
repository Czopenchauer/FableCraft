using FableCraft.Infrastructure.Clients;

namespace FableCraft.Tests.Agents;

internal class MockRagSearch : IRagSearch
{
    public Task<SearchResponse> SearchAsync(CallerContext context, string query, string searchType = "GRAPH_COMPLETION", CancellationToken cancellationToken = default)
    {
        // Return a mock response for testing
        return Task.FromResult(new SearchResponse
        {
            Results = new List<string> { "Mock search result for testing purposes." }
        });
    }
}

