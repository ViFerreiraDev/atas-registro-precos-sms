using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AtasApi.Models;

[Table("sincronizacao_log")]
public class SincronizacaoLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("pagina")]
    public int Pagina { get; set; }

    [Column("total_paginas")]
    public int TotalPaginas { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pendente"; // pendente, sucesso, erro

    [Column("tentativas")]
    public int Tentativas { get; set; } = 0;

    [Column("itens_processados")]
    public int ItensProcessados { get; set; } = 0;

    [Column("erro_mensagem")]
    public string? ErroMensagem { get; set; }

    [Column("data_criacao")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    [Column("data_ultima_tentativa")]
    public DateTime? DataUltimaTentativa { get; set; }
}
