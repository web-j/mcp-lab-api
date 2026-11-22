# mcp-lab-api

Ensaio de uma API **AI native** em .NET 11: monolito modular com PostgreSQL cuja única interface é um servidor
[MCP](https://modelcontextprotocol.io). Não tem front-end nem REST; quem usa o sistema é um agente (Claude, Cursor etc.).

O objetivo é aprender, na prática, como desenhar um servidor MCP e registrar as boas práticas em
[`docs/mcp-guidelines.md`](docs/mcp-guidelines.md).

## Estrutura

```
src/
  McpLab.AppHost/          Aspire: Postgres + host + MCP Inspector
  McpLab.ServiceDefaults/  OpenTelemetry, health checks, resiliência
  McpLab.Host/             servidor MCP (Streamable HTTP em /mcp)
  BuildingBlocks/          SharedKernel e convenções MCP (IModule, nomes de tools)
  Modules/                 Finance, Scheduling, ... (cada um com seu projeto .Contracts)
tests/
  McpLab.ArchitectureTests/  fronteiras entre módulos
  McpLab.McpContractTests/   cliente MCP real garantindo as convenções das tools
docs/
  architecture.md, mcp-guidelines.md, adr/
```

## Rodando

Requisitos: [SDK .NET 11](https://dotnet.microsoft.com/download) e Docker (ou Colima).

```bash
dotnet test
dotnet run --project src/McpLab.AppHost
```

O servidor MCP fica em `http://localhost:5180/mcp`. Para conectar o Claude Code, o `.mcp.json` da raiz já aponta para ele.
Para testar à mão, abra o MCP Inspector pelo painel do Aspire.

## Roteiro

| Fase | Conteúdo | Estado |
|---|---|---|
| 0 | Solution, Aspire, Postgres, host MCP, testes de contrato e arquitetura, ADRs | feito |
| 1 | Módulo Finanças (contas, categorias, lançamentos) | |
| 2 | Módulo Agendamento, primeiro evento entre módulos, resources e prompts | |
| 3 | OAuth 2.1, escopos por módulo, auditoria, idempotência, elicitation | |
| 4 | Evals com agente real e versão 1 do `mcp-guidelines.md` | |

## Licença

MIT
