namespace AtasApi.Models;

// DTOs para respostas da API
public record ItemPesquisaDto(
    int CodigoItem,
    string TipoItem,
    string? DescricaoPrincipal,
    int TotalAtasVigentes,
    int TotalAtasVencidas,
    List<string> OutrasDescricoes
);

public record ItemDetalheDto(
    int CodigoItem,
    string TipoItem,
    string? DescricaoPrincipal,
    int? CodigoPdm,
    string? NomePdm,
    List<string> TodasDescricoes,
    List<AtaResumoDto> AtasVigentes,
    List<AtaResumoDto> AtasVencidas
);

public record AtaResumoDto(
    int Id,
    string NumeroAta,
    DateTime DataVigenciaFinal,
    int DiasParaVencer,
    string StatusVigencia,
    string? LinkPncp,
    string? Fornecedor,
    decimal? ValorUnitario,
    string? DescricaoItemOriginal,
    int TotalItens,
    decimal ValorTotal,
    List<ItemPreviewDto>? ItensPreview
);

public record ItemPreviewDto(
    int CodigoItem,
    string? Descricao,
    string? TipoItem
);

public record AtaDetalheDto(
    int Id,
    string NumeroAta,
    string? NomeUnidadeGerenciadora,
    string? NomeModalidadeCompra,
    DateTime? DataAssinatura,
    DateTime DataVigenciaInicial,
    DateTime DataVigenciaFinal,
    int DiasParaVencer,
    string StatusVigencia,
    string? LinkPncp,
    List<AtaItemDto> Itens
);

public record AtaItemDto(
    int CodigoItem,
    string? DescricaoItemOriginal,
    string? TipoItem,
    string? Fornecedor,
    decimal? ValorUnitario,
    decimal? QuantidadeHomologadaItem,
    decimal? QuantidadeEmpenhada
);

public record DashboardDto(
    int TotalAtasVigentes,
    int AtasCriticas,
    int AtasAlerta,
    int AtasAtencao,
    int TotalItensComAta,
    int TotalItensSemAta,
    List<AtaResumoDto> ProximasVencer
);

public record ItemSemAtaDto(
    int CodigoItem,
    string TipoItem,
    string? DescricaoPrincipal,
    DateTime? UltimaAtaVencida
);

public record ItemAlertaDto(
    int CodigoItem,
    string TipoItem,
    string? DescricaoPrincipal,
    string NumeroAtaVigente,
    DateTime DataVigenciaFinal,
    int DiasParaVencer,
    string StatusVigencia,
    string? LinkPncp,
    string? Fornecedor,
    decimal? ValorUnitario
);
