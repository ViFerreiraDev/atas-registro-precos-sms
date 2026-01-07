using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtasApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ata_registro_preco",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero_ata = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    codigo_unidade_gerenciadora = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    numero_compra = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ano_compra = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    codigo_modalidade_compra = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    nome_modalidade_compra = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    data_assinatura = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_vigencia_inicial = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_vigencia_final = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    nome_unidade_gerenciadora = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    id_compra = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    numero_controle_pncp_compra = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    numero_controle_pncp_ata = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    data_hora_inclusao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_hora_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ata_registro_preco", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "item",
                columns: table => new
                {
                    codigo_item = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_item = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descricao_principal = table.Column<string>(type: "text", nullable: true),
                    codigo_pdm = table.Column<int>(type: "integer", nullable: true),
                    nome_pdm = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item", x => x.codigo_item);
                });

            migrationBuilder.CreateTable(
                name: "ata_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ata_id = table.Column<int>(type: "integer", nullable: false),
                    codigo_item = table.Column<int>(type: "integer", nullable: false),
                    numero_item = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    descricao_item_original = table.Column<string>(type: "text", nullable: true),
                    quantidade_homologada_item = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    classificacao_fornecedor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ni_fornecedor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    nome_razao_social_fornecedor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    quantidade_homologada_vencedor = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    valor_unitario = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    valor_total = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    maximo_adesao = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    quantidade_empenhada = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    percentual_maior_desconto = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    situacao_sicaf = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    item_excluido = table.Column<bool>(type: "boolean", nullable: false),
                    data_hora_exclusao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ata_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_ata_item_ata_registro_preco_ata_id",
                        column: x => x.ata_id,
                        principalTable: "ata_registro_preco",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ata_item_item_codigo_item",
                        column: x => x.codigo_item,
                        principalTable: "item",
                        principalColumn: "codigo_item",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_descricao",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_item = table.Column<int>(type: "integer", nullable: false),
                    descricao_item = table.Column<string>(type: "text", nullable: false),
                    data_registro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_descricao", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_descricao_item_codigo_item",
                        column: x => x.codigo_item,
                        principalTable: "item",
                        principalColumn: "codigo_item",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ata_item_ata_id_codigo_item_numero_item",
                table: "ata_item",
                columns: new[] { "ata_id", "codigo_item", "numero_item" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ata_item_codigo_item",
                table: "ata_item",
                column: "codigo_item");

            migrationBuilder.CreateIndex(
                name: "IX_ata_registro_preco_data_vigencia_final",
                table: "ata_registro_preco",
                column: "data_vigencia_final");

            migrationBuilder.CreateIndex(
                name: "IX_ata_registro_preco_numero_ata_codigo_unidade_gerenciadora",
                table: "ata_registro_preco",
                columns: new[] { "numero_ata", "codigo_unidade_gerenciadora" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ata_registro_preco_numero_controle_pncp_ata",
                table: "ata_registro_preco",
                column: "numero_controle_pncp_ata",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_descricao_codigo_item_descricao_item",
                table: "item_descricao",
                columns: new[] { "codigo_item", "descricao_item" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ata_item");

            migrationBuilder.DropTable(
                name: "item_descricao");

            migrationBuilder.DropTable(
                name: "ata_registro_preco");

            migrationBuilder.DropTable(
                name: "item");
        }
    }
}
