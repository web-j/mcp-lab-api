# Arquitetura

Monolito modular em .NET 11 com PostgreSQL, exposto somente via MCP.
Versão visual: https://claude.ai/artifact/9eryizjkGo4sgV5DWTqo7h

## Camadas

```
Clientes MCP (Claude, ChatGPT, Cursor, Grok, MCP Inspector, agente de evals)
        │  https://mcp.agateo.com.br/mcp  (Streamable HTTP + bearer token OAuth 2.1)
        │        ▲ login e consentimento em https://auth.agateo.com.br (Supabase Auth do lab)
        ▼
McpLab.Host ─ servidor MCP + pipeline transversal
        │  forwarded headers → CORS → autenticação (JWT) → rate limit → autorização → handler → telemetria
        ▼
Módulos (Finance, Scheduling, ...) ─ cada um registra tools, resources e prompts via IModule
        │  Mcp → Application → Domain → Infrastructure
        ▼
BuildingBlocks ─ SharedKernel (Result, Error, ...) e Mcp.Conventions (IModule, ToolNaming)
        ▼
PostgreSQL ─ um schema por módulo + schema platform (auditoria, idempotência)
```

## Módulos

- Um projeto por módulo (`McpLab.<Modulo>`), com as camadas em pastas: `Domain`, `Application`, `Infrastructure`, `Mcp`.
- Um projeto `McpLab.<Modulo>.Contracts` com o que outros módulos podem usar (interfaces e eventos de integração).
- Cada módulo implementa `IModule`: informa o prefixo das tools (`Name`), uma descrição e registra os próprios serviços e primitivas MCP.
- O host só registra a lista de módulos em `Program.cs`. Um módulo novo não muda mais nada no host.

## Persistência

- Um `DbContext` por módulo, no schema do módulo (ADR 0004), com nomes em snake_case e tabela de migrations no próprio
  schema (`<schema>.__migrations`). Todos usam o `NpgsqlDataSource` registrado pelo host (Aspire).
- Migrations ficam em `src/Modules/<Modulo>/McpLab.<Modulo>/Persistence/Migrations` e são aplicadas na subida do host,
  por `IModule.InitializeAsync`, antes de aceitar requisições.
- Criar migration: `dotnet tool restore` e
  `dotnet ef migrations add <Nome> --project src/Modules/<Modulo>/McpLab.<Modulo> --output-dir Persistence/Migrations`.

## Contexto portátil

Módulo `Context` (ADR 0010, `openspec/changes/contexto-portatil`): toda tool de escrita registra um evento curto no
`IActivityRecorder`; um filtro de `CallTool` grava em `context.events` e soma em `context.weekly_rollups` quando a tool
termina com sucesso. A IA salva notas curtas (`context_save_note`: objetivo, pendência, decisão, preferência, fato,
"onde paramos") e lê tudo num cartão de tamanho fixo (`context_get_summary`, resource `context://summary`, prompt
`context_resume`). As `instructions` do `initialize` pedem para ler o cartão no começo da conversa. O que não cabe no
cartão sai pela busca (`context_search_history`, texto em português sem acento) e pela linha do tempo paginada
(`context_list_activity`).

## Host

- `ModelContextProtocol.AspNetCore` com transporte Streamable HTTP em `/mcp`.
- O SDK 2.4 roda em modo stateless por padrão (sem `Mcp-Session-Id`). Recursos que dependem de sessão, como elicitation e sampling, exigem ligar o modo com sessão; essa decisão fica para a fase 3.
- Tools do próprio host usam o prefixo `system` (`system_get_status`, `system_get_current_user`).
- OpenTelemetry já coleta as spans e métricas do SDK do MCP (`Experimental.ModelContextProtocol`), visíveis no painel do Aspire.

## Autenticação

Detalhes no ADR 0007. Resumo do fluxo quando alguém conecta o servidor num cliente (ChatGPT, Claude...):

```
cliente ──► POST /mcp sem token            → 401 + WWW-Authenticate: resource_metadata=...
cliente ──► /.well-known/oauth-protected-resource/mcp
            → authorization_servers: https://eowxffcgonmbbsohedox.supabase.co/auth/v1
cliente ──► Supabase: metadados OAuth, registro dinâmico (DCR), /oauth/authorize
Supabase ─► https://auth.agateo.com.br/oauth/consent?authorization_id=...   (src/McpLab.Auth)
pessoa   ─► entra ou cria conta, clica em Permitir
Supabase ─► devolve o código ao cliente, que troca por access token (JWT ES256)
cliente ──► POST /mcp com Bearer → host valida emissor, assinatura (JWKS), aud e client_id
```

- `ICurrentUser` (em `Mcp.Conventions`) entrega o `Id` (claim `sub`), o e-mail e o `ClientId` para as tools.
  Todo dado de módulo é filtrado por `ICurrentUser.Id`.
- A página `src/McpLab.Auth` é HTML + JS puros com o supabase-js servido pelo próprio container. Ela cuida de
  login, cadastro, confirmação de e-mail, nova senha e consentimento.

## Produção

Detalhes nos ADRs 0008 (proteções) e 0009 (deploy), e a operação em `docs/deploy.md`.

```
waha-nginx (80/443, borda compartilhada da VPS)
  ├─ mcp.agateo.com.br  → mcp-lab-api:8080   (rede waha)
  └─ auth.agateo.com.br → mcp-lab-auth:80    (rede waha)
mcp-lab-api ── rede mcplab ──► mcp-lab-postgres (sem porta no host)
```

## Testes

- `McpLab.ArchitectureTests`: um módulo não referencia outro diretamente; building blocks não dependem de módulos.
- `McpLab.IntegrationTests`: host completo com Postgres em container (Testcontainers) e migrations reais; precisa
  de Docker rodando.
- `McpLab.McpContractTests`: sobe o host em memória, conecta um cliente MCP real (com token assinado por uma chave de teste no formato do Supabase) e valida todas as tools contra as convenções e o contrato de autorização.
- `McpLab.Evals` (fase 4): cenários em linguagem natural executados por um agente real.
