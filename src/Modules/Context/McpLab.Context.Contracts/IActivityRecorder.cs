namespace McpLab.Context.Contracts;

/// <summary>
/// Toda tool de escrita registra aqui o que fez (ADR 0010). O host grava na linha do tempo depois que a tool termina
/// com sucesso; se ela falhar, nada é gravado. Uma tool de escrita que termina sem registrar falha em desenvolvimento
/// e nos testes.
/// </summary>
public interface IActivityRecorder
{
    void Record(ActivityEntry entry);
}
