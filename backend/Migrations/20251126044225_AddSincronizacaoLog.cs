using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtasApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSincronizacaoLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sincronizacao_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pagina = table.Column<int>(type: "integer", nullable: false),
                    total_paginas = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    itens_processados = table.Column<int>(type: "integer", nullable: false),
                    erro_mensagem = table.Column<string>(type: "text", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    data_ultima_tentativa = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sincronizacao_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sincronizacao_log_pagina",
                table: "sincronizacao_log",
                column: "pagina",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sincronizacao_log");
        }
    }
}
