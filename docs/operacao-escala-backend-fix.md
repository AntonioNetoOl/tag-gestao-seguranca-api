# Ajustes backend — Operação / Escala

Este arquivo registra os ajustes backend feitos durante a etapa de Operação / Escala.

## Ajustes

- Corrigido o seed de funções padrão no `SchemaInitializer` para não depender de `ON CONFLICT (nome)`.
- O seed agora evita duplicidade por `id` ou por nome ativo, compatível com índice parcial por `ativo = true`.
- Removidos defaults redundantes do modelo EF para enums `Evento.Status` e `Pagamento.Status`, pois as entidades já inicializam os valores em código.
- Adicionado filtro operacional em `GET /api/eventos?apenasOperacao=true`, ocultando eventos cancelados e finalizados fora da janela de 24h.
- Ajustado fluxo de escala: adicionar/substituir funcionário não muda mais o status do evento para `Escalado`.
- Criado endpoint `POST /api/eventos/{eventoId}/funcionarios/finalizar` para finalizar explicitamente a escala e marcar o evento como `Escalado`.
- Criado endpoint `POST /api/eventos/{eventoId}/funcionarios/cancelar-finalizacao` para voltar a escala de `Escalado` para `Rascunho`, com bloqueio para evento finalizado/cancelado e vínculo pago.
- Criada tabela `evento_funcionarios_historico` para auditar as ações de escala: adicionar, reativar, remover, substituir, finalizar escala e cancelar finalização.
- Cada histórico registra evento, vínculo quando aplicável, funcionário anterior, funcionário novo, ação, motivo, observação, usuário e data da ação.
- Adicionada migration `20260624120000_AddEventoFuncionarioHistorico` para versionar formalmente a criação da tabela de histórico.

## Motivo

O PostgreSQL rejeitava `ON CONFLICT (nome)` porque não existe mais constraint única simples para `nome`. A unicidade atual é parcial/funcional, considerando apenas registros ativos.

Na operação, o status `Escalado` deve representar escala finalizada pelo usuário, não apenas a existência de funcionário vinculado.

A tabela `evento_funcionarios` representa o estado atual da escala. O rastreio operacional fica separado em `evento_funcionarios_historico`, evitando misturar estado atual com auditoria e preparando a base para relatórios futuros.
