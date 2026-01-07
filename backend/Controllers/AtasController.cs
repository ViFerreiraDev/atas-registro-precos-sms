using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AtasApi.Data;
using AtasApi.Models;

namespace AtasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AtasController : ControllerBase
{
    private readonly AtasDbContext _db;

    public AtasController(AtasDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lista todas as atas vigentes
    /// </summary>
    [HttpGet("vigentes")]
    public async Task<ActionResult<List<AtaResumoDto>>> ListarVigentes(
        [FromQuery] string? status = null,
        [FromQuery] int limite = 100)
    {
        var hoje = DateTime.Today;

        var query = _db.Atas
            .Include(a => a.Itens)
                .ThenInclude(ai => ai.Item)
            .Where(a => a.DataVigenciaFinal >= hoje)
            .AsQueryable();

        // Filtrar por faixa de dias ANTES de buscar
        // Críticas (30d) = 0 a 30 dias
        // Alerta (60d) = 31 a 60 dias
        // Atenção (120d) = 61 a 120 dias
        // Vigente = mais de 120 dias
        if (!string.IsNullOrEmpty(status))
        {
            switch (status.ToLower())
            {
                case "critico":
                    // 0 a 30 dias
                    query = query.Where(a => (a.DataVigenciaFinal - hoje).Days <= 30);
                    break;
                case "alerta":
                    // 31 a 60 dias
                    query = query.Where(a => (a.DataVigenciaFinal - hoje).Days > 30 && (a.DataVigenciaFinal - hoje).Days <= 60);
                    break;
                case "atencao":
                    // 61 a 120 dias
                    query = query.Where(a => (a.DataVigenciaFinal - hoje).Days > 60 && (a.DataVigenciaFinal - hoje).Days <= 120);
                    break;
                case "vigente":
                    // mais de 120 dias
                    query = query.Where(a => (a.DataVigenciaFinal - hoje).Days > 120);
                    break;
            }
        }

        var atas = await query
            .OrderBy(a => a.DataVigenciaFinal)
            .Take(limite)
            .Select(a => new AtaResumoDto(
                a.Id,
                a.NumeroAta,
                a.DataVigenciaFinal,
                (a.DataVigenciaFinal - hoje).Days,
                (a.DataVigenciaFinal - hoje).Days <= 0 ? "Vencida" :
                (a.DataVigenciaFinal - hoje).Days <= 30 ? "Critico" :
                (a.DataVigenciaFinal - hoje).Days <= 60 ? "Alerta" :
                (a.DataVigenciaFinal - hoje).Days <= 120 ? "Atencao" : "Vigente",
                null,
                null,
                null,
                null,
                a.Itens.Count,
                a.Itens.Sum(i => i.ValorTotal ?? 0),
                a.Itens.Take(3).Select(i => new ItemPreviewDto(
                    i.CodigoItem,
                    i.DescricaoItemOriginal,
                    i.Item.TipoItem
                )).ToList()
            ))
            .ToListAsync();

        return Ok(atas);
    }

    /// <summary>
    /// Lista atas recém-encerradas (últimos 15 dias)
    /// </summary>
    [HttpGet("recem-encerradas")]
    public async Task<ActionResult<List<AtaResumoDto>>> RecemEncerradas([FromQuery] int dias = 15)
    {
        var hoje = DateTime.Today;
        var dataLimite = hoje.AddDays(-dias);

        var atas = await _db.Atas
            .Where(a => a.DataVigenciaFinal < hoje && a.DataVigenciaFinal >= dataLimite)
            .OrderByDescending(a => a.DataVigenciaFinal)
            .Select(a => new AtaResumoDto(
                a.Id,
                a.NumeroAta,
                a.DataVigenciaFinal,
                (a.DataVigenciaFinal - hoje).Days,
                "Vencida",
                null,
                null,
                null,
                null,
                a.Itens.Count,
                a.Itens.Sum(i => i.ValorTotal ?? 0),
                null
            ))
            .ToListAsync();

        return Ok(atas);
    }

    /// <summary>
    /// Lista atas novas (assinadas nos últimos N dias)
    /// </summary>
    [HttpGet("novas")]
    public async Task<ActionResult<List<AtaResumoDto>>> AtasNovas([FromQuery] int dias = 30)
    {
        var hoje = DateTime.Today;
        var dataLimite = hoje.AddDays(-dias);

        var atas = await _db.Atas
            .Include(a => a.Itens)
                .ThenInclude(ai => ai.Item)
            .Where(a => a.DataVigenciaInicial >= dataLimite)
            .OrderByDescending(a => a.DataVigenciaInicial)
            .Select(a => new AtaResumoDto(
                a.Id,
                a.NumeroAta,
                a.DataVigenciaFinal,
                (a.DataVigenciaFinal - hoje).Days,
                (a.DataVigenciaFinal - hoje).Days <= 0 ? "Vencida" :
                (a.DataVigenciaFinal - hoje).Days <= 30 ? "Critico" :
                (a.DataVigenciaFinal - hoje).Days <= 60 ? "Alerta" :
                (a.DataVigenciaFinal - hoje).Days <= 120 ? "Atencao" : "Vigente",
                null,
                null,
                null,
                null,
                a.Itens.Count,
                a.Itens.Sum(i => i.ValorTotal ?? 0),
                a.Itens.Take(3).Select(i => new ItemPreviewDto(
                    i.CodigoItem,
                    i.DescricaoItemOriginal,
                    i.Item.TipoItem
                )).ToList()
            ))
            .ToListAsync();

        return Ok(atas);
    }

    /// <summary>
    /// Obtém detalhes de uma ata específica
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AtaDetalheDto>> ObterAta(int id)
    {
        var ata = await _db.Atas
            .Include(a => a.Itens)
                .ThenInclude(ai => ai.Item)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (ata == null)
            return NotFound("Ata não encontrada");

        var itens = ata.Itens.Select(ai => new AtaItemDto(
            ai.CodigoItem,
            ai.DescricaoItemOriginal,
            ai.Item.TipoItem,
            ai.NomeRazaoSocialFornecedor,
            ai.ValorUnitario,
            ai.QuantidadeHomologadaItem,
            ai.QuantidadeEmpenhada
        )).ToList();

        return Ok(new AtaDetalheDto(
            ata.Id,
            ata.NumeroAta,
            ata.NomeUnidadeGerenciadora,
            ata.NomeModalidadeCompra,
            ata.DataAssinatura,
            ata.DataVigenciaInicial,
            ata.DataVigenciaFinal,
            ata.DiasParaVencer,
            ata.StatusVigencia,
            ata.LinkPncp,
            itens
        ));
    }

    /// <summary>
    /// Pesquisa atas por número ou fornecedor
    /// </summary>
    [HttpGet("pesquisar")]
    public async Task<ActionResult<List<AtaResumoDto>>> Pesquisar([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Informe o termo de busca");

        var hoje = DateTime.Today;
        var termoBusca = q.ToUpper();

        var atas = await _db.Atas
            .Include(a => a.Itens)
                .ThenInclude(ai => ai.Item)
            .Where(a => a.NumeroAta.Contains(q) ||
                        a.Itens.Any(i => i.NomeRazaoSocialFornecedor != null &&
                                         i.NomeRazaoSocialFornecedor.ToUpper().Contains(termoBusca)))
            .OrderByDescending(a => a.DataVigenciaFinal)
            .Take(50)
            .Select(a => new AtaResumoDto(
                a.Id,
                a.NumeroAta,
                a.DataVigenciaFinal,
                (a.DataVigenciaFinal - hoje).Days,
                (a.DataVigenciaFinal - hoje).Days <= 0 ? "Vencida" :
                (a.DataVigenciaFinal - hoje).Days <= 30 ? "Critico" :
                (a.DataVigenciaFinal - hoje).Days <= 60 ? "Alerta" :
                (a.DataVigenciaFinal - hoje).Days <= 120 ? "Atencao" : "Vigente",
                null,
                null,
                null,
                null,
                a.Itens.Count,
                a.Itens.Sum(i => i.ValorTotal ?? 0),
                a.Itens.Take(3).Select(i => new ItemPreviewDto(
                    i.CodigoItem,
                    i.DescricaoItemOriginal,
                    i.Item.TipoItem
                )).ToList()
            ))
            .ToListAsync();

        return Ok(atas);
    }
}
