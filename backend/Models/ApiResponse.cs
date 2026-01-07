using System.Text.Json.Serialization;

namespace AtasApi.Models;

public class ApiResponse
{
    [JsonPropertyName("resultado")]
    public List<ApiArpItem> Resultado { get; set; } = new();

    [JsonPropertyName("paginaAtual")]
    public int PaginaAtual { get; set; }

    [JsonPropertyName("totalPaginas")]
    public int TotalPaginas { get; set; }

    [JsonPropertyName("itensPorPagina")]
    public int ItensPorPagina { get; set; }

    [JsonPropertyName("totalItens")]
    public int TotalItens { get; set; }
}

public class ApiArpItem
{
    [JsonPropertyName("codigoUnidadeGerenciadora")]
    public string? CodigoUnidadeGerenciadora { get; set; }

    [JsonPropertyName("numeroCompra")]
    public string? NumeroCompra { get; set; }

    [JsonPropertyName("anoCompra")]
    public string? AnoCompra { get; set; }

    [JsonPropertyName("codigoModalidadeCompra")]
    public string? CodigoModalidadeCompra { get; set; }

    [JsonPropertyName("nomeModalidadeCompra")]
    public string? NomeModalidadeCompra { get; set; }

    [JsonPropertyName("numeroAtaRegistroPreco")]
    public string? NumeroAta { get; set; }

    [JsonPropertyName("dataAssinatura")]
    public string? DataAssinatura { get; set; }

    [JsonPropertyName("dataVigenciaInicial")]
    public string? DataVigenciaInicial { get; set; }

    [JsonPropertyName("dataVigenciaFinal")]
    public string? DataVigenciaFinal { get; set; }

    [JsonPropertyName("nomeUnidadeGerenciadora")]
    public string? NomeUnidadeGerenciadora { get; set; }

    [JsonPropertyName("idCompra")]
    public string? IdCompra { get; set; }

    [JsonPropertyName("numeroControlePncpCompra")]
    public string? NumeroControlePncpCompra { get; set; }

    [JsonPropertyName("numeroControlePncpAta")]
    public string? NumeroControlePncpAta { get; set; }

    [JsonPropertyName("codigoItem")]
    public int CodigoItem { get; set; }

    [JsonPropertyName("tipoItem")]
    public string? TipoItem { get; set; }

    [JsonPropertyName("descricaoItem")]
    public string? DescricaoItem { get; set; }

    [JsonPropertyName("numeroItem")]
    public string? NumeroItem { get; set; }

    [JsonPropertyName("quantidadeHomologadaItem")]
    public decimal? QuantidadeHomologadaItem { get; set; }

    [JsonPropertyName("classificacaoFornecedor")]
    public string? ClassificacaoFornecedor { get; set; }

    [JsonPropertyName("niFornecedor")]
    public string? NiFornecedor { get; set; }

    [JsonPropertyName("nomeRazaoSocialFornecedor")]
    public string? NomeRazaoSocialFornecedor { get; set; }

    [JsonPropertyName("quantidadeHomologadaVencedor")]
    public decimal? QuantidadeHomologadaVencedor { get; set; }

    [JsonPropertyName("valorUnitario")]
    public decimal? ValorUnitario { get; set; }

    [JsonPropertyName("valorTotal")]
    public decimal? ValorTotal { get; set; }

    [JsonPropertyName("maximoAdesao")]
    public decimal? MaximoAdesao { get; set; }

    [JsonPropertyName("quantidadeEmpenhada")]
    public decimal? QuantidadeEmpenhada { get; set; }

    [JsonPropertyName("percentualMaiorDesconto")]
    public decimal? PercentualMaiorDesconto { get; set; }

    [JsonPropertyName("situacaoSicaf")]
    public string? SituacaoSicaf { get; set; }

    [JsonPropertyName("itemExcluido")]
    public bool? ItemExcluido { get; set; }

    [JsonPropertyName("dataHoraExclusao")]
    public string? DataHoraExclusao { get; set; }

    [JsonPropertyName("dataHoraInclusao")]
    public string? DataHoraInclusao { get; set; }

    [JsonPropertyName("dataHoraAtualizacao")]
    public string? DataHoraAtualizacao { get; set; }

    [JsonPropertyName("codigoPdm")]
    public int? CodigoPdm { get; set; }

    [JsonPropertyName("nomePdm")]
    public string? NomePdm { get; set; }
}
