using FableCraft.Infrastructure.Clients;

namespace FableCraft.Tests.Agents;

internal class MockRagSearch : IRagSearch
{
    public Task<SearchResult[]> SearchAsync(
        CallerContext context,
        List<string> datasets,
        string[] query,
        SearchType? searchType = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[]
        {
            new SearchResult("Mock",
                new SearchResponse
                {
                    Results = [new SearchResultItem { Text = "Mock search result for testing purposes." }]
                })
        });
    }
}