using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace McpLab.McpContractTests;

/// <summary>Emite tokens no formato dos do Supabase OAuth, assinados com uma chave local (ES256).</summary>
public static class TestTokens
{
    public const string Issuer = "https://test.supabase.local/auth/v1";

    public static readonly Guid UserId = Guid.Parse("748db4cd-6ae1-4883-b2a0-6a1d24e72238");
    public const string Email = "pessoa@exemplo.com";
    public const string ClientId = "e6f7fae2-79ff-4bce-95fc-7bed4df12ff2";

    public static ECDsaSecurityKey SigningKey { get; } = new(ECDsa.Create(ECCurve.NamedCurves.nistP256)) { KeyId = "test" };

    /// <summary>Token de acesso OAuth válido, como o que o ChatGPT ou o Claude recebem depois do consentimento.</summary>
    public static string OAuthAccessToken(bool includeClientId = true, string issuer = Issuer)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = UserId.ToString(),
            ["email"] = Email,
            ["role"] = "authenticated",
        };

        if (includeClientId)
        {
            claims["client_id"] = ClientId;
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = "authenticated",
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.EcdsaSha256),
        });
    }
}
