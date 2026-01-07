namespace AtasApi.Models;

public class Item
{
    public int CodigoItem { get; set; }
    public string TipoItem { get; set; } = string.Empty;
    public string? DescricaoPrincipal { get; set; }
    public int? CodigoPdm { get; set; }
    public string? NomePdm { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public ICollection<ItemDescricao> Descricoes { get; set; } = new List<ItemDescricao>();
    public ICollection<AtaItem> AtaItens { get; set; } = new List<AtaItem>();
}
