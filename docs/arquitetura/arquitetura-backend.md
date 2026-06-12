# Arquitetura Backend — Sistema TAG

## Objetivo

Este repositório contém a API backend do sistema TAG Gestão de Segurança.

## Responsabilidades

- Autenticação do usuário master
- Cadastros
- Eventos
- Escalas
- Pagamentos pendentes
- Confirmação de pagamentos
- Relatórios em Excel
- Finalização automática de eventos

## Stack

- ASP.NET Core
- C#
- PostgreSQL
- Entity Framework Core em etapa posterior

## Organização sugerida

```text
src/TagSeguranca.Api/
  Controllers/
  Application/
  Domain/
  Infrastructure/
  Reports/
  BackgroundServices/
```

## Integração com frontend

O frontend consumirá esta API por HTTP/REST.

## Rotina de finalização automática

A API deverá possuir uma rotina em background para finalizar eventos quando a data e hora fim forem atingidas.

Critério:

```text
status = Escalado
data_evento + hora_fim <= data_hora_atual
```

Ao finalizar, os vínculos de funcionários não removidos ficam disponíveis como pendência de pagamento.
