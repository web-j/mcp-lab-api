# 0001. Monolito modular

- Estado: aceito
- Data: 2026-11-22

## Contexto

O ensaio precisa de vários domínios (finanças, agendamento e outros) sem o custo operacional de microsserviços.

## Decisão

Um único deploy com módulos isolados. Cada módulo é um projeto com camadas em pastas e um projeto `*.Contracts` para o que pode ser usado por outros módulos. Módulos se registram no host pela interface `IModule`.

## Consequências

- Fronteiras garantidas por `McpLab.ArchitectureTests`.
- Um módulo pode virar serviço separado no futuro sem reescrever o domínio.
- Comunicação entre módulos é in-process (interfaces e eventos de integração).
