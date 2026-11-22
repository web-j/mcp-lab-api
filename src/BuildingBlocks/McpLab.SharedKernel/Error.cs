namespace McpLab.SharedKernel;

/// <summary>
/// Erro de negócio esperado. Vira resultado de tool com <c>isError</c> (ADR 0006), então a
/// mensagem precisa dizer ao agente o que deu errado e como corrigir.
/// </summary>
public sealed record Error(string Code, string Message, ErrorKind Kind = ErrorKind.Validation)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);
    public static Error Validation(string code, string message) => new(code, message, ErrorKind.Validation);
}

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
}
