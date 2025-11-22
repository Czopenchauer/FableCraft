using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Tracker;
using FableCraft.Infrastructure.Persistence.Entities;

namespace FableCraft.Tests.Tracker;

public class TrackerConverterTests
{
    private readonly static JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    [Fact]
    public void TestMethod1()
    {
        // var structure = JsonSerializer.Deserialize<TrackerStructure>(TestTracker.InputJson, JsonOptions);
        //
        // var kek = new TrackerExampleModel(structure);
        // var serializedKeke = JsonSerializer.Serialize(kek, JsonOptions);
        // var tracker = JsonSerializer.Deserialize<TrackerModel>(serializedKeke);
        // Assert.True(kek.Equals(structure));
    }
}