using McpLab.Context;
using McpLab.Finance;
using McpLab.Host;
using McpLab.Host.Auth;
using McpLab.Host.Mcp;
using McpLab.Host.Security;
using McpLab.Mcp.Conventions.Modules;
using McpLab.Scheduling;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("mcplab");
builder.AddEdgeProtection();
builder.AddMcpAuthentication();

var mcp = builder.Services
    .AddMcpServer(options => options.ServerInfo = new() { Name = "mcp-lab-api", Version = ServerVersion.Current })
    .WithHttpTransport()
    .AddAuthorizationFilters()
    .WithTools<SystemTools>();

builder.AddModules(mcp, new ContextModule(), new FinanceModule(), new SchedulingModule());

var app = builder.Build();

// Migrations de cada módulo antes de aceitar requisições (como no ADS). Os testes de contrato desligam, pois não têm banco.
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await app.Services.InitializeModulesAsync();
}

app.UseForwardedHeaders();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapDefaultEndpoints();

// Única interface de negócio: não existem controllers nem endpoints REST (ADR 0002).
app.MapMcp("/mcp")
    .RequireAuthorization(AuthExtensions.McpPolicy)
    .RequireRateLimiting(EdgeProtectionExtensions.McpRateLimit);

await app.RunAsync();

public partial class Program;
