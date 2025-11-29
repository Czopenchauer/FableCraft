using System.Text.Json;

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
        // var kek = new TrackerPromptBuilder(structure!);
        // var serializedKeke = JsonSerializer.Serialize(kek, JsonOptions);
        // Assert.True(kek.Equals(structure));
    }
}