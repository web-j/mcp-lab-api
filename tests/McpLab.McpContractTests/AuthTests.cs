using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace McpLab.McpContractTests;

/// <summary>Contrato de autorização do MCP (ADR 0008): o que um cliente como o ChatGPT encontra ao conectar.</summary>
public sealed class AuthTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private const string InitializeRequest = """
        {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1"}}}
        """;

    [Fact]
    public async Task Request_without_token_gets_401_pointing_to_resource_metadata()
    {
        var response = await PostInitializeAsync(token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Bearer", challenge.Scheme);
        Assert.Contains("resource_metadata=", challenge.Parameter);
        Assert.Contains("/.well-known/oauth-protected-resource", challenge.Parameter);
    }

    [Fact]
    public async Task Resource_metadata_points_to_the_supabase_authorization_server()
    {
        var http = server.Factory.CreateClient();

        var metadata = await http.GetFromJsonAsync<JsonElement>("/.well-known/oauth-protected-resource/mcp", TestContext.Current.CancellationToken);

        Assert.Equal("http://localhost/mcp", metadata.GetProperty("resource").GetString());
        Assert.Equal([TestTokens.Issuer], metadata.GetProperty("authorization_servers").EnumerateArray().Select(e => e.GetString()));
        // Sem offline_access o ChatGPT e o Claude não recebem refresh token e desconectam a pessoa a cada hora.
        Assert.Contains("offline_access", metadata.GetProperty("scopes_supported").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Supabase_session_token_without_client_id_is_forbidden()
    {
        var response = await PostInitializeAsync(TestTokens.OAuthAccessToken(includeClientId: false));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Token_from_another_issuer_is_rejected()
    {
        var response = await PostInitializeAsync(TestTokens.OAuthAccessToken(issuer: "https://outro.supabase.co/auth/v1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Current_user_tool_returns_the_token_owner()
    {
        var result = await server.Client.CallToolAsync("system_get_current_user", cancellationToken: TestContext.Current.CancellationToken);

        var user = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(result.StructuredContent));
        Assert.Equal(TestTokens.UserId.ToString(), user.GetProperty("userId").GetString());
        Assert.Equal(TestTokens.Email, user.GetProperty("email").GetString());
        Assert.Equal(TestTokens.ClientId, user.GetProperty("clientId").GetString());
    }

    private async Task<HttpResponseMessage> PostInitializeAsync(string? token)
    {
        var http = server.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(InitializeRequest, Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await http.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
