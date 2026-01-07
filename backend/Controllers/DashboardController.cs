using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AtasApi.Data;
using AtasApi.Models;

namespace AtasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AtasDbContext _db;

    public DashboardController(AtasDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retorna dados resumidos para o dashboard
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> ObterDashboard()
    {
        var hoje = DateTime.Today;
        var em30Dias = hoje.AddDays(30);
        var em60Dias = hoje.AddDays(60);
        var em120Dias = hoje.AddDays(120);

        // Contagem de atas por status
        var atasVigentes = await _db.Atas
            .Include(a => a.Itens)
            .Where(a => a.DataVigenciaFinal >= hoje)
            .ToListAsync();

        var totalAtasVigentes = atasVigentes.Count;
        var atasCriticas = atasVigentes.Count(a => a.DataVigenciaFinal <= em30Dias);
        var atasAlerta = atasVigentes.Count(a => a.DataVigenciaFinal > em30Dias && a.DataVigenciaFinal <= em60Dias);
        var atasAtencao = atasVigentes.Count(a => a.DataVigenciaFinal > em60Dias && a.DataVigenciaFinal <= em120Dias);

        // Itens com e sem ata
        var totalItens = await _db.Itens.CountAsync();
        var itensComAtaVigente = await _db.AtaItens
            .Where(ai => ai.Ata.DataVigenciaFinal >= hoje)
            .Select(ai => ai.CodigoItem)
            .Distinct()
            .CountAsync();

        // Próximas a vencer (30 dias)
        var proximasVencer = atasVigentes
            .Where(a => a.DataVigenciaFinal <= em30Dias)
            .OrderBy(a => a.DataVigenciaFinal)
            .Take(10)
            .Select(a => new AtaResumoDto(
                a.Id,
                a.NumeroAta,
                a.DataVigenciaFinal,
                a.DiasParaVencer,
                a.StatusVigencia,
                null,
                null,
                null,
                null,
                a.Itens.Count,
                a.Itens.Sum(i => i.ValorTotal ?? 0),
                null
            ))
            .ToList();

        return Ok(new DashboardDto(
            totalAtasVigentes,
            atasCriticas,
            atasAlerta,
            atasAtencao,
            itensComAtaVigente,
            totalItens - itensComAtaVigente,
            proximasVencer
        ));
    }

    /// <summary>
    /// Retorna histórico de atas por mês (para gráfico)
    /// </summary>
    [HttpGet("historico")]
    public async Task<ActionResult<object>> ObterHistorico([FromQuery] int meses = 12)
    {
        var hoje = DateTime.Today;
        var dataInicio = hoje.AddMonths(-meses);

        var historico = new List<object>();

        for (int i = 0; i <= meses; i++)
        {
            var data = dataInicio.AddMonths(i);
            var primeiroDia = new DateTime(data.Year, data.Month, 1);
            var ultimoDia = primeiroDia.AddMonths(1).AddDays(-1);

            var vigentes = await _db.Atas
                .CountAsync(a => a.DataVigenciaInicial <= ultimoDia && a.DataVigenciaFinal >= primeiroDia);

            historico.Add(new
            {
                mes = primeiroDia.ToString("MMM/yy"),
                vigentes
            });
        }

        return Ok(historico);
    }
}
