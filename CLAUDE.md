# mcp-lab-api

Ensaio de uma API .NET 11 **sem front-end e sem REST**: toda funcionalidade é exposta somente via MCP.
O objetivo é aprender e definir boas práticas e padrões para servidores MCP AI native.
O resultado do ensaio é o `docs/mcp-guidelines.md`.

## Antes de mudar qualquer coisa

1. Ler `docs/architecture.md`, `docs/mcp-guidelines.md` e os ADRs em `docs/adr/`.
2. Uma decisão nova de arquitetura ou de padrão de tool vira um ADR novo (próximo número, mesmo formato).
3. Quando um padrão mudar, atualizar `docs/mcp-guidelines.md` e o teste que garante esse padrão no mesmo PR.

## Regras

- **MCP é a única interface.** Não criar controllers, minimal APIs nem endpoints HTTP de negócio. Fora do MCP só existem `/health`, `/alive` e os metadados OAuth (`/.well-known/oauth-protected-resource/mcp`).
- **Toda tool exige login.** O host valida o token do Supabase do lab (ADR 0007); as tools usam `ICurrentUser` e filtram tudo pelo `Id`. Nunca receber ID de usuário como parâmetro.
- **Monolito modular.** Cada módulo fica em `src/Modules/<Modulo>/` e implementa `IModule`. Um módulo só usa outro pelo projeto `*.Contracts`; os testes de arquitetura falham se isso for violado.
- **Tools seguem `docs/mcp-guidelines.md`.** Nome `modulo_verbo_objeto`, `Title`, descrição dizendo quando usar e quando não usar, as quatro anotações (`ReadOnly`, `Destructive`, `Idempotent`, `OpenWorld`) e `UseStructuredContent = true`. `McpLab.McpContractTests` falha se alguma tool fugir disso.
- **Erro de negócio é resultado, não exceção.** O domínio devolve `Result<T>`/`Error` (SharedKernel) e a mensagem ensina o agente a corrigir.
- **Um schema Postgres por módulo**, com `DbContext` e migrations próprios.
- Código e identificadores em inglês; descrições de tools, mensagens de erro, docs e ADRs em português do Brasil.
- Versões de pacotes só em `Directory.Packages.props`.

## Comandos

```bash
dotnet build                                   # warnings são erros
dotnet test                                    # Microsoft Testing Platform (global.json)
dotnet run --project src/McpLab.AppHost        # Aspire: Postgres + host + MCP Inspector + painel
```

Requisitos locais: SDK .NET 11 (`global.json`) e um runtime de containers (Docker/Colima) para o Postgres e a página de login.
Com o AppHost rodando, o servidor MCP fica em `http://localhost:5180/mcp` (o `.mcp.json` da raiz conecta o Claude Code) e a página de login em `http://localhost:5190`.

## Produção

- `https://mcp.agateo.com.br/mcp` (servidor) e `https://auth.agateo.com.br` (login). Push na `main` faz deploy (`.github/workflows/deploy.yml`).
- Supabase do lab: projeto `mcp-lab` (`eowxffcgonmbbsohedox`), exclusivo deste repositório. Não usar o projeto da Nuncie.
- VPS: `/opt/agateo/mcp-lab`. Operação em `docs/deploy.md`.

## Fluxo de trabalho

- Branch a partir da `main` (`feat/...`, `fix/...`, `chore/...`, `docs/...`), PR com squash merge.
- Build e testes precisam passar na CI (`.github/workflows/ci.yml`).
- Worktrees em `.worktrees/<nome>` (ignorado pelo git).
