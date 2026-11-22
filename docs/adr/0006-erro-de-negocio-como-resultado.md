# 0006. Erro de negócio como resultado

- Estado: aceito
- Data: 2026-11-22

## Contexto

Uma exceção interrompe o agente com uma mensagem genérica. Um erro legível permite que ele corrija a chamada sozinho.

## Decisão

O domínio devolve `Result<T>` com `Error` (SharedKernel). A camada MCP converte erro em resultado com `isError` e uma mensagem que diz o que deu errado e qual tool usar para corrigir. Exceções ficam para falhas inesperadas.

## Consequências

- O helper de conversão (`Result` → resultado de tool) entra junto com a primeira tool real, na fase 1.
