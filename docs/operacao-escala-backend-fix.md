# Ajustes backend — Operação / Escala

Este arquivo registra os ajustes backend feitos durante a etapa de Operação / Escala.

## Ajustes

- Corrigido o seed de funções padrão no `SchemaInitializer` para não depender de `ON CONFLICT (nome)`.
- O seed agora evita duplicidade por `id` ou por nome ativo, compatível com índice parcial por `ativo = true`.
- Removidos defaults redundantes do modelo EF para enums `Evento.Status` e `Pagamento.Status`, pois as entidades já inicializam os valores em código.
- Adicionado filtro operacional em `GET /api/eventos?apenasOperacao=true`, ocultando eventos cancelados e finalizados fora da janela de 24h.
- Ajustado fluxo de escala: adicionar/substituir funcionário não muda mais o status do evento para `Escalado`.
- Criado endpoint `POST /api/eventos/{eventoId}/funcionarios/finalizar` para finalizar explicitamente a escala e marcar o evento como `Escalado`.

## Motivo

O PostgreSQL rejeitava `ON CONFLICT (nome)` porque não existe mais constraint única simples para `nome`. A unicidade atual é parcial/funcional, considerando apenas registros ativos.

Na operação, o status `Escalado` deve representar escala finalizada pelo usuário, não apenas a existência de funcionário vinculado.
