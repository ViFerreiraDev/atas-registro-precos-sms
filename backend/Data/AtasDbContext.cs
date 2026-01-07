using Microsoft.EntityFrameworkCore;
using AtasApi.Models;

namespace AtasApi.Data;

public class AtasDbContext : DbContext
{
    public AtasDbContext(DbContextOptions<AtasDbContext> options) : base(options) { }

    public DbSet<Item> Itens { get; set; }
    public DbSet<ItemDescricao> ItemDescricoes { get; set; }
    public DbSet<AtaRegistroPreco> Atas { get; set; }
    public DbSet<AtaItem> AtaItens { get; set; }
    public DbSet<SincronizacaoLog> SincronizacaoLogs { get; set; }
    public DbSet<ConfiguracaoSistema> Configuracoes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Item
        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("item");
            entity.HasKey(e => e.CodigoItem);
            entity.Property(e => e.CodigoItem).HasColumnName("codigo_item");
            entity.Property(e => e.TipoItem).HasColumnName("tipo_item").HasMaxLength(50);
            entity.Property(e => e.DescricaoPrincipal).HasColumnName("descricao_principal");
            entity.Property(e => e.CodigoPdm).HasColumnName("codigo_pdm");
            entity.Property(e => e.NomePdm).HasColumnName("nome_pdm").HasMaxLength(255);
            entity.Property(e => e.DataCriacao).HasColumnName("data_criacao");
        });

        // ItemDescricao
        modelBuilder.Entity<ItemDescricao>(entity =>
        {
            entity.ToTable("item_descricao");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CodigoItem).HasColumnName("codigo_item");
            entity.Property(e => e.DescricaoItem).HasColumnName("descricao_item");
            entity.Property(e => e.DataRegistro).HasColumnName("data_registro");

            entity.HasIndex(e => new { e.CodigoItem, e.DescricaoItem }).IsUnique();

            entity.HasOne(e => e.Item)
                  .WithMany(i => i.Descricoes)
                  .HasForeignKey(e => e.CodigoItem);
        });

        // AtaRegistroPreco
        modelBuilder.Entity<AtaRegistroPreco>(entity =>
        {
            entity.ToTable("ata_registro_preco");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NumeroAta).HasColumnName("numero_ata").HasMaxLength(20);
            entity.Property(e => e.CodigoUnidadeGerenciadora).HasColumnName("codigo_unidade_gerenciadora").HasMaxLength(10);
            entity.Property(e => e.NumeroCompra).HasColumnName("numero_compra").HasMaxLength(10);
            entity.Property(e => e.AnoCompra).HasColumnName("ano_compra").HasMaxLength(4);
            entity.Property(e => e.CodigoModalidadeCompra).HasColumnName("codigo_modalidade_compra").HasMaxLength(5);
            entity.Property(e => e.NomeModalidadeCompra).HasColumnName("nome_modalidade_compra").HasMaxLength(50);
            entity.Property(e => e.DataAssinatura).HasColumnName("data_assinatura");
            entity.Property(e => e.DataVigenciaInicial).HasColumnName("data_vigencia_inicial");
            entity.Property(e => e.DataVigenciaFinal).HasColumnName("data_vigencia_final");
            entity.Property(e => e.NomeUnidadeGerenciadora).HasColumnName("nome_unidade_gerenciadora").HasMaxLength(255);
            entity.Property(e => e.IdCompra).HasColumnName("id_compra").HasMaxLength(50);
            entity.Property(e => e.NumeroControlePncpCompra).HasColumnName("numero_controle_pncp_compra").HasMaxLength(100);
            entity.Property(e => e.NumeroControlePncpAta).HasColumnName("numero_controle_pncp_ata").HasMaxLength(100);
            entity.Property(e => e.DataHoraInclusao).HasColumnName("data_hora_inclusao");
            entity.Property(e => e.DataHoraAtualizacao).HasColumnName("data_hora_atualizacao");

            entity.HasIndex(e => e.NumeroControlePncpAta).IsUnique();
            entity.HasIndex(e => new { e.NumeroAta, e.CodigoUnidadeGerenciadora }).IsUnique();
            entity.HasIndex(e => e.DataVigenciaFinal);

            entity.Ignore(e => e.DiasParaVencer);
            entity.Ignore(e => e.StatusVigencia);
            entity.Ignore(e => e.LinkPncp);
        });

        // AtaItem
        modelBuilder.Entity<AtaItem>(entity =>
        {
            entity.ToTable("ata_item");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AtaId).HasColumnName("ata_id");
            entity.Property(e => e.CodigoItem).HasColumnName("codigo_item");
            entity.Property(e => e.NumeroItem).HasColumnName("numero_item").HasMaxLength(10);
            entity.Property(e => e.DescricaoItemOriginal).HasColumnName("descricao_item_original");
            entity.Property(e => e.QuantidadeHomologadaItem).HasColumnName("quantidade_homologada_item").HasColumnType("decimal(18,4)");
            entity.Property(e => e.ClassificacaoFornecedor).HasColumnName("classificacao_fornecedor").HasMaxLength(10);
            entity.Property(e => e.NiFornecedor).HasColumnName("ni_fornecedor").HasMaxLength(20);
            entity.Property(e => e.NomeRazaoSocialFornecedor).HasColumnName("nome_razao_social_fornecedor").HasMaxLength(255);
            entity.Property(e => e.QuantidadeHomologadaVencedor).HasColumnName("quantidade_homologada_vencedor").HasColumnType("decimal(18,4)");
            entity.Property(e => e.ValorUnitario).HasColumnName("valor_unitario").HasColumnType("decimal(18,4)");
            entity.Property(e => e.ValorTotal).HasColumnName("valor_total").HasColumnType("decimal(18,4)");
            entity.Property(e => e.MaximoAdesao).HasColumnName("maximo_adesao").HasColumnType("decimal(18,4)");
            entity.Property(e => e.QuantidadeEmpenhada).HasColumnName("quantidade_empenhada").HasColumnType("decimal(18,4)");
            entity.Property(e => e.PercentualMaiorDesconto).HasColumnName("percentual_maior_desconto").HasColumnType("decimal(10,4)");
            entity.Property(e => e.SituacaoSicaf).HasColumnName("situacao_sicaf").HasMaxLength(5);
            entity.Property(e => e.ItemExcluido).HasColumnName("item_excluido");
            entity.Property(e => e.DataHoraExclusao).HasColumnName("data_hora_exclusao");

            entity.HasIndex(e => new { e.AtaId, e.CodigoItem, e.NumeroItem }).IsUnique();
            entity.HasIndex(e => e.CodigoItem);

            entity.HasOne(e => e.Ata)
                  .WithMany(a => a.Itens)
                  .HasForeignKey(e => e.AtaId);

            entity.HasOne(e => e.Item)
                  .WithMany(i => i.AtaItens)
                  .HasForeignKey(e => e.CodigoItem);
        });

        // SincronizacaoLog
        modelBuilder.Entity<SincronizacaoLog>(entity =>
        {
            entity.ToTable("sincronizacao_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Pagina).HasColumnName("pagina");
            entity.Property(e => e.TotalPaginas).HasColumnName("total_paginas");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.Tentativas).HasColumnName("tentativas");
            entity.Property(e => e.ItensProcessados).HasColumnName("itens_processados");
            entity.Property(e => e.ErroMensagem).HasColumnName("erro_mensagem");
            entity.Property(e => e.DataCriacao).HasColumnName("data_criacao");
            entity.Property(e => e.DataUltimaTentativa).HasColumnName("data_ultima_tentativa");

            entity.HasIndex(e => e.Pagina).IsUnique();
        });

        // ConfiguracaoSistema
        modelBuilder.Entity<ConfiguracaoSistema>(entity =>
        {
            entity.ToTable("configuracao_sistema");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Chave).HasColumnName("chave").HasMaxLength(100);
            entity.Property(e => e.Valor).HasColumnName("valor").HasMaxLength(500);
            entity.Property(e => e.Descricao).HasColumnName("descricao").HasMaxLength(255);
            entity.Property(e => e.DataAtualizacao).HasColumnName("data_atualizacao");

            entity.HasIndex(e => e.Chave).IsUnique();
        });
    }
}
