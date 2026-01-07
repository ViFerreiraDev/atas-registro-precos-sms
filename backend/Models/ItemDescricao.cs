namespace AtasApi.Models;

public class ItemDescricao
{
    public int Id { get; set; }
    public int CodigoItem { get; set; }
    public string DescricaoItem { get; set; } = string.Empty;
    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

    public Item Item { get; set; } = null!;
}
