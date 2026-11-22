using System.Net.Http.Json;
using System.Text.Json.Serialization;
using McpLab.Context.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpLab.Context.Clients;

internal sealed class ClientDirectoryOptions
{
    /// <summary>URL do Auth do Supabase (<c>https://&lt;ref&gt;.supabase.co/auth/v1</c>), a mesma do emissor dos tokens.</summary>
    public string? AuthUrl { get; set; }

    /// <summary>Chave secreta do projeto Supabase do lab. Sem ela, os apps aparecem como desconhecidos.</summary>
    public string? SecretKey { get; set; }
}

/// <summary>
/// Nome dos apps conectados ("ChatGPT", "Claude"...). O token só traz o <c>client_id</c>; o nome vem do cadastro do
/// cliente OAuth no Supabase e fica em cache na tabela <c>context.clients</c> por 7 dias.
/// </summary>
internal sealed class ClientDirectory(
    HttpClient http,
    ContextDbContext db,
    IOptions<ClientDirectoryOptions> options,
    TimeProvider time,
    ILogger<ClientDirectory> logger)
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromDays(7);

    public async Task EnsureKnownAsync(string clientId, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var cached = await db.Clients.FindAsync([clientId], cancellationToken);
        if (cached is not null && now - cached.FetchedAt < CacheFor)
        {
            return;
        }

        var name = await FetchNameAsync(clientId, cancellationToken);
        if (cached is null)
        {
            db.Clients.Add(new ConnectedClient { ClientId = clientId, Name = name, FetchedAt = now });
        }
        else
        {
            cached.Name = name ?? cached.Name;
            cached.FetchedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> FetchNameAsync(string clientId, CancellationToken cancellationToken)
    {
        var (authUrl, secretKey) = (options.Value.AuthUrl, options.Value.SecretKey);
        if (string.IsNullOrEmpty(authUrl) || string.IsNullOrEmpty(secretKey))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{authUrl.TrimEnd('/')}/admin/oauth/clients/{Uri.EscapeDataString(clientId)}");
            request.Headers.Add("apikey", secretKey);
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Supabase respondeu {Status} ao buscar o cliente OAuth {ClientId}.", (int)response.StatusCode, clientId);
                return null;
            }

            var client = await response.Content.ReadFromJsonAsync<OAuthClient>(cancellationToken);
            return client?.ClientName;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Não foi possível buscar o nome do cliente OAuth {ClientId}.", clientId);
            return null;
        }
    }

    private sealed record OAuthClient([property: JsonPropertyName("client_name")] string? ClientName);
}
