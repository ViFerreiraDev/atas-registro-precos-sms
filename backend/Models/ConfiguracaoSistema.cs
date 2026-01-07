using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtasApi.Models;

[Table("configuracao_sistema")]
public class ConfiguracaoSistema
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("chave")]
    public string Chave { get; set; } = "";

    [Column("valor")]
    public string Valor { get; set; } = "";

    [Column("descricao")]
    public string? Descricao { get; set; }

    [Column("data_atualizacao")]
    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
}
