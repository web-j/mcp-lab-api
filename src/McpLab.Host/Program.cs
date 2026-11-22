using McpLab.Finance;
using McpLab.Host;
using McpLab.Host.Mcp;
using McpLab.Mcp.Conventions.Modules;
using McpLab.Scheduling;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("mcplab");

var mcp = builder.Services
    .AddMcpServer(options => options.ServerInfo = new() { Name = "mcp-lab-api", Version = ServerVersion.Current })
    .WithHttpTransport()
    .WithTools<SystemTools>();

builder.AddModules(mcp, new FinanceModule(), new SchedulingModule());

var app = builder.Build();

app.MapDefaultEndpoints();

// Única interface de negócio: não existem controllers nem endpoints REST (ADR 0002).
// A autenticação OAuth 2.1 entra na fase 3 (ADR 0003).
app.MapMcp("/mcp");

app.Run();

public partial class Program;
