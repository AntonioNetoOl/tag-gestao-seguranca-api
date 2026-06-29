# Relatorio de Pagamentos

## Objetivo

Emitir o relatorio geral de pagamentos confirmados em Excel e PDF.

## Endpoints

```http
GET /api/relatorios/pagamentos/excel
GET /api/relatorios/pagamentos/pdf
```

## Filtros

- `dataInicio`: obrigatorio.
- `dataFim`: obrigatorio.
- `busca`: opcional, para funcionario, CPF, RG, chave Pix, evento ou casa.

## Validacoes

- Data inicial e data final sao obrigatorias.
- Data inicial nao pode ser maior que data final.

## Regra de data

O periodo informado usa a data operacional de Sao Paulo e e convertido para UTC antes da consulta. Assim, o relatorio fica alinhado com a listagem de pagamentos confirmados da tela financeira.

## Conteudo

O relatorio possui resumo por pagamento e detalhamento por evento.

Campos principais:

- Data do pagamento.
- Funcionario.
- CPF.
- RG.
- Chave Pix.
- Quantidade de eventos.
- Total de horas extras.
- Valor total pago.
- Evento.
- Casa.
- Valor da diaria.
- Valor da hora extra.
- Quantidade de horas extras.
- Total do evento.

## Arquivos alterados

- `src/TagSeguranca.Api/Controllers/RelatoriosController.cs`
- `src/TagSeguranca.Api/Application/Relatorios/Services/PagamentoExcelService.cs`
- `src/TagSeguranca.Api/Application/Relatorios/Services/PagamentosPdfService.cs`
- `src/TagSeguranca.Api/Program.cs`
