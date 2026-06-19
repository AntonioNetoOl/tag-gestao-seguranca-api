using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagSeguranca.Api.Infrastructure.Persistence.Migrations
{
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

            migrationBuilder.CreateIndex(
                name: "ix_funcoes_funcionario_nome",
                table: "funcoes_funcionario",
                column: "nome",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "funcoes_funcionario");
        }
    }
}
