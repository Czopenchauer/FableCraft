using FableCraft.Infrastructure.Clients;

namespace FableCraft.Tests.Agents;

internal class MockRagSearch : IRagSearch
{
    public Task<SearchResult[]> SearchAsync(CallerContext context, string[] query, string searchType = "GRAPH_COMPLETION", CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SearchResult[]
        {
            new SearchResult("Mock",
                new SearchResponse
                {
                    Results = new List<string> { "Mock search result for testing purposes." }
                })
        });
    }
}