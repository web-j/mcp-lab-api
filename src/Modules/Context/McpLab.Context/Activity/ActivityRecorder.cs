using McpLab.Context.Contracts;

namespace McpLab.Context.Activity;

/// <summary>Junta os eventos registrados pela tool durante a requisição; o filtro grava depois do sucesso.</summary>
internal sealed class ActivityRecorder : IActivityRecorder
{
    private readonly List<ActivityEntry> _entries = [];

    public IReadOnlyList<ActivityEntry> Entries => _entries;

    public void Record(ActivityEntry entry) => _entries.Add(entry);
}
