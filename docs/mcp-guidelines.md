# Guia de tools MCP

> Rascunho. Este documento é o principal resultado do ensaio e vai sendo ajustado a cada fase.
> Quando uma regra é garantida por teste, o teste está indicado.

## Desenho

1. **Uma tool por intenção, não por tabela.** O agente pensa em tarefas ("lancei uma despesa"), não em CRUD.
   `finance_record_expense` em vez de `insert_transaction`.
2. **Nome `modulo_verbo_objeto`** em snake case, com o prefixo do módulo (ou `system` para tools do host).
   Evita colisão com outros servidores no mesmo cliente. *Teste:* `ToolConventionTests`.
3. **Orçamento de tools.** Toda tool listada custa tokens em toda conversa. Meta: até 12 por módulo.

## Descrição

4. **A descrição é um prompt.** Diz o que a tool faz, quando usar, quando *não* usar e qual tool usar no lugar.
   Mínimo de 60 caracteres. *Teste:* `ToolConventionTests`.
5. **Todo parâmetro tem `[Description]`** com unidade, formato e exemplo (`"Valor em reais, positivo. Ex.: 80.50"`).
6. **`Title`** curto e legível para humanos (aparece na interface do cliente). *Teste:* `ToolConventionTests`.

## Anotações

7. **As quatro anotações sempre declaradas:** `ReadOnly`, `Destructive`, `Idempotent`, `OpenWorld`.
   É com elas que o cliente decide quando pedir confirmação. *Teste:* `ToolConventionTests`.

## Entrada e saída

8. **Saída estruturada.** `UseStructuredContent = true` (gera `outputSchema`) e um record de retorno. *Teste:* `ToolConventionTests`.
9. **Respostas enxutas.** Devolver nomes legíveis junto dos IDs; paginação por cursor; `detail: concise | full` quando houver muito dado.
10. **Tipos sem ambiguidade.** Dinheiro em `decimal` com moeda, datas em ISO 8601 com fuso, enums no lugar de texto livre.

## Erros

11. **Erro de negócio é resultado com `isError`, não exceção.** A mensagem diz o que deu errado e como corrigir,
    citando a tool que resolve: `"Categoria 'mercado' não existe. Use finance_list_categories ou finance_create_category."`

## Escrita segura

12. **Toda escrita aceita `idempotency_key`.**
13. **Ações destrutivas pedem confirmação**, por elicitation ou em duas etapas (prévia e depois confirmar).

## Contexto portátil (ADR 0010)

14. **Toda tool de escrita registra o que fez** com `IActivityRecorder.Record(new ActivityEntry(...))`: resumo de até
    120 caracteres montado por código, referência `modulo:tipo:id` e, quando fizer sentido, números para o resumo
    semanal (ex.: `amount`). Use a `idempotency_key` da escrita. *Teste:* a tool de escrita que não registra devolve
    erro fora de produção (`ActivityTimelineTests`).
15. **Textos sempre em pt-BR**, com `PtBr.Money` para valores (`R$ 80,00`), independentemente da cultura do servidor.
    *Teste:* `PtBrTests`.

## Outras primitivas

16. **Resources** para dados que o cliente anexa ao contexto (`finance://accounts`).
17. **Prompts** para fluxos de várias etapas (`monthly_close`).

## Identidade

18. **Toda tool trabalha em nome de quem está conectado.** Use `ICurrentUser`; nunca receba o ID do usuário como
    parâmetro da tool. Todo dado é filtrado por `ICurrentUser.Id`.
19. **Permissão no nosso código.** O Supabase não emite escopos por módulo (ADR 0007), então regras como
    "só leitura" ou "módulo bloqueado" ficam no host/módulo.

## Idioma

20. Descrições, títulos e mensagens em português do Brasil, porque é o idioma de quem conversa com o agente.
    Hipótese a validar nos evals da fase 4.
