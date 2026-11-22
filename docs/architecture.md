# Arquitetura

Monolito modular em .NET 11 com PostgreSQL, exposto somente via MCP.
Versão visual: https://claude.ai/artifact/9eryizjkGo4sgV5DWTqo7h

## Camadas

```
Clientes MCP (Claude Code/Desktop, Cursor, MCP Inspector, agente de evals)
        │  Streamable HTTP em /mcp  (OAuth 2.1 na fase 3)
        ▼
McpLab.Host ─ servidor MCP + pipeline transversal
        │  auth → tenant → rate limit → validação → handler → auditoria → telemetria
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

## Host

- `ModelContextProtocol.AspNetCore` com transporte Streamable HTTP em `/mcp`.
- O SDK 2.4 roda em modo stateless por padrão (sem `Mcp-Session-Id`). Recursos que dependem de sessão, como elicitation e sampling, exigem ligar o modo com sessão; essa decisão fica para a fase 3.
- Tools do próprio host usam o prefixo `system` (ex.: `system_get_status`).
- OpenTelemetry já coleta as spans e métricas do SDK do MCP (`Experimental.ModelContextProtocol`), visíveis no painel do Aspire.

## Testes

- `McpLab.ArchitectureTests`: um módulo não referencia outro diretamente; building blocks não dependem de módulos.
- `McpLab.McpContractTests`: sobe o host em memória, conecta um cliente MCP real e valida todas as tools contra as convenções.
- `McpLab.Evals` (fase 4): cenários em linguagem natural executados por um agente real.
