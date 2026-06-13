using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagSeguranca.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "casas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    endereco = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    cep = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    usuario_criacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_alteracao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_alteracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_casas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "funcionarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_completo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rg = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    chave_pix = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    funcao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    usuario_criacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_alteracao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_alteracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_funcionarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_evento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    usuario_criacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_alteracao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_alteracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tipos_evento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    perfil = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pagamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    funcionario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_pagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_horas_extras = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    quantidade_eventos = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Confirmado"),
                    usuario_pagamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_pagamentos_funcionarios_funcionario_id",
                        column: x => x.funcionario_id,
                        principalTable: "funcionarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eventos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    casa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    data_evento = table.Column<DateTime>(type: "date", nullable: false),
                    hora_inicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    hora_fim = table.Column<TimeSpan>(type: "time", nullable: false),
                    valor_diaria = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_hora_extra = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Rascunho"),
                    usuario_criacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_alteracao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_alteracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos", x => x.id);
                    table.ForeignKey(
                        name: "fk_eventos_casas_casa_id",
                        column: x => x.casa_id,
                        principalTable: "casas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_eventos_tipos_evento_tipo_evento_id",
                        column: x => x.tipo_evento_id,
                        principalTable: "tipos_evento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evento_funcionarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    funcionario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pago = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    removido = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    motivo_remocao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    usuario_criacao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    usuario_alteracao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_alteracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evento_funcionarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_evento_funcionarios_eventos_evento_id",
                        column: x => x.evento_id,
                        principalTable: "eventos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_evento_funcionarios_funcionarios_funcionario_id",
                        column: x => x.funcionario_id,
                        principalTable: "funcionarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pagamento_itens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pagamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento_funcionario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor_diaria_pago = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_hora_extra_pago = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    quantidade_horas_extras = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_total_item = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagamento_itens", x => x.id);
                    table.ForeignKey(
                        name: "fk_pagamento_itens_evento_funcionarios_evento_funcionario_id",
                        column: x => x.evento_funcionario_id,
                        principalTable: "evento_funcionarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pagamento_itens_pagamentos_pagamento_id",
                        column: x => x.pagamento_id,
                        principalTable: "pagamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_evento_funcionarios_evento_id_funcionario_id",
                table: "evento_funcionarios",
                columns: new[] { "evento_id", "funcionario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_evento_funcionarios_funcionario_id",
                table: "evento_funcionarios",
                column: "funcionario_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_casa_id",
                table: "eventos",
                column: "casa_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_tipo_evento_id",
                table: "eventos",
                column: "tipo_evento_id");

            migrationBuilder.CreateIndex(
                name: "ix_funcionarios_cpf",
                table: "funcionarios",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_funcionarios_rg",
                table: "funcionarios",
                column: "rg",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pagamento_itens_evento_funcionario_id",
                table: "pagamento_itens",
                column: "evento_funcionario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pagamento_itens_pagamento_id",
                table: "pagamento_itens",
                column: "pagamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagamentos_funcionario_id",
                table: "pagamentos",
                column: "funcionario_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pagamento_itens");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "evento_funcionarios");

            migrationBuilder.DropTable(
                name: "pagamentos");

            migrationBuilder.DropTable(
                name: "eventos");

            migrationBuilder.DropTable(
                name: "funcionarios");

            migrationBuilder.DropTable(
                name: "casas");

            migrationBuilder.DropTable(
                name: "tipos_evento");
        }
    }
}
