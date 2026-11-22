# auth-portal Specification

## Purpose

Página `https://auth.agateo.com.br` (`src/McpLab.Auth`): conta, login, cadastro, consentimento OAuth e instruções
de conexão para pessoas e IAs. Decisões nos ADRs 0007 e 0008.

## Requirements

### Requirement: Conta por e-mail e senha

A página SHALL permitir cadastro com e-mail e senha (mínimo 10 caracteres, com minúscula, maiúscula e número),
confirmação obrigatória por e-mail, login e redefinição de senha.

#### Scenario: Cadastro durante uma conexão

- **GIVEN** a pessoa chegou pela tela de consentimento de um app
- **WHEN** cria a conta
- **THEN** recebe o e-mail de confirmação e o link a devolve para a mesma tela de consentimento

### Requirement: Consentimento OAuth

Em `/oauth/consent?authorization_id=...` a página SHALL mostrar o nome do app, a conta, para onde o app volta e os
escopos, com Permitir e Recusar, e SHALL tratar solicitação expirada pedindo para conectar de novo pelo app.

#### Scenario: Pessoa permite o acesso

- **GIVEN** a pessoa está logada e a autorização é válida
- **WHEN** clica em Permitir
- **THEN** volta para o app com o código de autorização

### Requirement: Instruções de conexão

`/conectar` SHALL ser HTML estático legível sem JavaScript, com o endereço do servidor, o texto para colar na IA,
o passo a passo por app e os dados técnicos para agentes. `/llms.txt` SHALL ter o mesmo conteúdo em Markdown.

#### Scenario: IA lê as instruções

- **WHEN** uma IA busca `https://auth.agateo.com.br/llms.txt`
- **THEN** recebe o endpoint, o tipo de autenticação e onde adicionar o servidor em cada app
