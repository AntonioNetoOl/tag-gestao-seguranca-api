# Relatórios de Escala — Backend

Etapa: `feature/relatorios-escala`.

## Objetivo

Preparar a API para emissão do relatório geral de escalas pela aba Relatórios do frontend.

## Endpoints

```text
GET /api/relatorios/escalas/excel
GET /api/relatorios/escalas/pdf
```

Parâmetros:

```text
casaId?      uuid opcional
dataInicio   date obrigatório
dataFim      date obrigatório
nomeEvento?  string opcional
```

## Validações

- `dataInicio` e `dataFim` são obrigatórias.
- `dataInicio` não pode ser maior que `dataFim`.
- O relatório geral considera apenas eventos com status `Escalado`.

## Regra operacional

O relatório geral da aba Relatórios deve representar escalas já finalizadas pelo usuário.

Por isso, a consulta exclui:

- eventos em `Rascunho`;
- eventos `Cancelado`;
- eventos `Finalizado`.

A emissão individual da escala continua disponível diretamente dentro do evento.

## Serviços

- `EscalaExcelService.GerarEscalaGeralAsync`: geração Excel.
- `EscalaPdfService.GerarEscalaGeralAsync`: geração PDF dedicada para escala geral.
