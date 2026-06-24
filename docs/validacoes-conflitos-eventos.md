# Validações de conflito — Eventos e Escalas

Etapa: `feature/validacoes-conflito-eventos`.

## Objetivo

Evitar inconsistências operacionais na criação/edição de eventos e na montagem da escala.

## Regras implementadas

### Conflito de casa

Não é permitido criar ou editar um evento usando a mesma casa em período conflitante com outro evento ativo.

A validação considera eventos com status diferente de `Cancelado`.

Critério de conflito:

```text
inicio_evento_novo < fim_evento_existente
E
inicio_evento_existente < fim_evento_novo
```

Eventos que terminam exatamente no início de outro evento não conflitam.

Exemplo permitido:

```text
Evento A: 06:00 - 22:00
Evento B: 22:00 - 04:00
```

Exemplo bloqueado:

```text
Evento A: 06:00 - 22:00
Evento B: 06:00 - 21:00
```

### Eventos que cruzam meia-noite

Quando `horaFim` é menor que `horaInicio`, o fim do evento é considerado no dia seguinte.

Exemplo:

```text
24/06/2026 19:00 até 25/06/2026 04:00
```

### Conflito de funcionário

Não é permitido vincular, reativar ou substituir um funcionário para evento cujo período conflite com outro evento ativo em que ele já esteja vinculado.

A mensagem informa:

- nome do funcionário;
- nome do evento conflitante;
- casa do evento conflitante;
- data/período;
- horário.

### Edição de evento com escala em rascunho

Mesmo em `Rascunho`, se o evento já possuir funcionários vinculados, a edição de data/horário/casa valida:

- conflito de casa;
- conflito de cada funcionário já vinculado.

### Finalização de escala

Antes de mudar o evento para `Escalado`, o backend valida novamente:

- conflito da casa;
- conflito dos funcionários vinculados.

Isso evita finalizar escalas inconsistentes criadas antes da validação ou por chamadas diretas à API.

### Horário igual

O backend agora também bloqueia `horaInicio == horaFim`, mantendo a mesma regra já existente no frontend.

## Pontos de entrada protegidos

```text
POST /api/eventos
PUT  /api/eventos/{id}
POST /api/eventos/{eventoId}/funcionarios
POST /api/eventos/{eventoId}/funcionarios/substituir
POST /api/eventos/{eventoId}/funcionarios/finalizar
```

## Observação

As validações foram implementadas no backend para impedir bypass pelo frontend, DevTools ou chamadas diretas à API.
