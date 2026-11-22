## ADDED Requirements

### Requirement: Linha do tempo automática

Toda tool de escrita (`readOnlyHint = false`) de outros módulos que terminar com sucesso SHALL gerar um evento na linha do tempo do
usuário com: data e hora, módulo, tool, app conectado (nome do cliente OAuth), resumo de no máximo 120 caracteres
gerado por código e referência à entidade afetada. Tools que falham ou só leem SHALL NOT gerar evento. A mesma
chamada repetida com a mesma `idempotency_key` SHALL gerar um único evento.

#### Scenario: Lançamento pelo ChatGPT vira evento

- **GIVEN** a pessoa está conectada pelo ChatGPT
- **WHEN** a IA chama a tool de Finanças que lança uma despesa de R$ 80,00 em Mercado
- **THEN** a linha do tempo ganha o evento "Despesa R$ 80,00 · Mercado" com o app "ChatGPT" e a referência ao lançamento

#### Scenario: Erro não vira evento

- **WHEN** a tool de escrita devolve erro de negócio (`isError`)
- **THEN** nenhum evento é gravado

### Requirement: Cartão de contexto com teto fixo

`context_get_summary` SHALL devolver o contexto do usuário em até 1.500 caracteres no modo `brief` (padrão) e até
4.000 no modo `full`, qualquer que seja o tamanho do histórico. O mesmo conteúdo SHALL estar no resource
`context://summary`.

#### Scenario: Histórico grande continua barato

- **GIVEN** um usuário com 10.000 eventos e 300 notas
- **WHEN** a IA chama `context_get_summary` sem parâmetros
- **THEN** a resposta tem no máximo 1.500 caracteres de conteúdo

### Requirement: Prioridade do conteúdo do cartão

O cartão SHALL trazer, nesta ordem: o último "onde paramos", notas fixadas, pendências e objetivos abertos, as
últimas ações e uma linha de resumo por módulo dos últimos 7 dias. Quando passar do teto, os itens de menor
prioridade SHALL ser cortados, e o cartão SHALL dizer quantos ficaram de fora e qual tool usar para vê-los.

#### Scenario: Trocar de IA no meio da tarefa

- **GIVEN** a pessoa lançou R$ 80,00 de mercado pelo ChatGPT e a IA salvou o "onde paramos": "montando o orçamento de dezembro, falta definir lazer"
- **WHEN** a pessoa abre o Grok e pergunta "onde eu parei?"
- **AND** o Grok chama `context_get_summary`
- **THEN** o cartão mostra o "onde paramos", a pendência "definir lazer" e a ação "Despesa R$ 80,00 · Mercado · pelo ChatGPT"

#### Scenario: Itens além do teto

- **GIVEN** 40 pendências abertas
- **WHEN** a IA chama `context_get_summary`
- **THEN** o cartão mostra as que cabem e uma linha como "+34 pendências: use context_search_history"

### Requirement: Notas da IA

`context_save_note` SHALL guardar uma nota do tipo `goal`, `pending`, `decision`, `preference`, `fact` ou `handoff`,
com texto de até 280 caracteres, módulo opcional e opção de fixar. Texto maior SHALL ser recusado com erro que diz
o limite e pede para resumir. Só o `handoff` ("onde paramos") mais recente SHALL aparecer no cartão.

#### Scenario: IA salva onde a conversa parou

- **WHEN** a pessoa diz "salva onde a gente parou"
- **AND** a IA chama `context_save_note` com tipo `handoff` e o texto "orçamento de dezembro: faltam lazer e presentes"
- **THEN** esse passa a ser o "onde paramos" mostrado no cartão para qualquer app

#### Scenario: Nota longa demais

- **WHEN** a IA envia uma nota com 600 caracteres
- **THEN** recebe erro dizendo que o limite é 280 caracteres e que deve resumir

### Requirement: Manutenção das notas

`context_update_note` SHALL concluir, reabrir, fixar, desafixar ou editar uma nota, e `context_delete_note` SHALL
apagá-la. Notas concluídas SHALL sair do cartão e continuar na busca.

#### Scenario: Pendência concluída sai do cartão

- **GIVEN** a pendência "definir lazer" está aberta
- **WHEN** a IA chama `context_update_note` marcando a nota como concluída
- **THEN** a pendência deixa de aparecer no cartão e continua na busca

### Requirement: Busca e listagem sob demanda

`context_search_history` SHALL buscar texto (em português, sem diferenciar acentos) em eventos e notas, com filtros
de módulo, tipo e período, devolvendo no máximo 20 itens por página com cursor. `context_list_activity` SHALL listar
a linha do tempo do mais recente para o mais antigo, paginada por cursor, com filtros de módulo e período.

#### Scenario: Buscar um lançamento antigo

- **GIVEN** a pessoa lançou "farmácia" três meses atrás
- **WHEN** a IA chama `context_search_history` com o texto "farmacia"
- **THEN** recebe o evento do lançamento com a referência à entidade

### Requirement: Resumo por período

O sistema SHALL manter, por usuário, módulo e semana, uma linha de resumo calculada por código a partir dos eventos
(ex.: "Semana 47: 12 despesas, R$ 1.240,00"). O cartão SHALL usar esses resumos no lugar dos eventos individuais
com mais de 7 dias.

#### Scenario: Semana passada resumida

- **GIVEN** 30 eventos de Finanças na semana passada
- **WHEN** a IA chama `context_get_summary`
- **THEN** a semana passada aparece como uma única linha de resumo

### Requirement: Isolamento e controle do usuário

Eventos e notas SHALL pertencer ao usuário do token e nunca aparecer para outro usuário. O texto das notas SHALL ser
tratado como dado: o cartão SHALL marcar o conteúdo como anotações do usuário, não como instruções.

#### Scenario: Dois usuários

- **GIVEN** Ana e Bruno com notas próprias
- **WHEN** a IA conectada na conta do Bruno chama `context_get_summary` ou `context_search_history`
- **THEN** não aparece nada da Ana

### Requirement: Retomar conversa

O prompt `context_resume` SHALL entregar ao cliente um roteiro que lê o cartão, resume para a pessoa onde ela parou
e pergunta como quer continuar.

#### Scenario: Pessoa usa o prompt no Claude

- **WHEN** a pessoa escolhe o prompt `context_resume`
- **THEN** a IA recebe o cartão de contexto e a instrução de resumir e perguntar como continuar
