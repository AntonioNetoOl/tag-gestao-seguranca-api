# TAG Gestão de Segurança — API

API backend do sistema TAG Gestão de Segurança.

## Objetivo

Este repositório contém o backend responsável pelas regras de negócio, persistência, autenticação, eventos, escalas, pagamentos e relatórios do sistema TAG.

O frontend web fica em um repositório separado:

```text
tag-gestao-seguranca
```

## Stack proposta

- ASP.NET Core
- C#
- PostgreSQL
- Entity Framework Core, em etapa posterior
- JWT Auth, em etapa posterior
- Exportação de relatórios em Excel
- Rotina em background para finalização automática de eventos

## Estrutura inicial

```text
src/TagSeguranca.Api/       Projeto principal da API
tests/                      Testes automatizados
database/                   Scripts, seeds e documentação do banco
docs/                       Documentação técnica e regras da API
docker-compose.yml          PostgreSQL local para desenvolvimento
```

## Executar localmente

Subir o PostgreSQL local:

```bash
docker compose up -d
```

Executar a API:

```bash
dotnet run --project src/TagSeguranca.Api/TagSeguranca.Api.csproj
```

Testar health check:

```text
GET /health
```

## Status

Projeto em fase inicial de estruturação do backend.
