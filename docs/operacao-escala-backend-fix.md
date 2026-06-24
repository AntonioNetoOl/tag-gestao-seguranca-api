# Ajustes backend — Operação / Escala

Este arquivo registra o primeiro ajuste backend feito durante a etapa de Operação / Escala.

## Ajustes

- Corrigido o seed de funções padrão no `SchemaInitializer` para não depender de `ON CONFLICT (nome)`.
- O seed agora evita duplicidade por `id` ou por nome ativo, compatível com índice parcial por `ativo = true`.
- Removidos defaults redundantes do modelo EF para enums `Evento.Status` e `Pagamento.Status`, pois as entidades já inicializam os valores em código.

## Motivo

O PostgreSQL rejeitava `ON CONFLICT (nome)` porque não existe mais constraint única simples para `nome`. A unicidade atual é parcial/funcional, considerando apenas registros ativos.
