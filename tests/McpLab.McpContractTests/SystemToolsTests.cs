using System.Text.Json;

namespace McpLab.McpContractTests;

public sealed class SystemToolsTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    [Fact]
    public async Task Get_status_returns_structured_content_with_loaded_modules()
    {
        var result = await server.Client.CallToolAsync(
            "system_get_status",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(true, result.IsError);
        Assert.NotNull(result.StructuredContent);

        var status = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(result.StructuredContent));
        var modules = status.GetProperty("modules").EnumerateArray().Select(m => m.GetProperty("name").GetString()).ToList();

        Assert.Equal("mcp-lab-api", status.GetProperty("server").GetString());
        Assert.Equal(["context", "finance", "scheduling"], modules);
    }
}
