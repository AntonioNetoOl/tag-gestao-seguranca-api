using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TagSeguranca.Api.Infrastructure.Persistence;

#nullable disable

namespace TagSeguranca.Api.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(TagDbContext))]
    [Migration("20260619220000_AddFuncoesFuncionario")]
    public partial class AddFuncoesFuncionario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "funcoes_funcionario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    usuario_criacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_alteracao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_alteracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_funcoes_funcionario", x => x.id);
                });

            migrationBuilder.CreateIndex(name: "ix_funcoes_funcionario_nome", table: "funcoes_funcionario", column: "nome", unique: true);

            migrationBuilder.Sql("""
                INSERT INTO funcoes_funcionario (id, nome, ativo, data_criacao)
                VALUES
                    ('11111111-1111-1111-1111-111111111001', 'Segurança', true, NOW()),
                    ('11111111-1111-1111-1111-111111111002', 'Líder', true, NOW()),
                    ('11111111-1111-1111-1111-111111111003', 'Coordenador', true, NOW())
                ON CONFLICT (nome) DO NOTHING;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "funcao_funcionario_id",
                table: "funcionarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_funcionarios_funcao_funcionario_id",
                table: "funcionarios",
                column: "funcao_funcionario_id");

            migrationBuilder.Sql("""
                UPDATE funcionarios f
                SET funcao_funcionario_id = ff.id
                FROM funcoes_funcionario ff
                WHERE lower(f.funcao) = lower(ff.nome)
                  AND f.funcao_funcionario_id IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_funcionarios_funcoes_funcionario_funcao_funcionario_id",
                table: "funcionarios",
                column: "funcao_funcionario_id",
                principalTable: "funcoes_funcionario",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_funcionarios_funcoes_funcionario_funcao_funcionario_id", table: "funcionarios");
            migrationBuilder.DropIndex(name: "ix_funcionarios_funcao_funcionario_id", table: "funcionarios");
            migrationBuilder.DropColumn(name: "funcao_funcionario_id", table: "funcionarios");
            migrationBuilder.DropTable(name: "funcoes_funcionario");
        }
    }
}
