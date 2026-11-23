Issue: [web-j/mcp-lab-api#5](https://github.com/web-j/mcp-lab-api/issues/5)

## 1. Entrega 1: base do módulo e linha do tempo

- [x] 1.1 Padrão de persistência por módulo: EF Core + Npgsql, `DbContext` por schema em snake_case, migrations aplicadas na subida (`IModule.InitializeAsync`)
- [x] 1.2 `McpLab.Context.Contracts`: `ActivityEntry` e `IActivityRecorder`
- [x] 1.3 Módulo `Context`: schema `context` (events, notes, weekly_rollups, clients), gravação idempotente com rollup somado no banco (`context.jsonb_sum`)
- [x] 1.4 Filtro de `CallTool` que grava o evento de tools de escrita com sucesso (erro fora de produção quando a tool não registra); cache do nome do app (Supabase admin API, `SUPABASE_SECRET_KEY`)
- [x] 1.5 Testes: arquitetura (módulos e `PtBr.Money`), integração com Testcontainers (evento só em sucesso, leitura não grava, idempotência, soma semanal, isolamento entre usuários, tool que esquece o evento, app conectado em cache)
- [x] 1.6 ADR 0010; PR verde, merge e deploy (web-j/mcp-lab-api#7): schema `context` criado em produção

## 2. Entrega 2: notas, cartão e instruções

- [x] 2.1 Tools `context_save_note`, `context_update_note`, `context_delete_note` (limite de 280 caracteres com erro que ensina; número curto por usuário `#12` em vez do ID)
- [x] 2.2 `context_get_summary` com montagem por prioridade e teto (`brief` 1.500 / `full` 4.000) e resource `context://summary`
- [x] 2.3 `instructions` do servidor (≤ 600 caracteres) e prompt `context_resume`
- [x] 2.4 Testes: contrato (tools, resource, prompt, `instructions`, orçamento de 3.600 caracteres das descrições) e integração (troca de app, só o último `handoff`, nota longa, concluir/apagar, número inexistente, 10.000 eventos e 300 notas dentro do teto, isolamento, cartão vazio, resource e prompt)
- [x] 2.5 PR verde, merge e deploy (web-j/mcp-lab-api#9, #10): em produção, nota salva por um app aparece no cartão lido por outro, com o nome do app; refresh token emitido com `offline_access`

## 3. Entrega 3: busca e listagem

- [ ] 3.1 `context_search_history` (tsvector em português com unaccent, filtros, cursor) e `context_list_activity`
- [ ] 3.2 Testes de integração: busca sem acento, paginação, filtros por módulo e período
- [ ] 3.3 PR verde, merge e deploy

## 4. Validação entre apps

- [ ] 4.1 Primeira tool de escrita real (módulo Finanças) declarando evento, para o cenário "R$ 80 de mercado"
- [ ] 4.2 Teste manual: lançar pelo ChatGPT, retomar pelo Grok ou pelo Claude com "onde eu parei?"; registrar quais apps respeitam `instructions`
- [ ] 4.3 Atualizar `docs/mcp-guidelines.md` (escrita declara evento; leitura em camadas; teto do cartão) e `docs/architecture.md`
