# Regras da API — Sistema TAG

## Funcionários

- CPF deve ser válido e único.
- RG deve ser informado e único.
- Funcionário inativo não pode ser vinculado a novos eventos.
- Funcionário inativo deve permanecer no histórico.

## Eventos

- Evento deve possuir casa, tipo, nome, data, hora início, hora fim, diária e valor de hora extra.
- Evento nasce como Rascunho.
- Evento com funcionários vinculados pode ficar Escalado.
- Evento Escalado deve ser finalizado automaticamente após a data e hora fim.
- Evento Cancelado não gera pagamento.
- Evento Finalizado não pode ser cancelado.

## Escalas

- Apenas funcionários ativos podem ser adicionados a novos eventos.
- Funcionário não pago pode ser removido de evento finalizado.
- Funcionário pago não pode ser removido.
- Remoção deve ser lógica, usando campo removido.

## Pagamentos

- Pagamento não pode ser parcial.
- Ao confirmar pagamento, todos os eventos pendentes do funcionário são pagos.
- Horas extras são informadas manualmente no detalhe do pagamento.
- Pagamento confirmado não pode ser editado, cancelado ou estornado.
- Eventos pagos não aparecem novamente como pendentes.

## Relatórios

- A API deverá gerar escala em Excel.
- Relatório de pagamento será detalhado posteriormente.
