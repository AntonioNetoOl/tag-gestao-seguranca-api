using Microsoft.EntityFrameworkCore;

namespace TagSeguranca.Api.Infrastructure.Persistence;

public static class SchemaInitializer
{
    public static async Task EnsureFuncoesFuncionarioSchemaAsync(TagDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.ExecuteSqlRawAsync("""
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
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO funcoes_funcionario (id, nome, ativo, data_criacao)
            SELECT v.id, v.nome, true, NOW()
            FROM (VALUES
                (CAST('11111111-1111-1111-1111-111111111001' AS uuid), 'Segurança'),
                (CAST('11111111-1111-1111-1111-111111111002' AS uuid), 'Líder'),
                (CAST('11111111-1111-1111-1111-111111111003' AS uuid), 'Coordenador')
            ) AS v(id, nome)
            WHERE NOT EXISTS (
                SELECT 1
                FROM funcoes_funcionario ff
                WHERE ff.id = v.id
                   OR (ff.ativo = true AND lower(ff.nome) = lower(v.nome))
            );
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE funcionarios
                ADD COLUMN IF NOT EXISTS funcao_funcionario_id uuid NULL;
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_funcionarios_funcao_funcionario_id
                ON funcionarios (funcao_funcionario_id);
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            UPDATE funcionarios f
            SET funcao_funcionario_id = ff.id
            FROM funcoes_funcionario ff
            WHERE lower(f.funcao) = lower(ff.nome)
              AND f.funcao_funcionario_id IS NULL;
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
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
            """, cancellationToken);

        await EnsureTiposEventoSchemaAsync(context, cancellationToken);
        await EnsureEventoFuncionarioHistoricoSchemaAsync(context, cancellationToken);
        await EnsureSoftDeleteUniqueIndexesAsync(context, cancellationToken);
    }

    public static async Task EnsureTiposEventoSchemaAsync(TagDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE tipos_evento
                ADD COLUMN IF NOT EXISTS ativo boolean NOT NULL DEFAULT true;
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            UPDATE tipos_evento
            SET ativo = true
            WHERE ativo IS NULL;
            """, cancellationToken);
    }

    private static async Task EnsureEventoFuncionarioHistoricoSchemaAsync(TagDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS evento_funcionarios_historico (
                id uuid NOT NULL,
                evento_id uuid NOT NULL,
                evento_funcionario_id uuid NULL,
                funcionario_anterior_id uuid NULL,
                funcionario_novo_id uuid NULL,
                acao character varying(50) NOT NULL,
                motivo character varying(500) NULL,
                observacao character varying(1000) NULL,
                usuario_acao_id uuid NULL,
                data_acao timestamp with time zone NOT NULL DEFAULT NOW(),
                CONSTRAINT pk_evento_funcionarios_historico PRIMARY KEY (id)
            );
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_evento_funcionarios_historico_evento_id
                ON evento_funcionarios_historico (evento_id);

            CREATE INDEX IF NOT EXISTS ix_evento_funcionarios_historico_evento_funcionario_id
                ON evento_funcionarios_historico (evento_funcionario_id);

            CREATE INDEX IF NOT EXISTS ix_evento_funcionarios_historico_data_acao
                ON evento_funcionarios_historico (data_acao);
            """, cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'fk_evento_funcionarios_historico_eventos_evento_id'
                ) THEN
                    ALTER TABLE evento_funcionarios_historico
                        ADD CONSTRAINT fk_evento_funcionarios_historico_eventos_evento_id
                        FOREIGN KEY (evento_id)
                        REFERENCES eventos(id)
                        ON DELETE RESTRICT;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'fk_evento_funcionarios_historico_evento_funcionarios_evento_funcionario_id'
                ) THEN
                    ALTER TABLE evento_funcionarios_historico
                        ADD CONSTRAINT fk_evento_funcionarios_historico_evento_funcionarios_evento_funcionario_id
                        FOREIGN KEY (evento_funcionario_id)
                        REFERENCES evento_funcionarios(id)
                        ON DELETE RESTRICT;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'fk_evento_funcionarios_historico_funcionarios_funcionario_anterior_id'
                ) THEN
                    ALTER TABLE evento_funcionarios_historico
                        ADD CONSTRAINT fk_evento_funcionarios_historico_funcionarios_funcionario_anterior_id
                        FOREIGN KEY (funcionario_anterior_id)
                        REFERENCES funcionarios(id)
                        ON DELETE RESTRICT;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'fk_evento_funcionarios_historico_funcionarios_funcionario_novo_id'
                ) THEN
                    ALTER TABLE evento_funcionarios_historico
                        ADD CONSTRAINT fk_evento_funcionarios_historico_funcionarios_funcionario_novo_id
                        FOREIGN KEY (funcionario_novo_id)
                        REFERENCES funcionarios(id)
                        ON DELETE RESTRICT;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'fk_evento_funcionarios_historico_usuarios_usuario_acao_id'
                ) THEN
                    ALTER TABLE evento_funcionarios_historico
                        ADD CONSTRAINT fk_evento_funcionarios_historico_usuarios_usuario_acao_id
                        FOREIGN KEY (usuario_acao_id)
                        REFERENCES usuarios(id)
                        ON DELETE SET NULL;
                END IF;
            END $$;
            """, cancellationToken);
    }

    private static async Task EnsureSoftDeleteUniqueIndexesAsync(TagDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync("""
            DROP INDEX IF EXISTS ix_funcionarios_cpf;
            DROP INDEX IF EXISTS ix_funcionarios_rg;
            DROP INDEX IF EXISTS ix_usuarios_email;
            DROP INDEX IF EXISTS ix_funcoes_funcionario_nome;
            DROP INDEX IF EXISTS ix_tipos_evento_nome;

            CREATE UNIQUE INDEX IF NOT EXISTS ix_funcionarios_cpf_ativo
                ON funcionarios (cpf)
                WHERE ativo = true;

            CREATE UNIQUE INDEX IF NOT EXISTS ix_funcionarios_rg_ativo
                ON funcionarios (lower(rg))
                WHERE ativo = true;

            CREATE UNIQUE INDEX IF NOT EXISTS ix_usuarios_email_ativo
                ON usuarios (lower(email))
                WHERE ativo = true;

            CREATE UNIQUE INDEX IF NOT EXISTS ix_funcoes_funcionario_nome_ativo
                ON funcoes_funcionario (lower(nome))
                WHERE ativo = true;

            CREATE UNIQUE INDEX IF NOT EXISTS ix_tipos_evento_nome_ativo
                ON tipos_evento (lower(nome))
                WHERE ativo = true;
            """, cancellationToken);
    }
}
