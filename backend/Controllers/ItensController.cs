using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AtasApi.Data;
using AtasApi.Models;

namespace AtasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItensController : ControllerBase
{
    private readonly AtasDbContext _db;

    public ItensController(AtasDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Verifica se o texto contém TODOS os termos da busca (em qualquer ordem/posição)
    /// </summary>
    private static bool ContemTodosTermos(string? texto, string busca)
    {
        if (string.IsNullOrEmpty(texto)) return false;
        var textoLower = texto.ToLower();
        var termos = busca.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return termos.All(termo => textoLower.Contains(termo));
    }

    /// <summary>
    /// Pesquisa itens por descrição (ex: "dipirona 500mg" encontra "DIPIRONA SÓDICA 500MG COMPRIMIDO")
    /// Busca por palavras em qualquer ordem/posição
    /// </summary>
    [HttpGet("pesquisar")]
    public async Task<ActionResult<List<ItemPesquisaDto>>> Pesquisar(
        [FromQuery] string q,
        [FromQuery] bool apenasVigentes = false)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
            return BadRequest("A pesquisa deve ter pelo menos 3 caracteres");

        var hoje = DateTime.Today;
        var termos = q.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Busca itens que tenham alguma descrição contendo TODOS os termos
        var todasDescricoes = await _db.ItemDescricoes.ToListAsync();
        var itensComDescricao = todasDescricoes
            .Where(d => termos.All(termo => d.DescricaoItem.ToLower().Contains(termo)))
            .Select(d => d.CodigoItem)
            .Distinct()
            .ToList();

        // Busca os itens
        var itens = await _db.Itens
            .Where(i => itensComDescricao.Contains(i.CodigoItem))
            .Include(i => i.Descricoes)
            .Include(i => i.AtaItens)
                .ThenInclude(ai => ai.Ata)
            .ToListAsync();

        var resultado = itens.Select(i =>
        {
            var atasVigentes = i.AtaItens
                .Where(ai => ai.Ata.DataVigenciaFinal >= hoje)
                .Count();

            var atasVencidas = i.AtaItens
                .Where(ai => ai.Ata.DataVigenciaFinal < hoje)
                .Count();

            var outrasDescricoes = i.Descricoes
                .Where(d => d.DescricaoItem != i.DescricaoPrincipal)
                .Select(d => d.DescricaoItem)
                .Take(3)
                .ToList();

            return new ItemPesquisaDto(
                i.CodigoItem,
                i.TipoItem,
                i.DescricaoPrincipal,
                atasVigentes,
                atasVencidas,
                outrasDescricoes
            );
        })
        .Where(i => !apenasVigentes || i.TotalAtasVigentes > 0)
        .OrderByDescending(i => i.TotalAtasVigentes)
        .ThenByDescending(i => i.TotalAtasVencidas)
        .ThenBy(i => i.DescricaoPrincipal)
        .Take(50)
        .ToList();

        return Ok(resultado);
    }

    /// <summary>
    /// Obtém detalhes de um item específico com todas as atas (vigentes e vencidas)
    /// </summary>
    [HttpGet("{codigoItem:int}")]
    public async Task<ActionResult<ItemDetalheDto>> ObterItem(int codigoItem)
    {
        var hoje = DateTime.Today;

        var item = await _db.Itens
            .Include(i => i.Descricoes)
            .Include(i => i.AtaItens)
                .ThenInclude(ai => ai.Ata)
            .FirstOrDefaultAsync(i => i.CodigoItem == codigoItem);

        if (item == null)
            return NotFound("Item não encontrado");

        var atasVigentes = item.AtaItens
            .Where(ai => ai.Ata.DataVigenciaFinal >= hoje)
            .OrderBy(ai => ai.Ata.DataVigenciaFinal)
            .Select(ai => new AtaResumoDto(
                ai.Ata.Id,
                ai.Ata.NumeroAta,
                ai.Ata.DataVigenciaFinal,
                ai.Ata.DiasParaVencer,
                ai.Ata.StatusVigencia,
                null,
                ai.NomeRazaoSocialFornecedor,
                ai.ValorUnitario,
                ai.DescricaoItemOriginal,
                ai.Ata.Itens.Count,
                ai.Ata.Itens.Sum(i => i.ValorTotal ?? 0),
                null
            ))
            .ToList();

        var atasVencidas = item.AtaItens
            .Where(ai => ai.Ata.DataVigenciaFinal < hoje)
            .OrderByDescending(ai => ai.Ata.DataVigenciaFinal)
            .Select(ai => new AtaResumoDto(
                ai.Ata.Id,
                ai.Ata.NumeroAta,
                ai.Ata.DataVigenciaFinal,
                ai.Ata.DiasParaVencer,
                ai.Ata.StatusVigencia,
                null,
                ai.NomeRazaoSocialFornecedor,
                ai.ValorUnitario,
                ai.DescricaoItemOriginal,
                ai.Ata.Itens.Count,
                ai.Ata.Itens.Sum(i => i.ValorTotal ?? 0),
                null
            ))
            .ToList();

        return Ok(new ItemDetalheDto(
            item.CodigoItem,
            item.TipoItem,
            item.DescricaoPrincipal,
            item.CodigoPdm,
            item.NomePdm,
            item.Descricoes.Select(d => d.DescricaoItem).ToList(),
            atasVigentes,
            atasVencidas
        ));
    }

    /// <summary>
    /// Lista itens sem ata vigente (que já tiveram ata no passado)
    /// </summary>
    [HttpGet("sem-ata")]
    public async Task<ActionResult<List<ItemSemAtaDto>>> ItensSemAta([FromQuery] int limite = 50)
    {
        var hoje = DateTime.Today;

        var itensSemAta = await _db.Itens
            .Where(i => i.AtaItens.Any() && !i.AtaItens.Any(ai => ai.Ata.DataVigenciaFinal >= hoje))
            .Select(i => new
            {
                i.CodigoItem,
                i.TipoItem,
                i.DescricaoPrincipal,
                UltimaAtaVencida = i.AtaItens.Max(ai => ai.Ata.DataVigenciaFinal)
            })
            .OrderByDescending(i => i.UltimaAtaVencida)
            .Take(limite)
            .ToListAsync();

        var resultado = itensSemAta.Select(i => new ItemSemAtaDto(
            i.CodigoItem,
            i.TipoItem,
            i.DescricaoPrincipal,
            i.UltimaAtaVencida
        )).ToList();

        return Ok(resultado);
    }

    /// <summary>
    /// Lista itens por status considerando a ata de MAIOR vigência (a que vale)
    /// Busca por palavras em qualquer ordem/posição
    /// </summary>
    [HttpGet("por-status")]
    public async Task<ActionResult<List<ItemAlertaDto>>> ItensPorStatus(
        [FromQuery] string? status = null,
        [FromQuery] string? tipoItem = null,
        [FromQuery] string? busca = null,
        [FromQuery] int? diasMin = null,
        [FromQuery] int? diasMax = null,
        [FromQuery] bool todos = false,
        [FromQuery] int limite = 500)
    {
        var hoje = DateTime.Today;
        var limite30DiasAtras = hoje.AddDays(-30);

        // Busca todos os itens que têm ata (vigente ou não)
        var query = _db.Itens
            .Include(i => i.AtaItens.Where(ai => !ai.ItemExcluido))
                .ThenInclude(ai => ai.Ata)
            .Where(i => i.AtaItens.Any(ai => !ai.ItemExcluido))
            .AsQueryable();

        // Filtro por tipo (Material/Serviço)
        if (!string.IsNullOrEmpty(tipoItem))
        {
            query = query.Where(i => i.TipoItem == tipoItem);
        }

        var itensComAtas = await query.ToListAsync();

        // Filtro por busca (descrição) - busca por palavras em qualquer ordem
        if (!string.IsNullOrEmpty(busca) && busca.Length >= 3)
        {
            itensComAtas = itensComAtas
                .Where(i => ContemTodosTermos(i.DescricaoPrincipal, busca))
                .ToList();
        }

        // Para cada item, pega a ata com MAIOR data de vigência (a que vale)
        var resultado = itensComAtas
            .Select(i =>
            {
                var ataMaiorVigencia = i.AtaItens
                    .Where(ai => !ai.ItemExcluido)
                    .OrderByDescending(ai => ai.Ata.DataVigenciaFinal)
                    .First();

                return new ItemAlertaDto(
                    i.CodigoItem,
                    i.TipoItem,
                    i.DescricaoPrincipal,
                    ataMaiorVigencia.Ata.NumeroAta,
                    ataMaiorVigencia.Ata.DataVigenciaFinal,
                    ataMaiorVigencia.Ata.DiasParaVencer,
                    ataMaiorVigencia.Ata.StatusVigencia,
                    ataMaiorVigencia.Ata.LinkPncp,
                    ataMaiorVigencia.NomeRazaoSocialFornecedor,
                    ataMaiorVigencia.ValorUnitario
                );
            })
            // Filtro por status
            .Where(i => string.IsNullOrEmpty(status) || i.StatusVigencia == status)
            // Se não for "todos", filtra: vigentes OU vencidos há no máximo 30 dias
            .Where(i => todos || i.DataVigenciaFinal >= limite30DiasAtras)
            // Filtro por dias mínimos para vencer
            .Where(i => !diasMin.HasValue || i.DiasParaVencer >= diasMin.Value)
            // Filtro por dias máximos para vencer
            .Where(i => !diasMax.HasValue || i.DiasParaVencer <= diasMax.Value)
            .OrderBy(i => i.DataVigenciaFinal)
            .Take(limite)
            .ToList();

        return Ok(resultado);
    }
}
