# Pagamentos — Backend

Etapa: `feature/pagamentos-v1`.

## Objetivo

Apoiar a tela financeira de pagamentos pendentes e confirmados.

## Regras principais

- Apenas vínculos de eventos `Finalizado`, não removidos e ainda não pagos aparecem como pendência.
- O pagamento é sempre por funcionário.
- O pagamento não pode ser parcial: todos os eventos pendentes do funcionário precisam ser enviados na confirmação.
- Horas extras são informadas manualmente por evento no momento do pagamento.
- A quantidade de horas extras por evento deve ficar entre 0 e 24.
- Ao confirmar pagamento, o sistema cria `pagamentos`, cria `pagamento_itens` e marca os vínculos `evento_funcionarios.pago = true`.
- Pagamento confirmado não possui edição, cancelamento ou estorno nesta versão.

## Endpoints

```text
GET  /api/pagamentos/pendentes
GET  /api/pagamentos/pendentes/{funcionarioId}
POST /api/pagamentos/confirmar
GET  /api/pagamentos
GET  /api/pagamentos/{id}
```

## Ajustes implementados

- Respostas de pendência passam a retornar a função do funcionário.
- Confirmação passa a gravar o usuário responsável pelo pagamento em `usuario_pagamento_id`.
- Vínculos pagos passam a receber `usuario_alteracao_id` e `data_alteracao`.
- Filtro de data dos pagamentos confirmados considera a data operacional de São Paulo, convertendo o intervalo para UTC.
- Mensagem de concorrência foi melhorada quando o funcionário já não possui pendências.
