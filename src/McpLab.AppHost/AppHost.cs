var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("mcp-lab-postgres")
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("mcplab");

var host = builder.AddDotnetProject("mcp-host", "../McpLab.Host/McpLab.Host.csproj")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

builder.AddMcpInspector("mcp-inspector")
    .WithMcpServer(host);

builder.Build().Run();
