using System.ComponentModel;
using McpLab.Context.Contracts;
using McpLab.Mcp.Conventions;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace McpLab.IntegrationTests;

/// <summary>Tools falsas, só nos testes de integração, com o prefixo "testing".</summary>
[McpServerToolType]
public sealed class TestingTools(IActivityRecorder activity)
{

    [McpServerTool(Name = "testing_record_sample", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Escrita de teste que registra um evento.")]
    public string RecordSample(decimal amount, string? idempotencyKey = null)
    {
        activity.Record(new ActivityEntry(
            $"Despesa {PtBr.Money(amount)} · Mercado",
            $"testing:sample:{Guid.NewGuid()}",
            new Dictionary<string, decimal> { ["amount"] = amount },
            idempotencyKey));
        return "ok";
    }

    [McpServerTool(Name = "testing_fail_sample", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Escrita de teste que registra um evento e depois falha com erro de negócio.")]
    public string FailSample()
    {
        activity.Record(new ActivityEntry("Não deveria aparecer"));
        throw new McpException("Categoria 'x' não existe.");
    }

    [McpServerTool(Name = "testing_forget_sample", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Escrita de teste que esquece de registrar o evento.")]
    public string ForgetSample() => "ok";

    [McpServerTool(Name = "testing_read_sample", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Leitura de teste.")]
    public string ReadSample() => "ok";
}
