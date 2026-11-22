# 0010. Contexto portátil entre IAs e orçamento de tokens

- Estado: aceito
- Data: 2026-11-22

## Contexto

A pessoa troca de app de IA (ChatGPT, Grok, Claude...) no meio de uma tarefa e quer continuar de onde parou.
Os dados de negócio já ficam no nosso banco, mas nenhum app compartilha o histórico do chat, e reler muito histórico
custa token. Plano completo em `openspec/changes/contexto-portatil/` (issue #5).

## Decisão

- **Módulo `Context`** como base da plataforma: linha do tempo das escritas, notas da IA e cartão "onde parei".
  Os módulos de negócio só conhecem `McpLab.Context.Contracts`.
- **Toda tool de escrita registra um evento** com `IActivityRecorder.Record(new ActivityEntry(...))`: resumo de até
  120 caracteres montado por código (zero token para gravar), referência à entidade e números para o resumo semanal.
  Um filtro de `CallTool` grava o evento só quando a tool termina com sucesso; idempotente pela chave da escrita.
- **Tool de escrita que não registra falha** em desenvolvimento e testes (e gera log de erro em produção). Isso
  pega o esquecimento nos testes de integração de cada módulo.
- **Leitura em camadas com teto fixo**: cartão de até 1.500 caracteres, busca paginada sob demanda e detalhe só na
  tool do módulo. O histórico antigo vira uma linha por semana (`weekly_rollups`, somado no banco a cada evento).
- **Textos em pt-BR sempre** (`PtBr.Money`), independentemente da cultura do servidor, e só com espaço comum.

## Alternativas descartadas

- `WriteResult<T>` como retorno das tools: o SDK colocaria o evento no `outputSchema` e na resposta para a IA,
  gastando token sem utilidade. O gravador por requisição fica fora do que a IA vê.
- Resumo por LLM no servidor, guardar a conversa inteira e embeddings desde o início (motivos no design do change).

## Consequências

- O evento é gravado logo depois da escrita, fora da transação do módulo (cada módulo tem seu `DbContext`). Se
  falhar, a escrita continua valendo e o erro vai para o log; uma outbox entra quando houver consumidor que exija garantia.
- O nome do app conectado vem da API admin do Supabase (`SUPABASE_SECRET_KEY`), em cache por 7 dias em `context.clients`.
- Semana do resumo calculada no fuso de São Paulo. Fuso por usuário fica para quando houver usuários fora do Brasil.
