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
- Entity Framework Core
- JWT Auth
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

Aplicar migrations do Entity Framework:

```bash
dotnet ef database update --project src/TagSeguranca.Api/TagSeguranca.Api.csproj
```

Executar a API:

```bash
dotnet run --project src/TagSeguranca.Api/TagSeguranca.Api.csproj
```

Testar health check:

```text
GET /health
```

## Cadastro de funções de funcionário

A branch `feature/funcoes-backend` adiciona o cadastro mestre de funções de funcionário.

Endpoints principais:

```text
GET    /api/funcoes-funcionario
GET    /api/funcoes-funcionario/opcoes
GET    /api/funcoes-funcionario/{id}
POST   /api/funcoes-funcionario
PUT    /api/funcoes-funcionario/{id}
DELETE /api/funcoes-funcionario/{id}
PATCH  /api/funcoes-funcionario/{id}/ativar
```

Para criar a tabela, use a migration do EF ou execute diretamente o script:

```text
database/scripts/20260619_add_funcoes_funcionario.sql
```

## Status

Projeto em evolução incremental. O backend já possui autenticação JWT, cadastros mestres, eventos, escalas, pagamentos, relatórios e dashboard em desenvolvimento contínuo.
