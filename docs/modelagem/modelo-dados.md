# Modelo de Dados — Sistema TAG

## Entidades principais

- usuarios
- funcionarios
- casas
- tipos_evento
- eventos
- evento_funcionarios
- pagamentos
- pagamento_itens

## Pagamento pendente

Um vínculo deve aparecer como pendente quando:

```text
evento.status = Finalizado
evento_funcionario.pago = false
evento_funcionario.removido = false
```

## Observação

O modelo físico com tipos PostgreSQL, constraints, índices e relacionamentos será detalhado em etapa posterior.
