# 0005. Convenções de nome e descrição das tools

- Estado: aceito
- Data: 2026-11-22

## Contexto

O modelo escolhe tools pelo nome e pela descrição. Inconsistência piora a escolha e gasta tokens.

## Decisão

Nomes `modulo_verbo_objeto` em snake case com prefixo do módulo (`system` para o host), `Title`, descrição dizendo quando usar e quando não usar, as quatro anotações e saída estruturada. Detalhes em `docs/mcp-guidelines.md`.

## Consequências

- `McpLab.McpContractTests` valida todas as tools registradas; uma tool fora do padrão quebra o build.
- O guia evolui com o ensaio; mudanças de regra atualizam o teste no mesmo PR.
