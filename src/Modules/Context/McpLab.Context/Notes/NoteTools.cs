using System.ComponentModel;
using McpLab.Context.Clients;
using McpLab.Context.Persistence;
using McpLab.Mcp.Conventions.Identity;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace McpLab.Context.Notes;

/// <summary>Notas que a IA escreve sobre a conversa: o que só ela sabe e precisa seguir para o próximo app.</summary>
[McpServerToolType]
internal sealed class NoteTools(ContextDbContext db, ICurrentUser user, TimeProvider time, ClientDirectory clients)
{
    public const int MaxTextLength = 280;

    private readonly ContextDbContext _db = db;
    private readonly ICurrentUser _user = user;
    private readonly TimeProvider _time = time;

    [McpServerTool(Name = "context_save_note", Title = "Salvar nota de contexto",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Guarda uma nota curta que acompanha a pessoa em qualquer app de IA: objetivo, pendência, decisão, preferência,
        fato, ou kind=handoff com onde a conversa parou (ao encerrar ou quando pedirem). Não use para o que outras tools
        já gravam (lançamentos, compromissos).
        """)]
    public async Task<NoteInfo> SaveNote(
        [Description("handoff = onde a conversa parou")] NoteKind kind,
        [Description("pt-BR, até 280 caracteres")] string text,
        [Description("Módulo relacionado. Ex.: finance")] string? module = null,
        [Description("Manter no topo do cartão")] bool pinned = false,
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

        // O cartão mostra por qual app a nota foi salva ("Onde paramos (ChatGPT, há 2 h)").
        await clients.EnsureKnownAsync(_user.ClientId, cancellationToken);
        return NoteInfo.From(note);
    }

    [McpServerTool(Name = "context_update_note", Title = "Atualizar nota de contexto",
        ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Conclui, reabre, fixa, desafixa ou edita uma nota pelo número (#12) do cartão. Use complete quando uma pendência
        ou objetivo for resolvido: sai do cartão e continua na busca.
        """)]
    public async Task<NoteInfo> UpdateNote(
        [Description("Número sem o #. Ex.: 12")] int number,
        NoteAction action,
        [Description("Novo texto, para action=edit")] string? text = null,
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
        Apaga de vez uma nota pelo número (#12). Só quando a pessoa pedir para esquecer algo; para pendência resolvida,
        use context_update_note com action=complete.
        """)]
    public async Task<DeletedNote> DeleteNote(
        [Description("Número sem o #. Ex.: 12")] int number,
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
