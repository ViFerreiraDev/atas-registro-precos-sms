namespace AtasApi.Models;

public class AtaItem
{
    public int Id { get; set; }
    public int AtaId { get; set; }
    public int CodigoItem { get; set; }
    public string? NumeroItem { get; set; }
    public string? DescricaoItemOriginal { get; set; }
    public decimal? QuantidadeHomologadaItem { get; set; }
    public string? ClassificacaoFornecedor { get; set; }
    public string? NiFornecedor { get; set; }
    public string? NomeRazaoSocialFornecedor { get; set; }
    public decimal? QuantidadeHomologadaVencedor { get; set; }
    public decimal? ValorUnitario { get; set; }
    public decimal? ValorTotal { get; set; }
    public decimal? MaximoAdesao { get; set; }
    public decimal? QuantidadeEmpenhada { get; set; }
    public decimal? PercentualMaiorDesconto { get; set; }
    public string? SituacaoSicaf { get; set; }
    public bool ItemExcluido { get; set; }
    public DateTime? DataHoraExclusao { get; set; }

    public AtaRegistroPreco Ata { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
