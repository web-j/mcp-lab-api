using McpLab.Context.Clients;
using McpLab.Mcp.Conventions;
using McpLab.Mcp.Conventions.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpLab.Context.Activity;

/// <summary>
/// Depois de cada tool de escrita que terminou com sucesso, grava na linha do tempo o que ela registrou no
/// <see cref="Contracts.IActivityRecorder"/>. Tools que só leem, que falharam ou do próprio módulo context não geram evento.
/// </summary>
internal static class ActivityRecordingFilter
{
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Create(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next) => async (context, cancellationToken) =>
    {
        var result = await next(context, cancellationToken);

        if (context.MatchedPrimitive is not McpServerTool tool
            || tool.ProtocolTool.Annotations?.ReadOnlyHint == true
            || result.IsError == true)
        {
            return result;
        }

        var toolName = tool.ProtocolTool.Name;
        var module = ToolNaming.PrefixOf(toolName);
        if (module == ContextModule.Prefix)
        {
            return result;
        }

        var services = context.Services ?? throw new InvalidOperationException("Requisição MCP sem escopo de serviços.");
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ActivityRecordingFilter));
        var recorder = services.GetRequiredService<ActivityRecorder>();

        if (recorder.Entries.Count == 0)
        {
            logger.LogError("A tool de escrita {Tool} terminou sem registrar evento no IActivityRecorder.", toolName);

            if (!services.GetRequiredService<IHostEnvironment>().IsProduction())
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = $"Erro de desenvolvimento: a tool {toolName} grava dados mas não registrou o evento (IActivityRecorder.Record)." }],
                };
            }

            return result;
        }

        try
        {
            var user = services.GetRequiredService<ICurrentUser>();
            var log = services.GetRequiredService<ActivityLog>();
            var now = services.GetRequiredService<TimeProvider>().GetUtcNow();

            foreach (var entry in recorder.Entries)
            {
                await log.RecordAsync(new ActivityRecord(user.Id, user.ClientId, module, toolName, entry, now), cancellationToken);
            }

            await services.GetRequiredService<ClientDirectory>().EnsureKnownAsync(user.ClientId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A escrita de negócio já aconteceu e continua valendo; perder o evento só empobrece o contexto (ADR 0010).
            logger.LogError(ex, "Falha ao gravar o evento da tool {Tool} na linha do tempo.", toolName);
        }

        return result;
    };
}
