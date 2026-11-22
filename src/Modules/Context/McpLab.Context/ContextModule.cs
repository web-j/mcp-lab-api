using McpLab.Context.Activity;
using McpLab.Context.Clients;
using McpLab.Context.Contracts;
using McpLab.Context.Persistence;
using McpLab.Mcp.Conventions.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace McpLab.Context;

/// <summary>
/// Contexto portátil entre IAs (change contexto-portatil, ADR 0010): linha do tempo das escritas, notas da IA e o
/// cartão "onde parei". Base da plataforma; os módulos de negócio só conhecem <c>McpLab.Context.Contracts</c>.
/// </summary>
public sealed class ContextModule : IModule
{
    public const string Prefix = "context";

    public string Name => Prefix;

    public string Description => "Contexto que acompanha a pessoa entre apps de IA: o que foi feito, pendências e onde parou.";

    public void Register(IHostApplicationBuilder builder, IMcpServerBuilder mcp)
    {
        builder.Services.AddDbContext<ContextDbContext>((sp, o) => o.UseNpgsql(
            sp.GetRequiredService<NpgsqlDataSource>(),
            npgsql => npgsql.MigrationsHistoryTable("__migrations", ContextDbContext.Schema)));

        builder.Services.AddScoped<ActivityRecorder>();
        builder.Services.AddScoped<IActivityRecorder>(sp => sp.GetRequiredService<ActivityRecorder>());
        builder.Services.AddScoped<ActivityLog>();
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddOptions<ClientDirectoryOptions>()
            .Configure(o =>
            {
                o.AuthUrl = builder.Configuration["Auth:Issuer"];
                o.SecretKey = builder.Configuration["Supabase:SecretKey"];
            });
        builder.Services.AddHttpClient<ClientDirectory>(http => http.Timeout = TimeSpan.FromSeconds(5));

        var assembly = typeof(ContextModule).Assembly;
        mcp.WithToolsFromAssembly(assembly)
           .WithResourcesFromAssembly(assembly)
           .WithPromptsFromAssembly(assembly)
           .WithRequestFilters(filters => filters.AddCallToolFilter(ActivityRecordingFilter.Create));
    }

    public async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<ContextDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
