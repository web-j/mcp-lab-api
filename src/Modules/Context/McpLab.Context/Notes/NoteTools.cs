using System.ComponentModel;
using McpLab.Context.Persistence;
using McpLab.Mcp.Conventions.Identity;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace McpLab.Context.Notes;

/// <summary>Notas que a IA escreve sobre a conversa: o que só ela sabe e precisa seguir para o próximo app.</summary>
[McpServerToolType]
internal sealed class NoteTools(ContextDbContext db, ICurrentUser user, TimeProvider time)
{
    public const int MaxTextLength = 280;

    private readonly ContextDbContext _db = db;
    private readonly ICurrentUser _user = user;
    private readonly TimeProvider _time = time;

    [McpServerTool(Name = "context_save_note", Title = "Salvar nota de contexto",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Guarda uma nota curta que acompanha a pessoa em qualquer app de IA. Use quando surgir um objetivo, uma pendência,
        uma decisão, uma preferência ou um fato importante, e salve kind=handoff com onde a conversa parou quando a pessoa
        encerrar ou pedir. Não use para o que as outras tools já gravam (lançamentos, compromissos).
        """)]
    public async Task<NoteInfo> SaveNote(
        [Description("goal, pending, decision, preference, fact ou handoff (onde a conversa parou)")] NoteKind kind,
        [Description("Texto em pt-BR, até 280 caracteres. Ex.: \"orçamento de dezembro: faltam lazer e presentes\"")] string text,
        [Description("Prefixo do módulo relacionado, se houver. Ex.: finance")] string? module = null,
        [Description("true para manter no topo do cartão")] bool pinned = false,
        CancellationToken cancellationToken = default)
    {
        text = ValidText(text);
        var now = _time.GetUtcNow();
        var number = (await _db.Notes.Where(n => n.UserId == _user.Id).MaxAsync(n => (int?)n.Number, cancellationToken) ?? 0) + 1;

        var note = new ContextNote
        {
            Id = Guid.CreateVersion7(),
            UserId = _user.Id,
            Number = number,
            Kind = kind.Code(),
            Text = text,
            Module = string.IsNullOrWhiteSpace(module) ? null : module.Trim().ToLowerInvariant(),
            Status = NoteKinds.Open,
            Pinned = pinned,
            ClientId = _user.ClientId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Notes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);
        return NoteInfo.From(note);
    }

    [McpServerTool(Name = "context_update_note", Title = "Atualizar nota de contexto",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Conclui, reabre, fixa, desafixa ou edita uma nota pelo número (#12) que aparece no cartão de context_get_summary.
        Use complete quando uma pendência ou objetivo for resolvido: a nota sai do cartão e continua guardada.
        """)]
    public async Task<NoteInfo> UpdateNote(
        [Description("Número da nota, sem o #. Ex.: 12")] int number,
        [Description("complete, reopen, pin, unpin ou edit")] NoteAction action,
        [Description("Novo texto, só para action=edit (até 280 caracteres)")] string? text = null,
        CancellationToken cancellationToken = default)
    {
        var note = await FindAsync(number, cancellationToken);

        switch (action)
        {
            case NoteAction.Complete: note.Status = NoteKinds.Done; break;
            case NoteAction.Reopen: note.Status = NoteKinds.Open; break;
            case NoteAction.Pin: note.Pinned = true; break;
            case NoteAction.Unpin: note.Pinned = false; break;
            case NoteAction.Edit:
                note.Text = ValidText(text ?? throw new McpException("Para action=edit, envie o novo texto em text."));
                break;
        }

        note.UpdatedAt = _time.GetUtcNow();
        await _db.SaveChangesAsync(cancellationToken);
        return NoteInfo.From(note);
    }

    [McpServerTool(Name = "context_delete_note", Title = "Apagar nota de contexto",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Apaga de vez uma nota pelo número (#12). Use só quando a pessoa pedir para esquecer algo. Para pendência resolvida,
        prefira context_update_note com action=complete, que mantém o histórico.
        """)]
    public async Task<DeletedNote> DeleteNote(
        [Description("Número da nota, sem o #. Ex.: 12")] int number,
        CancellationToken cancellationToken = default)
    {
        var note = await FindAsync(number, cancellationToken);
        _db.Notes.Remove(note);
        await _db.SaveChangesAsync(cancellationToken);
        return new DeletedNote(number);
    }

    private async Task<ContextNote> FindAsync(int number, CancellationToken cancellationToken) =>
        await _db.Notes.SingleOrDefaultAsync(n => n.UserId == _user.Id && n.Number == number, cancellationToken)
        ?? throw new McpException($"Nota #{number} não existe. Chame context_get_summary para ver os números das notas.");

    private static string ValidText(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            throw new McpException("O texto da nota está vazio.");
        }

        if (text.Length > MaxTextLength)
        {
            throw new McpException($"A nota tem {text.Length} caracteres; o limite é {MaxTextLength}. Resuma e tente de novo.");
        }

        return text;
    }
}

public sealed record NoteInfo(
    [property: Description("Número para referenciar a nota (#12)")] int Number,
    string Kind,
    string Text,
    [property: Description("open ou done")] string Status,
    bool Pinned)
{
    internal static NoteInfo From(ContextNote note) => new(note.Number, note.Kind, note.Text, note.Status, note.Pinned);
}

public sealed record DeletedNote(int Number);
