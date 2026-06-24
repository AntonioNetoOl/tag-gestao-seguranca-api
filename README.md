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
- Exportação de relatórios em Excel/PDF
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

## Operação de eventos e escala

A branch `feature/operacao-escala-evento` implementa a operação de eventos, montagem de escala, finalização explícita da escala, emissão de relatório e auditoria operacional.

Endpoints principais:

```text
GET    /api/eventos?apenasOperacao=true
POST   /api/eventos
PUT    /api/eventos/{id}
DELETE /api/eventos/{id}
GET    /api/eventos/{id}/escala/excel
GET    /api/eventos/{id}/escala/pdf
GET    /api/eventos/{eventoId}/funcionarios
POST   /api/eventos/{eventoId}/funcionarios
POST   /api/eventos/{eventoId}/funcionarios/finalizar
POST   /api/eventos/{eventoId}/funcionarios/cancelar-finalizacao
DELETE /api/eventos/{eventoId}/funcionarios/{funcionarioId}
POST   /api/eventos/{eventoId}/funcionarios/substituir
```

Regras centrais:

- evento nasce em `Rascunho`;
- adicionar funcionário não finaliza a escala;
- `Escalado` só ocorre ao confirmar `Finalizar escala`;
- evento `Escalado` bloqueia edição cadastral até cancelar a finalização da escala;
- evento `Finalizado` bloqueia edição cadastral;
- remoção em escala finalizada exige justificativa;
- vínculo pago não pode ser removido ou substituído;
- histórico de ações da escala é gravado em `evento_funcionarios_historico`.

Migration relacionada:

```text
20260624120000_AddEventoFuncionarioHistorico
```

Documentação complementar:

```text
docs/operacao-escala-backend-fix.md
```

## Status

Projeto em evolução incremental. O backend já possui autenticação JWT, cadastros mestres, eventos, escalas, pagamentos, relatórios e dashboard em desenvolvimento contínuo.
