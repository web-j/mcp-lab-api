# 0002. MCP como única interface

- Estado: aceito
- Data: 2026-11-22

## Contexto

O objetivo é aprender a desenhar sistemas para agentes. Ter REST ao lado do MCP levaria a desenhar pensando em telas e depois adaptar.

## Decisão

Toda funcionalidade de negócio é exposta somente como tools, resources e prompts MCP. Não existem controllers nem endpoints REST. Fora do MCP há apenas `/health` e `/alive`.

## Consequências

- Não há front-end; humanos interagem por um cliente MCP (Claude, Cursor, Inspector).
- Integrações que esperam REST/webhooks ficam fora do escopo do ensaio.
