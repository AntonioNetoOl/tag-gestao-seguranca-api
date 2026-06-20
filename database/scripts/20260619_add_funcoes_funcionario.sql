CREATE TABLE IF NOT EXISTS funcoes_funcionario (
    id uuid NOT NULL,
    nome character varying(100) NOT NULL,
    ativo boolean NOT NULL DEFAULT true,
    usuario_criacao_id uuid NULL,
    data_criacao timestamp with time zone NOT NULL DEFAULT NOW(),
    usuario_alteracao_id uuid NULL,
    data_alteracao timestamp with time zone NULL,
    CONSTRAINT pk_funcoes_funcionario PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_funcoes_funcionario_nome
    ON funcoes_funcionario (nome);

INSERT INTO funcoes_funcionario (id, nome, ativo, data_criacao)
VALUES
    ('11111111-1111-1111-1111-111111111001', 'Segurança', true, NOW()),
    ('11111111-1111-1111-1111-111111111002', 'Líder', true, NOW()),
    ('11111111-1111-1111-1111-111111111003', 'Coordenador', true, NOW())
ON CONFLICT (nome) DO NOTHING;

ALTER TABLE funcionarios
    ADD COLUMN IF NOT EXISTS funcao_funcionario_id uuid NULL;

CREATE INDEX IF NOT EXISTS ix_funcionarios_funcao_funcionario_id
    ON funcionarios (funcao_funcionario_id);

UPDATE funcionarios f
SET funcao_funcionario_id = ff.id
FROM funcoes_funcionario ff
WHERE lower(f.funcao) = lower(ff.nome)
  AND f.funcao_funcionario_id IS NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_funcionarios_funcoes_funcionario_funcao_funcionario_id'
    ) THEN
        ALTER TABLE funcionarios
            ADD CONSTRAINT fk_funcionarios_funcoes_funcionario_funcao_funcionario_id
            FOREIGN KEY (funcao_funcionario_id)
            REFERENCES funcoes_funcionario(id)
            ON DELETE RESTRICT;
    END IF;
END $$;
