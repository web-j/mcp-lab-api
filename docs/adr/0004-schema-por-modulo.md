# 0004. Um schema Postgres por módulo

- Estado: aceito
- Data: 2026-11-22

## Contexto

Os módulos precisam de isolamento de dados sem a complexidade de vários bancos.

## Decisão

Um banco PostgreSQL (`mcplab`) com um schema por módulo (`finance`, `scheduling`) e um schema `platform` para auditoria e idempotência. Cada módulo tem seu `DbContext` e sua tabela de migrations no próprio schema.

## Consequências

- Um módulo nunca lê tabelas de outro; usa o `*.Contracts`.
- Separar um módulo em outro banco exige só trocar a connection string.
