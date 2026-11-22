using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace McpLab.Context.Persistence;

/// <summary>Só para o <c>dotnet ef migrations add</c>; a conexão real vem do host.</summary>
internal sealed class ContextDbContextFactory : IDesignTimeDbContextFactory<ContextDbContext>
{
    public ContextDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ContextDbContext>()
        .UseNpgsql("Host=localhost;Database=mcplab", o => o.MigrationsHistoryTable("__migrations", ContextDbContext.Schema))
        .Options);
}
