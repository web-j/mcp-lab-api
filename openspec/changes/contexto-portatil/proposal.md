## Why

A pessoa quer trocar de IA no meio de uma tarefa (acabaram os tokens no ChatGPT, continua no Grok) sem perder o fio.
Os dados de negócio já ficam no nosso banco, mas o contexto da conversa (o que foi feito, o que ficou pendente,
o porquê) se perde, porque nenhum app compartilha o histórico do chat. Precisamos guardar esse contexto no servidor
de um jeito que qualquer IA leia **gastando poucos tokens**, inclusive quando o histórico crescer.

Issue: [web-j/mcp-lab-api#5](https://github.com/web-j/mcp-lab-api/issues/5)

## What Changes

- **Novo módulo `Context`** (prefixo `context`), base dos outros módulos:
  - **Linha do tempo automática**: toda tool de escrita de qualquer módulo registra um evento curto
    (ex.: "Despesa R$ 80,00 · Mercado · hoje, pelo ChatGPT"), gerado por código no momento da escrita, sem IA.
  - **Notas da IA**: objetivos, pendências, decisões, preferências e "onde paramos", escritas pela própria IA,
    com limite de tamanho.
  - **Resumos por período**: o histórico antigo vira uma linha por semana e por módulo, calculada por código.
- **Leitura em camadas** para economizar tokens: um cartão de contexto pequeno e com teto fixo, busca e listagem
  sob demanda, e os detalhes ficam nas tools de cada módulo.
- **Instruções do servidor** (`instructions` do `initialize`): pedem à IA que leia o cartão no começo da conversa
  e salve pendências e "onde paramos" ao longo dela.
- **Mudança na plataforma**: tools de escrita passam a registrar o evento que geram (`IActivityRecorder`); um filtro
  grava o evento depois que a tool termina com sucesso.

Tools, resources e prompts novos:

| Nome | Tipo | Faz |
|---|---|---|
| `context_get_summary` | tool, leitura | Cartão "onde parei": pendências, último "onde paramos", últimas ações, resumo da semana |
| `context_search_history` | tool, leitura | Busca texto em eventos e notas (filtros por módulo e período), paginada |
| `context_list_activity` | tool, leitura | Linha do tempo paginada por cursor |
| `context_save_note` | tool, escrita | Salva objetivo, pendência, decisão, preferência ou "onde paramos" |
| `context_update_note` | tool, escrita | Conclui, reabre, fixa ou edita uma nota |
| `context_delete_note` | tool, destrutiva | Apaga uma nota |
| `context://summary` | resource | O mesmo cartão, para o cliente anexar ao contexto |
| `context_resume` | prompt | Roteiro "continuar de onde parei" |

## Custo em tokens

- Cartão padrão com teto de **1.500 caracteres (~400 tokens)**, com qualquer tamanho de histórico; `detail: full`
  com teto de 4.000 caracteres.
- Evento com no máximo 120 caracteres; nota com no máximo 280. O texto é gerado por código, então gravar não custa
  token nenhum.
- Eventos levam só uma referência à entidade (`finance:transaction:<id>`); o detalhe vem da tool do módulo, só
  quando necessário.
- As 6 tools somam cerca de 900 tokens de descrição em `tools/list`, o custo fixo por conversa.

## Capabilities

### New Capabilities

- `context`: linha do tempo, notas, cartão de contexto, busca e instruções de continuidade entre IAs.

### Modified Capabilities

- `mcp-platform`: instruções do servidor no `initialize` e registro automático de eventos para tools de escrita.

## Impact

- Projetos novos: `src/Modules/Context/McpLab.Context` e `McpLab.Context.Contracts` (`ActivityEntry` e
  `IActivityRecorder`, que os outros módulos usam para registrar o evento da escrita).
- `McpLab.Context`: filtro de chamada de tool que grava os eventos. `McpLab.Host`: `instructions` do servidor.
- Banco: schema `context` (eventos, notas, resumos, nomes dos apps conectados). Primeira migration do projeto, que
  define o padrão de EF Core por módulo.
- `McpLab.Mcp.Conventions`: helper para tools de escrita declararem o evento.
- Supabase: leitura do nome do app conectado (cliente OAuth) com a chave de serviço, guardado em cache.
- Testes:
  - **Contrato MCP**: as novas tools seguem as convenções; `instructions` presente no `initialize`.
  - **Integração**: o cartão respeita o teto com 10 mil eventos; tool de escrita sem evento falha nos testes.
  - **Arquitetura**: módulos só usam `Context.Contracts`.
  - **Integração** (Testcontainers + Postgres): isolamento entre usuários, evento gravado só em escrita com sucesso,
    resumo semanal, busca em português.
- ADR novo: **0010 – Contexto portátil e orçamento de tokens**.
