var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("mcp-lab-postgres")
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("mcplab");

var host = builder.AddDotnetProject("mcp-host", "../McpLab.Host/McpLab.Host.csproj")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

// Página de login e consentimento (auth.agateo.com.br em produção). Em dev roda em http://localhost:5190,
// que está na lista de redirects permitidos do projeto Supabase.
builder.AddDockerfile("auth", "../McpLab.Auth")
    .WithHttpEndpoint(port: 5190, targetPort: 80)
    .WithEnvironment("SUPABASE_URL", builder.Configuration["Supabase:Url"])
    .WithEnvironment("SUPABASE_PUBLISHABLE_KEY", builder.Configuration["Supabase:PublishableKey"])
    .WithEnvironment("MCP_URL", "http://localhost:5180/mcp");

builder.AddMcpInspector("mcp-inspector")
    .WithMcpServer(host);

builder.Build().Run();
