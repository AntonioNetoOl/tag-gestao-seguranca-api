using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagSeguranca.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoFuncionarioHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evento_funcionarios_historico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento_funcionario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    funcionario_anterior_id = table.Column<Guid>(type: "uuid", nullable: true),
                    funcionario_novo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    acao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    usuario_acao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_acao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evento_funcionarios_historico", x => x.id);
                    table.ForeignKey(
                        name: "fk_evento_funcionarios_historico_eventos_evento_id",
                        column: x => x.evento_id,
                        principalTable: "eventos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evento_funcionarios_historico_evento_funcionarios_evento_funcionario_id",
                        column: x => x.evento_funcionario_id,
                        principalTable: "evento_funcionarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evento_funcionarios_historico_funcionarios_funcionario_anterior_id",
                        column: x => x.funcionario_anterior_id,
                        principalTable: "funcionarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evento_funcionarios_historico_funcionarios_funcionario_novo_id",
                        column: x => x.funcionario_novo_id,
                        principalTable: "funcionarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evento_funcionarios_historico_usuarios_usuario_acao_id",
                        column: x => x.usuario_acao_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_evento_funcionarios_historico_data_acao",
                table: "evento_funcionarios_historico",
                column: "data_acao");

            migrationBuilder.CreateIndex(
                name: "ix_evento_funcionarios_historico_evento_funcionario_id",
                table: "evento_funcionarios_historico",
                column: "evento_funcionario_id");

            migrationBuilder.CreateIndex(
                name: "ix_evento_funcionarios_historico_evento_id",
                table: "evento_funcionarios_historico",
                column: "evento_id");

            migrationBuilder.CreateIndex(
                name: "ix_evento_funcionarios_historico_funcionario_anterior_id",
                table: "evento_funcionarios_historico",
                column: "funcionario_anterior_id");

            migrationBuilder.CreateIndex(
                name: "ix_evento_funcionarios_historico_funcionario_novo_id",
                table: "evento_funcionarios_historico",
                column: "funcionario_novo_id");

            migrationBuilder.CreateIndex(
                name: "ix_evento_funcionarios_historico_usuario_acao_id",
                table: "evento_funcionarios_historico",
                column: "usuario_acao_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evento_funcionarios_historico");
        }
    }
}
