using System.ComponentModel;
using McpLab.Context.Notes;
using ModelContextProtocol.Server;

namespace McpLab.Context.History;

[McpServerToolType]
internal sealed class HistoryTools(HistoryQuery history)
{
    [McpServerTool(Name = "context_search_history", Title = "Buscar no histórico",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Busca palavras (sem diferenciar acentos) nas ações e notas da pessoa, inclusive concluídas e antigas fora do
        cartão. Use para perguntas sobre o passado ("quando anotei sobre a farmácia?"). Para listar ações em ordem, use
        context_list_activity.
        """)]
    public Task<HistoryPage> SearchHistory(
        [Description("Ex.: farmácia")] string text,
        [Description("Ex.: finance")] string? module = null,
        [Description("Só notas deste tipo")] NoteKind? kind = null,
        [Description("AAAA-MM-DD")] DateOnly? since = null,
        [Description("AAAA-MM-DD, inclusive")] DateOnly? until = null,
        [Description("nextCursor anterior")] string? cursor = null,
        [Description("Até 20")] int limit = 10,
        CancellationToken cancellationToken = default) =>
        history.SearchAsync(text, new HistoryFilter(module, kind?.Code(), since, until, cursor, limit), cancellationToken);

    [McpServerTool(Name = "context_list_activity", Title = "Linha do tempo",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Lista as ações registradas pelos módulos, da mais recente para a mais antiga, com o app de cada uma. Use para
        "o que fiz semana passada em finanças?". Para buscar palavras, use context_search_history.
        """)]
    public Task<HistoryPage> ListActivity(
        [Description("Ex.: finance")] string? module = null,
        [Description("AAAA-MM-DD")] DateOnly? since = null,
        [Description("AAAA-MM-DD, inclusive")] DateOnly? until = null,
        [Description("nextCursor anterior")] string? cursor = null,
        [Description("Até 20")] int limit = 20,
        CancellationToken cancellationToken = default) =>
        history.ListActivityAsync(new HistoryFilter(module, null, since, until, cursor, limit), cancellationToken);
}
