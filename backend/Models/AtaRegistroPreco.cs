namespace AtasApi.Models;

public class AtaRegistroPreco
{
    public int Id { get; set; }
    public string NumeroAta { get; set; } = string.Empty;
    public string CodigoUnidadeGerenciadora { get; set; } = string.Empty;
    public string? NumeroCompra { get; set; }
    public string? AnoCompra { get; set; }
    public string? CodigoModalidadeCompra { get; set; }
    public string? NomeModalidadeCompra { get; set; }
    public DateTime? DataAssinatura { get; set; }
    public DateTime DataVigenciaInicial { get; set; }
    public DateTime DataVigenciaFinal { get; set; }
    public string? NomeUnidadeGerenciadora { get; set; }
    public string? IdCompra { get; set; }
    public string? NumeroControlePncpCompra { get; set; }
    public string? NumeroControlePncpAta { get; set; }
    public DateTime? DataHoraInclusao { get; set; }
    public DateTime? DataHoraAtualizacao { get; set; }

    public ICollection<AtaItem> Itens { get; set; } = new List<AtaItem>();

    // Propriedades calculadas para alertas
    public int DiasParaVencer => (DataVigenciaFinal - DateTime.Today).Days;

    public string StatusVigencia => DiasParaVencer switch
    {
        <= 0 => "Vencida",
        <= 30 => "Critico",
        <= 60 => "Alerta",
        <= 120 => "Atencao",
        _ => "Vigente"
    };

    // Link para baixar o arquivo da ata no PNCP
    // Formato: 42498600000171-1-000586/2023-000007
    // URL: https://pncp.gov.br/pncp-api/v1/orgaos/{cnpj}/compras/{ano}/{numCompra}/atas/{numAta}/arquivos/1
    public string? LinkPncp
    {
        get
        {
            if (string.IsNullOrEmpty(NumeroControlePncpAta)) return null;

            try
            {
                // Formato: CNPJ-?-NUMCOMPRA/ANO-NUMATA
                var partes = NumeroControlePncpAta.Split('-');
                if (partes.Length < 4) return null;

                var cnpj = partes[0];
                var compraAno = partes[2].Split('/');
                if (compraAno.Length < 2) return null;

                var numCompra = int.Parse(compraAno[0]).ToString();
                var ano = compraAno[1];
                var numAta = int.Parse(partes[3]).ToString();

                return $"https://pncp.gov.br/pncp-api/v1/orgaos/{cnpj}/compras/{ano}/{numCompra}/atas/{numAta}/arquivos/1";
            }
            catch
            {
                return null;
            }
        }
    }
}
