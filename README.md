# mcp-lab-api

Ensaio de uma API **AI native** em .NET 11: monolito modular com PostgreSQL cuja única interface é um servidor
[MCP](https://modelcontextprotocol.io). Não tem front-end nem REST; quem usa o sistema é um agente (Claude, Cursor etc.).

O objetivo é aprender, na prática, como desenhar um servidor MCP e registrar as boas práticas em
[`docs/mcp-guidelines.md`](docs/mcp-guidelines.md).

## Conectar

Endereço do servidor: **`https://mcp.agateo.com.br/mcp`**. Na primeira conexão o app abre
[auth.agateo.com.br](https://auth.agateo.com.br) para você entrar (ou criar conta) e autorizar.

O passo a passo de cada app (Claude, ChatGPT, Grok, Cursor, Claude Code, VS Code) fica em
[auth.agateo.com.br/conectar](https://auth.agateo.com.br/conectar), com a versão em Markdown para IAs em
[/llms.txt](https://auth.agateo.com.br/llms.txt). Os dois arquivos ficam em `src/McpLab.Auth/public/` e
precisam ser atualizados juntos.

## Estrutura

```
src/
  McpLab.AppHost/          Aspire: Postgres + host + MCP Inspector
  McpLab.ServiceDefaults/  OpenTelemetry, health checks, resiliência
  McpLab.Host/             servidor MCP (Streamable HTTP em /mcp, OAuth 2.1 como resource server)
  McpLab.Auth/             página de login e consentimento (auth.agateo.com.br)
  BuildingBlocks/          SharedKernel e convenções MCP (IModule, nomes de tools)
  Modules/                 Finance, Scheduling, ... (cada um com seu projeto .Contracts)
tests/
  McpLab.ArchitectureTests/  fronteiras entre módulos
  McpLab.McpContractTests/   cliente MCP real garantindo as convenções das tools
deploy/prod/               compose, vhosts nginx e scripts da VPS
docs/
  architecture.md, mcp-guidelines.md, deploy.md, adr/
```

## Rodando

Requisitos: [SDK .NET 11](https://dotnet.microsoft.com/download) e Docker (ou Colima).

```bash
dotnet test
dotnet run --project src/McpLab.AppHost
```

Localmente o servidor fica em `http://localhost:5180/mcp` e a página de login em `http://localhost:5190`.
O login é o mesmo de produção (Supabase do lab). Para testar à mão, abra o MCP Inspector pelo painel do Aspire.

## Roteiro

| Fase | Conteúdo | Estado |
|---|---|---|
| 0 | Solution, Aspire, Postgres, host MCP, testes de contrato e arquitetura, ADRs | feito |
| 1 | Módulo Finanças (contas, categorias, lançamentos) | |
| 2 | Módulo Agendamento, primeiro evento entre módulos, resources e prompts | |
| 0.5 | OAuth 2.1 com Supabase, página de login, proteções de borda, deploy na VPS | feito |
| 3 | Auditoria, idempotência, elicitation, apps conectados, captcha | |
| 4 | Evals com agente real e versão 1 do `mcp-guidelines.md` | |

## Licença

MIT
