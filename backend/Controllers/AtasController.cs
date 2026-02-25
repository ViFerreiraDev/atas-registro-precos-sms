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

    private static AtaResumoDto MapToResumo(AtaRegistroPreco a, DateTime hoje, bool incluirPreview = false)
    {
        var dias = (a.DataVigenciaFinal - hoje).Days;
        var status = dias <= 0 ? "Vencida" :
                     dias <= 30 ? "Critico" :
                     dias <= 60 ? "Alerta" :
                     dias <= 120 ? "Atencao" : "Vigente";

        List<ItemPreviewDto>? preview = null;
        if (incluirPreview && a.Itens != null)
        {
            preview = a.Itens.Take(3).Select(i => new ItemPreviewDto(
                i.CodigoItem,
                i.DescricaoItemOriginal,
                i.Item?.TipoItem
            )).ToList();
        }

        return new AtaResumoDto(
            a.Id,
            a.NumeroAta,
            a.DataVigenciaFinal,
            dias,
            status,
            null,
            null,
            null,
            null,
            a.Itens?.Count ?? 0,
            a.Itens?.Sum(i => i.ValorTotal ?? 0) ?? 0,
            preview
        );
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

        if (!string.IsNullOrEmpty(status))
        {
            var em30 = hoje.AddDays(30);
            var em60 = hoje.AddDays(60);
            var em120 = hoje.AddDays(120);

            switch (status.ToLower())
            {
                case "critico":
                    query = query.Where(a => a.DataVigenciaFinal <= em30);
                    break;
                case "alerta":
                    query = query.Where(a => a.DataVigenciaFinal > em30 && a.DataVigenciaFinal <= em60);
                    break;
                case "atencao":
                    query = query.Where(a => a.DataVigenciaFinal > em60 && a.DataVigenciaFinal <= em120);
                    break;
                case "vigente":
                    query = query.Where(a => a.DataVigenciaFinal > em120);
                    break;
            }
        }

        var atas = await query
            .OrderBy(a => a.DataVigenciaFinal)
            .Take(limite)
            .ToListAsync();

        var resultado = atas.Select(a => MapToResumo(a, hoje, incluirPreview: true)).ToList();

        return Ok(resultado);
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
            .Include(a => a.Itens)
                .ThenInclude(ai => ai.Item)
            .Where(a => a.DataVigenciaFinal < hoje && a.DataVigenciaFinal >= dataLimite)
            .OrderByDescending(a => a.DataVigenciaFinal)
            .ToListAsync();

        var resultado = atas.Select(a => MapToResumo(a, hoje, incluirPreview: true)).ToList();

        return Ok(resultado);
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
            .ToListAsync();

        var resultado = atas.Select(a => MapToResumo(a, hoje, incluirPreview: true)).ToList();

        return Ok(resultado);
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
            .ToListAsync();

        var resultado = atas.Select(a => MapToResumo(a, hoje, incluirPreview: true)).ToList();

        return Ok(resultado);
    }
}
