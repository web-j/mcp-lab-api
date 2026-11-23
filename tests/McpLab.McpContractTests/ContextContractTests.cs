namespace McpLab.McpContractTests;

/// <summary>O que um app encontra do contexto portátil ao conectar (change contexto-portatil, entrega 2).</summary>
public sealed class ContextContractTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Context_tools_are_listed()
    {
        var tools = (await server.Client.ListToolsAsync(cancellationToken: Ct)).Select(t => t.Name).ToHashSet();

        Assert.Superset(new HashSet<string>
        {
            "context_get_summary", "context_save_note", "context_update_note", "context_delete_note",
            "context_search_history", "context_list_activity",
        }, tools);
    }

    [Fact]
    public void Initialize_carries_short_instructions_to_read_the_card_first()
    {
        var instructions = server.Client.ServerInstructions;

        Assert.NotNull(instructions);
        Assert.Contains("context_get_summary", instructions);
        Assert.Contains("context_save_note", instructions);
        Assert.True(instructions.Length <= 600, $"instructions com {instructions.Length} caracteres; o limite é 600.");
    }

    [Fact]
    public async Task Summary_resource_and_resume_prompt_are_listed()
    {
        var resources = await server.Client.ListResourcesAsync(cancellationToken: Ct);
        var prompts = await server.Client.ListPromptsAsync(cancellationToken: Ct);

        Assert.Contains(resources, r => r.Uri == "context://summary");
        Assert.Contains(prompts, p => p.Name == "context_resume");
    }

    [Fact]
    public async Task Context_tool_descriptions_stay_within_the_token_budget()
    {
        // As descrições vão em todo tools/list; o change prevê ~900 tokens (≈ 3.600 caracteres) para as tools de contexto.
        var tools = await server.Client.ListToolsAsync(cancellationToken: Ct);
        var size = tools.Where(t => t.Name.StartsWith("context_", StringComparison.Ordinal))
            .Sum(t => (t.Description?.Length ?? 0) + t.JsonSchema.GetRawText().Length);

        Assert.True(size <= 3_600, $"Tools de contexto somam {size} caracteres de descrição e schema; o orçamento é 3.600.");
    }
}
