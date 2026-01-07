using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AtasApi.Data;
using AtasApi.Services;

namespace AtasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SincronizacaoController : ControllerBase
{
    private readonly SincronizacaoService _sincronizacaoService;
    private readonly AtasDbContext _db;

    public SincronizacaoController(SincronizacaoService sincronizacaoService, AtasDbContext db)
    {
        _sincronizacaoService = sincronizacaoService;
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<SincronizacaoResult>> Sincronizar(CancellationToken cancellationToken)
    {
        var resultado = await _sincronizacaoService.SincronizarAsync(cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Sincronização paralela - dispara 1 requisição por segundo em threads separadas
    /// </summary>
    [HttpPost("paralelo")]
    public async Task<ActionResult<SincronizacaoResult>> SincronizarParalelo(
        [FromBody] SincronizacaoConfig? config,
        CancellationToken cancellationToken)
    {
        var cfg = config ?? new SincronizacaoConfig
        {
            ModoParalelo = true,
            IntervaloEntreRequisicoesMs = 1000,
            MaxConcorrencia = 10
        };
        var resultado = await _sincronizacaoService.SincronizarParaleloAsync(cfg, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Continuar sincronização - reprocessa páginas que falharam
    /// </summary>
    [HttpPost("continuar")]
    public async Task<ActionResult<SincronizacaoResult>> Continuar(CancellationToken cancellationToken)
    {
        var resultado = await _sincronizacaoService.ContinuarSincronizacaoAsync(cancellationToken);
        return Ok(resultado);
    }

    /// <summary>
    /// Sincronização incremental - busca apenas novas atas a partir da última data sincronizada
    /// </summary>
    [HttpPost("atualizar")]
    public async Task<ActionResult<SincronizacaoResult>> Atualizar(CancellationToken cancellationToken)
    {
        var resultado = await _sincronizacaoService.SincronizarIncrementalAsync(cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("status")]
    public async Task<ActionResult<SincronizacaoStatus>> ObterStatus(CancellationToken cancellationToken)
    {
        var status = await _sincronizacaoService.ObterStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Para a sincronização em andamento
    /// </summary>
    [HttpPost("parar")]
    public ActionResult Parar()
    {
        var parou = _sincronizacaoService.PararSincronizacao();
        if (parou)
        {
            return Ok(new { message = "Solicitação de parada enviada. A sincronização será interrompida em breve." });
        }
        return BadRequest(new { message = "Nenhuma sincronização em andamento para parar." });
    }

    [HttpDelete("logs")]
    public async Task<ActionResult> LimparLogs(CancellationToken cancellationToken)
    {
        await _sincronizacaoService.LimparLogsAsync(cancellationToken);
        return Ok(new { message = "Logs limpos" });
    }

    [HttpPost("resetar-travadas")]
    public async Task<ActionResult> ResetarTravadas(CancellationToken cancellationToken)
    {
        var count = await _sincronizacaoService.ResetarPaginasTravadasAsync(cancellationToken);
        return Ok(new { message = $"{count} paginas resetadas" });
    }

    [HttpDelete("reset")]
    public async Task<ActionResult> Reset()
    {
        await _db.AtaItens.ExecuteDeleteAsync();
        await _db.ItemDescricoes.ExecuteDeleteAsync();
        await _db.Atas.ExecuteDeleteAsync();
        await _db.Itens.ExecuteDeleteAsync();
        await _db.SincronizacaoLogs.ExecuteDeleteAsync();
        return Ok(new { message = "Dados limpos" });
    }

    /// <summary>
    /// Corrige itens sem descrição principal, buscando da tabela de descrições
    /// </summary>
    [HttpPost("corrigir-descricoes")]
    public async Task<ActionResult> CorrigirDescricoes(CancellationToken cancellationToken)
    {
        // Buscar itens sem descrição principal que têm descrições na tabela auxiliar
        var itensSemDescricao = await _db.Itens
            .Where(i => i.DescricaoPrincipal == null)
            .ToListAsync(cancellationToken);

        int corrigidos = 0;
        foreach (var item in itensSemDescricao)
        {
            var primeiraDescricao = await _db.ItemDescricoes
                .Where(d => d.CodigoItem == item.CodigoItem)
                .OrderBy(d => d.Id)
                .Select(d => d.DescricaoItem)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrEmpty(primeiraDescricao))
            {
                item.DescricaoPrincipal = primeiraDescricao;
                corrigidos++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = $"{corrigidos} itens corrigidos" });
    }
}
