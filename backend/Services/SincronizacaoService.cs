using Microsoft.EntityFrameworkCore;
using AtasApi.Data;
using AtasApi.Models;
using System.Text.Json;

namespace AtasApi.Services;

public class SincronizacaoResult
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = "";
    public int PaginasProcessadas { get; set; }
    public int TotalPaginas { get; set; }
    public int ItensProcessados { get; set; }
    public int AtasNovas { get; set; }
    public int ItensNovos { get; set; }
    public int Erros { get; set; }
    public int PeriodosProcessados { get; set; }
}

public class SincronizacaoStatus
{
    public bool EmAndamento { get; set; }
    public int PaginasProcessadas { get; set; }
    public int PaginasSucesso { get; set; }
    public int TotalPaginas { get; set; }
    public int PaginasPendentes { get; set; }
    public int PaginasComErro { get; set; }
    public int TotalItensProcessados { get; set; }
    public DateTime? UltimaAtualizacao { get; set; }
    public DateTime? DataUltimaAta { get; set; }
    public int TotalAtas { get; set; }
    public string? PeriodoAtual { get; set; }
    public List<int> PaginasFalhadas { get; set; } = new();
}

public class SincronizacaoConfig
{
    public bool ModoParalelo { get; set; } = false;
    public int IntervaloEntreRequisicoesMs { get; set; } = 1000;
    public int MaxConcorrencia { get; set; } = 10;
}

public class SincronizacaoService
{
    private readonly AtasDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SincronizacaoService> _logger;

    private static readonly SemaphoreSlim _syncSemaphore = new(1, 1);
    private static bool _sincronizacaoEmAndamento = false;
    private static int _paginasProcessadas = 0;
    private static int _totalPaginas = 0;
    private static int _itensProcessados = 0;
    private static int _paginasComErro = 0;
    private static string? _periodoAtual = null;
    private static readonly List<int> _paginasFalhadas = new();
    private static readonly object _lockPaginasFalhadas = new();
    private static CancellationTokenSource? _cancellationTokenSource;

    private const string BASE_URL = "https://dadosabertos.compras.gov.br/modulo-arp/2_consultarARPItem";
    private const string CODIGO_UNIDADE = "986001";
    private const int ITENS_POR_PAGINA = 500;
    private const int INTERVALO_ENTRE_PAGINAS_MS = 1000;
    private const int MAX_TENTATIVAS = 3;
    private const int TEMPO_ESPERA_RETRY_MS = 5000;
    private const int MAX_ANOS_RETROATIVOS = 5;

    public SincronizacaoService(AtasDbContext db, IHttpClientFactory httpClientFactory, ILogger<SincronizacaoService> logger)
    {
        _db = db;
        _httpClient = httpClientFactory.CreateClient("SincronizacaoClient");
        _logger = logger;
    }

    /// <summary>
    /// Gera os períodos anuais para busca (max 365 dias cada).
    /// Vai do ano atual até MAX_ANOS_RETROATIVOS anos atrás.
    /// </summary>
    private static List<(string dataMin, string dataMax, string label)> GerarPeriodos()
    {
        var periodos = new List<(string, string, string)>();
        var hoje = DateTime.Today;

        for (int i = 0; i <= MAX_ANOS_RETROATIVOS; i++)
        {
            var anoInicio = hoje.Year - i;
            var dataMin = $"{anoInicio}-01-01";
            var dataMax = $"{anoInicio + 1}-01-01";
            var label = $"{anoInicio}";
            periodos.Add((dataMin, dataMax, label));
        }

        return periodos;
    }

    /// <summary>
    /// Para a sincronização em andamento
    /// </summary>
    public bool PararSincronizacao()
    {
        if (!_sincronizacaoEmAndamento || _cancellationTokenSource == null)
        {
            return false;
        }

        _logger.LogWarning("Solicitação de parada da sincronização recebida");
        _cancellationTokenSource.Cancel();
        return true;
    }

    /// <summary>
    /// Sincronização completa - percorre todos os períodos anuais, todas as páginas
    /// </summary>
    public async Task<SincronizacaoResult> SincronizarAsync(CancellationToken cancellationToken)
    {
        if (_sincronizacaoEmAndamento)
        {
            return new SincronizacaoResult { Sucesso = false, Mensagem = "Sincronização já em andamento" };
        }

        bool acquired = await _syncSemaphore.WaitAsync(0, cancellationToken);
        if (!acquired)
        {
            return new SincronizacaoResult { Sucesso = false, Mensagem = "Sincronização já em andamento" };
        }

        try
        {
            _sincronizacaoEmAndamento = true;
            _paginasProcessadas = 0;
            _totalPaginas = 0;
            _itensProcessados = 0;
            _paginasComErro = 0;
            _periodoAtual = null;
            lock (_lockPaginasFalhadas) { _paginasFalhadas.Clear(); }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;

            var resultado = new SincronizacaoResult();
            var periodos = GerarPeriodos();

            _logger.LogInformation("Iniciando sincronização completa - {Count} períodos anuais", periodos.Count);

            foreach (var (dataMin, dataMax, label) in periodos)
            {
                if (token.IsCancellationRequested) break;

                _periodoAtual = label;
                _logger.LogInformation("=== Período {Label}: {DataMin} a {DataMax} ===", label, dataMin, dataMax);

                var resultadoPeriodo = await SincronizarPeriodoAsync(dataMin, dataMax, token);

                resultado.ItensProcessados += resultadoPeriodo.ItensProcessados;
                resultado.AtasNovas += resultadoPeriodo.AtasNovas;
                resultado.ItensNovos += resultadoPeriodo.ItensNovos;
                resultado.PaginasProcessadas += resultadoPeriodo.PaginasProcessadas;
                resultado.TotalPaginas += resultadoPeriodo.TotalPaginas;
                resultado.Erros += resultadoPeriodo.Erros;
                resultado.PeriodosProcessados++;

                // Se não encontrou registros neste período, parar de voltar no tempo
                if (resultadoPeriodo.TotalPaginas == 0)
                {
                    _logger.LogInformation("Período {Label} sem registros, parando busca retroativa", label);
                    break;
                }
            }

            _periodoAtual = null;

            if (!token.IsCancellationRequested)
            {
                resultado.Sucesso = resultado.Erros == 0;
                resultado.Mensagem = resultado.Sucesso
                    ? $"Sincronização concluída: {resultado.PeriodosProcessados} período(s), {resultado.ItensProcessados} itens ({resultado.ItensNovos} novos), {resultado.AtasNovas} atas novas"
                    : $"Sincronização concluída com {resultado.Erros} erro(s) em {resultado.PeriodosProcessados} período(s)";
            }
            else
            {
                resultado.Sucesso = false;
                resultado.Mensagem = $"Sincronização cancelada. {resultado.ItensProcessados} itens processados antes da parada.";
            }

            await RegistrarUltimaAtualizacaoAsync(CancellationToken.None);
            return resultado;
        }
        finally
        {
            _sincronizacaoEmAndamento = false;
            _periodoAtual = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _syncSemaphore.Release();
        }
    }

    /// <summary>
    /// Sincronização paralela - mesma lógica por períodos, mas com concorrência nas páginas
    /// </summary>
    public async Task<SincronizacaoResult> SincronizarParaleloAsync(SincronizacaoConfig config, CancellationToken cancellationToken)
    {
        if (_sincronizacaoEmAndamento)
        {
            return new SincronizacaoResult { Sucesso = false, Mensagem = "Sincronização já em andamento" };
        }

        bool acquired = await _syncSemaphore.WaitAsync(0, cancellationToken);
        if (!acquired)
        {
            return new SincronizacaoResult { Sucesso = false, Mensagem = "Sincronização já em andamento" };
        }

        try
        {
            _sincronizacaoEmAndamento = true;
            _paginasProcessadas = 0;
            _totalPaginas = 0;
            _itensProcessados = 0;
            _paginasComErro = 0;
            _periodoAtual = null;
            lock (_lockPaginasFalhadas) { _paginasFalhadas.Clear(); }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;

            var resultado = new SincronizacaoResult();
            var periodos = GerarPeriodos();

            _logger.LogInformation("Iniciando sincronização PARALELA - {Count} períodos, MaxConcorrência: {Max}",
                periodos.Count, config.MaxConcorrencia);

            foreach (var (dataMin, dataMax, label) in periodos)
            {
                if (token.IsCancellationRequested) break;

                _periodoAtual = label;
                _logger.LogInformation("=== Período {Label}: {DataMin} a {DataMax} (paralelo) ===", label, dataMin, dataMax);

                var resultadoPeriodo = await SincronizarPeriodoParaleloAsync(dataMin, dataMax, config, token);

                resultado.ItensProcessados += resultadoPeriodo.ItensProcessados;
                resultado.AtasNovas += resultadoPeriodo.AtasNovas;
                resultado.ItensNovos += resultadoPeriodo.ItensNovos;
                resultado.PaginasProcessadas += resultadoPeriodo.PaginasProcessadas;
                resultado.TotalPaginas += resultadoPeriodo.TotalPaginas;
                resultado.Erros += resultadoPeriodo.Erros;
                resultado.PeriodosProcessados++;

                if (resultadoPeriodo.TotalPaginas == 0)
                {
                    _logger.LogInformation("Período {Label} sem registros, parando busca retroativa", label);
                    break;
                }
            }

            _periodoAtual = null;

            if (!token.IsCancellationRequested)
            {
                resultado.Sucesso = resultado.Erros == 0;
                resultado.Mensagem = resultado.Sucesso
                    ? $"Sincronização paralela concluída: {resultado.PeriodosProcessados} período(s), {resultado.ItensProcessados} itens ({resultado.ItensNovos} novos)"
                    : $"Sincronização concluída com {resultado.Erros} erro(s)";
            }
            else
            {
                resultado.Sucesso = false;
                resultado.Mensagem = $"Sincronização cancelada. {resultado.ItensProcessados} itens processados antes da parada.";
            }

            await RegistrarUltimaAtualizacaoAsync(CancellationToken.None);
            return resultado;
        }
        finally
        {
            _sincronizacaoEmAndamento = false;
            _periodoAtual = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _syncSemaphore.Release();
        }
    }

    /// <summary>
    /// Sincronização incremental - busca apenas do último ano sincronizado até hoje
    /// </summary>
    public async Task<SincronizacaoResult> SincronizarIncrementalAsync(CancellationToken cancellationToken)
    {
        var ultimaData = await _db.Atas.MaxAsync(a => (DateTime?)a.DataVigenciaInicial, cancellationToken);

        if (ultimaData == null)
        {
            _logger.LogInformation("Nenhuma ata encontrada. Iniciando sincronização completa.");
            return await SincronizarAsync(cancellationToken);
        }

        if (_sincronizacaoEmAndamento)
        {
            return new SincronizacaoResult { Sucesso = false, Mensagem = "Sincronização já em andamento" };
        }

        bool acquired = await _syncSemaphore.WaitAsync(0, cancellationToken);
        if (!acquired)
        {
            return new SincronizacaoResult { Sucesso = false, Mensagem = "Sincronização já em andamento" };
        }

        try
        {
            _sincronizacaoEmAndamento = true;
            _paginasProcessadas = 0;
            _totalPaginas = 0;
            _itensProcessados = 0;
            _paginasComErro = 0;
            _periodoAtual = null;

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;

            // Buscar do início do ano da última ata até o próximo ano
            var dataMin = new DateTime(ultimaData.Value.Year, 1, 1).ToString("yyyy-MM-dd");
            var dataMax = new DateTime(DateTime.Today.Year + 1, 1, 1).ToString("yyyy-MM-dd");

            _periodoAtual = $"Incremental";
            _logger.LogInformation("Sincronização incremental: {DataMin} a {DataMax}", dataMin, dataMax);

            var resultado = await SincronizarPeriodoAsync(dataMin, dataMax, token);

            _periodoAtual = null;

            resultado.Sucesso = resultado.Erros == 0;
            resultado.Mensagem = resultado.Sucesso
                ? $"Atualização concluída: {resultado.ItensProcessados} itens ({resultado.ItensNovos} novos)"
                : $"Atualização concluída com {resultado.Erros} erro(s)";

            await RegistrarUltimaAtualizacaoAsync(CancellationToken.None);
            return resultado;
        }
        finally
        {
            _sincronizacaoEmAndamento = false;
            _periodoAtual = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _syncSemaphore.Release();
        }
    }

    /// <summary>
    /// Continuar sincronização - reprocessa páginas que falharam
    /// </summary>
    public async Task<SincronizacaoResult> ContinuarSincronizacaoAsync(CancellationToken cancellationToken)
    {
        List<int> paginasParaProcessar;
        lock (_lockPaginasFalhadas)
        {
            paginasParaProcessar = _paginasFalhadas.OrderBy(p => p).ToList();
        }

        if (paginasParaProcessar.Count == 0)
        {
            return new SincronizacaoResult { Sucesso = true, Mensagem = "Nenhuma página pendente para reprocessar" };
        }

        return new SincronizacaoResult
        {
            Sucesso = true,
            Mensagem = $"{paginasParaProcessar.Count} páginas falharam. Use sincronização completa para reprocessar."
        };
    }

    // ========== MÉTODOS INTERNOS DE PERÍODO ==========

    /// <summary>
    /// Processa todas as páginas de um período específico (sequencial)
    /// </summary>
    private async Task<SincronizacaoResult> SincronizarPeriodoAsync(string dataMin, string dataMax, CancellationToken token)
    {
        var resultado = new SincronizacaoResult();

        var primeiraResposta = await BuscarPaginaComRetryAsync(1, dataMin, dataMax, token);
        if (primeiraResposta == null)
        {
            resultado.Erros++;
            resultado.Mensagem = "Erro ao conectar com a API";
            return resultado;
        }

        var totalPaginas = primeiraResposta.RootElement.GetProperty("totalPaginas").GetInt32();
        var totalRegistros = primeiraResposta.RootElement.GetProperty("totalRegistros").GetInt32();

        if (totalRegistros == 0)
        {
            _logger.LogInformation("Período {DataMin}-{DataMax}: nenhum registro", dataMin, dataMax);
            return resultado;
        }

        resultado.TotalPaginas = totalPaginas;
        _totalPaginas += totalPaginas;
        _logger.LogInformation("Período {DataMin}-{DataMax}: {Total} registros em {Paginas} páginas",
            dataMin, dataMax, totalRegistros, totalPaginas);

        // Processar primeira página
        var (itens, atasNovas, itensNovos) = await ProcessarPaginaAsync(primeiraResposta, token);
        resultado.ItensProcessados += itens;
        resultado.AtasNovas += atasNovas;
        resultado.ItensNovos += itensNovos;
        resultado.PaginasProcessadas++;
        Interlocked.Add(ref _itensProcessados, itens);
        Interlocked.Increment(ref _paginasProcessadas);

        // Processar páginas restantes
        for (int pagina = 2; pagina <= totalPaginas; pagina++)
        {
            if (token.IsCancellationRequested) break;

            try
            {
                var json = await BuscarPaginaComRetryAsync(pagina, dataMin, dataMax, token);
                if (json != null)
                {
                    (itens, atasNovas, itensNovos) = await ProcessarPaginaAsync(json, token);
                    resultado.ItensProcessados += itens;
                    resultado.AtasNovas += atasNovas;
                    resultado.ItensNovos += itensNovos;
                    Interlocked.Add(ref _itensProcessados, itens);
                }
                else
                {
                    resultado.Erros++;
                    Interlocked.Increment(ref _paginasComErro);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar página {Pagina}", pagina);
                resultado.Erros++;
                Interlocked.Increment(ref _paginasComErro);
            }

            resultado.PaginasProcessadas++;
            Interlocked.Increment(ref _paginasProcessadas);

            if (!token.IsCancellationRequested)
            {
                try { await Task.Delay(INTERVALO_ENTRE_PAGINAS_MS, token); }
                catch (OperationCanceledException) { break; }
            }
        }

        return resultado;
    }

    /// <summary>
    /// Processa todas as páginas de um período específico (paralelo)
    /// </summary>
    private async Task<SincronizacaoResult> SincronizarPeriodoParaleloAsync(
        string dataMin, string dataMax, SincronizacaoConfig config, CancellationToken token)
    {
        var resultado = new SincronizacaoResult();

        var primeiraResposta = await BuscarPaginaComRetryAsync(1, dataMin, dataMax, token);
        if (primeiraResposta == null)
        {
            resultado.Erros++;
            return resultado;
        }

        var totalPaginas = primeiraResposta.RootElement.GetProperty("totalPaginas").GetInt32();
        var totalRegistros = primeiraResposta.RootElement.GetProperty("totalRegistros").GetInt32();

        if (totalRegistros == 0) return resultado;

        resultado.TotalPaginas = totalPaginas;
        _totalPaginas += totalPaginas;

        // Processar primeira página
        var (itens, atasNovas, itensNovos) = await ProcessarPaginaAsync(primeiraResposta, token);
        resultado.ItensProcessados += itens;
        resultado.AtasNovas += atasNovas;
        resultado.ItensNovos += itensNovos;
        resultado.PaginasProcessadas++;
        Interlocked.Add(ref _itensProcessados, itens);
        Interlocked.Increment(ref _paginasProcessadas);

        // Processar restante em paralelo
        using var concurrencySemaphore = new SemaphoreSlim(config.MaxConcorrencia, config.MaxConcorrencia);
        var tasks = new List<Task<(int pagina, int itens, int atasNovas, int itensNovos, bool sucesso)>>();

        for (int pagina = 2; pagina <= totalPaginas; pagina++)
        {
            if (token.IsCancellationRequested) break;

            var paginaAtual = pagina;
            await concurrencySemaphore.WaitAsync(token);

            var task = Task.Run(async () =>
            {
                try
                {
                    var json = await BuscarPaginaComRetryAsync(paginaAtual, dataMin, dataMax, token);
                    if (json != null)
                    {
                        var (it, an, inv) = await ProcessarPaginaAsync(json, token);
                        Interlocked.Add(ref _itensProcessados, it);
                        Interlocked.Increment(ref _paginasProcessadas);
                        return (paginaAtual, it, an, inv, true);
                    }
                    else
                    {
                        Interlocked.Increment(ref _paginasComErro);
                        Interlocked.Increment(ref _paginasProcessadas);
                        return (paginaAtual, 0, 0, 0, false);
                    }
                }
                catch (OperationCanceledException)
                {
                    return (paginaAtual, 0, 0, 0, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro página {Pagina}", paginaAtual);
                    Interlocked.Increment(ref _paginasComErro);
                    Interlocked.Increment(ref _paginasProcessadas);
                    return (paginaAtual, 0, 0, 0, false);
                }
                finally
                {
                    concurrencySemaphore.Release();
                }
            }, token);

            tasks.Add(task);

            if (pagina < totalPaginas && !token.IsCancellationRequested)
            {
                try { await Task.Delay(config.IntervaloEntreRequisicoesMs, token); }
                catch (OperationCanceledException) { break; }
            }
        }

        try
        {
            var results = await Task.WhenAll(tasks);
            foreach (var (_, itensProc, atasN, itensN, sucesso) in results)
            {
                resultado.ItensProcessados += itensProc;
                resultado.AtasNovas += atasN;
                resultado.ItensNovos += itensN;
                resultado.PaginasProcessadas++;
                if (!sucesso) resultado.Erros++;
            }
        }
        catch (OperationCanceledException) { }

        return resultado;
    }

    // ========== BUSCA E PROCESSAMENTO ==========

    private async Task<JsonDocument?> BuscarPaginaComRetryAsync(int pagina, string dataMin, string dataMax, CancellationToken cancellationToken)
    {
        for (int tentativa = 1; tentativa <= MAX_TENTATIVAS; tentativa++)
        {
            try
            {
                var url = $"{BASE_URL}?pagina={pagina}&tamanhoPagina={ITENS_POR_PAGINA}" +
                          $"&codigoUnidadeGerenciadora={CODIGO_UNIDADE}" +
                          $"&dataVigenciaInicialMin={dataMin}&dataVigenciaInicialMax={dataMax}";

                _logger.LogInformation("Buscando página {Pagina} [{DataMin}~{DataMax}] (tentativa {T}/{Max})",
                    pagina, dataMin, dataMax, tentativa, MAX_TENTATIVAS);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(45));

                var response = await _httpClient.GetAsync(url, timeoutCts.Token);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                return JsonDocument.Parse(content);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout tentativa {T} página {P}", tentativa, pagina);
                if (tentativa < MAX_TENTATIVAS)
                    await Task.Delay(TEMPO_ESPERA_RETRY_MS, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Erro HTTP tentativa {T} página {P}", tentativa, pagina);
                if (tentativa < MAX_TENTATIVAS)
                    await Task.Delay(TEMPO_ESPERA_RETRY_MS, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro tentativa {T} página {P}", tentativa, pagina);
                if (tentativa < MAX_TENTATIVAS)
                    await Task.Delay(TEMPO_ESPERA_RETRY_MS, cancellationToken);
            }
        }

        _logger.LogError("Falha página {P} após {Max} tentativas", pagina, MAX_TENTATIVAS);
        return null;
    }

    private async Task<(int processados, int atasNovas, int itensNovos)> ProcessarPaginaAsync(JsonDocument json, CancellationToken cancellationToken)
    {
        int itensProcessados = 0;
        int atasNovas = 0;
        int itensNovos = 0;
        var itens = json.RootElement.GetProperty("resultado").EnumerateArray();

        foreach (var itemJson in itens)
        {
            try
            {
                var (ataNova, itemNovo) = await ProcessarItemAtaAsync(itemJson, cancellationToken);
                itensProcessados++;
                if (ataNova) atasNovas++;
                if (itemNovo) itensNovos++;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true ||
                                                ex.InnerException?.Message.Contains("UNIQUE") == true ||
                                                ex.InnerException?.Message.Contains("23505") == true)
            {
                _db.ChangeTracker.Clear();
                itensProcessados++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar item de ata");
                _db.ChangeTracker.Clear();
            }
        }

        return (itensProcessados, atasNovas, itensNovos);
    }

    private async Task<(bool ataNova, bool itemNovo)> ProcessarItemAtaAsync(JsonElement itemJson, CancellationToken cancellationToken)
    {
        bool ataNova = false;
        bool itemNovo = false;

        var numeroControlePncpAta = GetStringOrNull(itemJson, "numeroControlePncpAta");
        if (string.IsNullOrEmpty(numeroControlePncpAta))
        {
            return (false, false);
        }

        var ata = await _db.Atas.FirstOrDefaultAsync(a => a.NumeroControlePncpAta == numeroControlePncpAta, cancellationToken);

        if (ata == null)
        {
            ata = CriarAtaDeItem(itemJson);
            _db.Atas.Add(ata);
            await _db.SaveChangesAsync(cancellationToken);
            ataNova = true;
        }
        else
        {
            AtualizarAtaDeItem(ata, itemJson);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var codigoItem = GetIntOrNull(itemJson, "codigoItem");
        var descricaoItem = GetStringOrNull(itemJson, "descricaoItem");

        if (codigoItem.HasValue)
        {
            var item = await _db.Itens.FirstOrDefaultAsync(i => i.CodigoItem == codigoItem.Value, cancellationToken);

            if (item == null)
            {
                item = new Item
                {
                    CodigoItem = codigoItem.Value,
                    TipoItem = GetStringOrNull(itemJson, "tipoItem") ?? "Material",
                    CodigoPdm = GetIntOrNull(itemJson, "codigoPdm"),
                    NomePdm = GetStringOrNull(itemJson, "nomePdm"),
                    DescricaoPrincipal = descricaoItem,
                    DataCriacao = DateTime.UtcNow
                };
                _db.Itens.Add(item);
                await _db.SaveChangesAsync(cancellationToken);
            }
            else if (string.IsNullOrEmpty(item.DescricaoPrincipal) && !string.IsNullOrEmpty(descricaoItem))
            {
                item.DescricaoPrincipal = descricaoItem;
                await _db.SaveChangesAsync(cancellationToken);
            }

            if (!string.IsNullOrEmpty(descricaoItem))
            {
                var descricaoExiste = await _db.ItemDescricoes
                    .AnyAsync(d => d.CodigoItem == codigoItem.Value && d.DescricaoItem == descricaoItem, cancellationToken);

                if (!descricaoExiste)
                {
                    _db.ItemDescricoes.Add(new ItemDescricao
                    {
                        CodigoItem = codigoItem.Value,
                        DescricaoItem = descricaoItem,
                        DataRegistro = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            var ataItem = await _db.AtaItens
                .FirstOrDefaultAsync(ai => ai.AtaId == ata.Id && ai.CodigoItem == codigoItem.Value && ai.NumeroItem == GetStringOrNull(itemJson, "numeroItem"), cancellationToken);

            if (ataItem == null)
            {
                ataItem = new AtaItem
                {
                    AtaId = ata.Id,
                    CodigoItem = codigoItem.Value,
                    DescricaoItemOriginal = descricaoItem,
                    NumeroItem = GetStringOrNull(itemJson, "numeroItem"),
                    QuantidadeHomologadaItem = GetDecimalOrNull(itemJson, "quantidadeHomologadaItem"),
                    ClassificacaoFornecedor = GetStringOrNull(itemJson, "classificacaoFornecedor"),
                    NiFornecedor = GetStringOrNull(itemJson, "niFornecedor"),
                    NomeRazaoSocialFornecedor = GetStringOrNull(itemJson, "nomeRazaoSocialFornecedor"),
                    QuantidadeHomologadaVencedor = GetDecimalOrNull(itemJson, "quantidadeHomologadaVencedor"),
                    ValorUnitario = GetDecimalOrNull(itemJson, "valorUnitario"),
                    ValorTotal = GetDecimalOrNull(itemJson, "valorTotal"),
                    MaximoAdesao = GetDecimalOrNull(itemJson, "maximoAdesao"),
                    QuantidadeEmpenhada = GetDecimalOrNull(itemJson, "quantidadeEmpenhada"),
                    PercentualMaiorDesconto = GetDecimalOrNull(itemJson, "percentualMaiorDesconto"),
                    SituacaoSicaf = GetStringOrNull(itemJson, "situacaoSicaf"),
                    ItemExcluido = GetBoolOrDefault(itemJson, "itemExcluido"),
                    DataHoraExclusao = GetDateOrNull(itemJson, "dataHoraExclusao")
                };
                _db.AtaItens.Add(ataItem);
                itemNovo = true;
            }
            else
            {
                ataItem.DescricaoItemOriginal = descricaoItem;
                ataItem.QuantidadeHomologadaItem = GetDecimalOrNull(itemJson, "quantidadeHomologadaItem");
                ataItem.ClassificacaoFornecedor = GetStringOrNull(itemJson, "classificacaoFornecedor");
                ataItem.NiFornecedor = GetStringOrNull(itemJson, "niFornecedor");
                ataItem.NomeRazaoSocialFornecedor = GetStringOrNull(itemJson, "nomeRazaoSocialFornecedor");
                ataItem.QuantidadeHomologadaVencedor = GetDecimalOrNull(itemJson, "quantidadeHomologadaVencedor");
                ataItem.ValorUnitario = GetDecimalOrNull(itemJson, "valorUnitario");
                ataItem.ValorTotal = GetDecimalOrNull(itemJson, "valorTotal");
                ataItem.MaximoAdesao = GetDecimalOrNull(itemJson, "maximoAdesao");
                ataItem.QuantidadeEmpenhada = GetDecimalOrNull(itemJson, "quantidadeEmpenhada");
                ataItem.PercentualMaiorDesconto = GetDecimalOrNull(itemJson, "percentualMaiorDesconto");
                ataItem.SituacaoSicaf = GetStringOrNull(itemJson, "situacaoSicaf");
                ataItem.ItemExcluido = GetBoolOrDefault(itemJson, "itemExcluido");
                ataItem.DataHoraExclusao = GetDateOrNull(itemJson, "dataHoraExclusao");
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return (ataNova, itemNovo);
    }

    // ========== HELPERS ==========

    private AtaRegistroPreco CriarAtaDeItem(JsonElement json)
    {
        return new AtaRegistroPreco
        {
            NumeroAta = GetStringOrNull(json, "numeroAtaRegistroPreco") ?? "SEM NUMERO",
            CodigoUnidadeGerenciadora = GetStringOrNull(json, "codigoUnidadeGerenciadora") ?? CODIGO_UNIDADE,
            NumeroCompra = GetStringOrNull(json, "numeroCompra"),
            AnoCompra = GetStringOrNull(json, "anoCompra"),
            CodigoModalidadeCompra = GetStringOrNull(json, "codigoModalidadeCompra"),
            NomeModalidadeCompra = GetStringOrNull(json, "nomeModalidadeCompra"),
            DataAssinatura = GetDateOrNull(json, "dataAssinatura"),
            DataVigenciaInicial = GetDateOrNull(json, "dataVigenciaInicial") ?? DateTime.MinValue,
            DataVigenciaFinal = GetDateOrNull(json, "dataVigenciaFinal") ?? DateTime.MinValue,
            NomeUnidadeGerenciadora = GetStringOrNull(json, "nomeUnidadeGerenciadora"),
            IdCompra = GetStringOrNull(json, "idCompra"),
            NumeroControlePncpCompra = GetStringOrNull(json, "numeroControlePncpCompra"),
            NumeroControlePncpAta = GetStringOrNull(json, "numeroControlePncpAta"),
            DataHoraInclusao = DateTime.UtcNow,
            DataHoraAtualizacao = DateTime.UtcNow
        };
    }

    private void AtualizarAtaDeItem(AtaRegistroPreco ata, JsonElement json)
    {
        ata.NumeroAta = GetStringOrNull(json, "numeroAtaRegistroPreco") ?? ata.NumeroAta;
        ata.CodigoUnidadeGerenciadora = GetStringOrNull(json, "codigoUnidadeGerenciadora") ?? ata.CodigoUnidadeGerenciadora;
        ata.NumeroCompra = GetStringOrNull(json, "numeroCompra") ?? ata.NumeroCompra;
        ata.AnoCompra = GetStringOrNull(json, "anoCompra") ?? ata.AnoCompra;
        ata.CodigoModalidadeCompra = GetStringOrNull(json, "codigoModalidadeCompra") ?? ata.CodigoModalidadeCompra;
        ata.NomeModalidadeCompra = GetStringOrNull(json, "nomeModalidadeCompra") ?? ata.NomeModalidadeCompra;

        var dataAssinatura = GetDateOrNull(json, "dataAssinatura");
        if (dataAssinatura.HasValue) ata.DataAssinatura = dataAssinatura;

        var dataVigenciaInicial = GetDateOrNull(json, "dataVigenciaInicial");
        if (dataVigenciaInicial.HasValue) ata.DataVigenciaInicial = dataVigenciaInicial.Value;

        var dataVigenciaFinal = GetDateOrNull(json, "dataVigenciaFinal");
        if (dataVigenciaFinal.HasValue) ata.DataVigenciaFinal = dataVigenciaFinal.Value;

        ata.NomeUnidadeGerenciadora = GetStringOrNull(json, "nomeUnidadeGerenciadora") ?? ata.NomeUnidadeGerenciadora;
        ata.IdCompra = GetStringOrNull(json, "idCompra") ?? ata.IdCompra;
        ata.NumeroControlePncpCompra = GetStringOrNull(json, "numeroControlePncpCompra") ?? ata.NumeroControlePncpCompra;
        ata.DataHoraAtualizacao = DateTime.UtcNow;
    }

    private async Task RegistrarUltimaAtualizacaoAsync(CancellationToken cancellationToken)
    {
        var config = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "ultima_sincronizacao", cancellationToken);
        if (config == null)
        {
            config = new ConfiguracaoSistema
            {
                Chave = "ultima_sincronizacao",
                Valor = DateTime.UtcNow.ToString("o"),
                Descricao = "Data/hora da última sincronização"
            };
            _db.Configuracoes.Add(config);
        }
        else
        {
            config.Valor = DateTime.UtcNow.ToString("o");
            config.DataAtualizacao = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SincronizacaoStatus> ObterStatusAsync(CancellationToken cancellationToken)
    {
        DateTime? ultimaAtualizacao = null;
        var config = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "ultima_sincronizacao", cancellationToken);
        if (config != null && DateTime.TryParse(config.Valor, out var dt))
        {
            ultimaAtualizacao = dt;
        }

        var dataUltimaAta = await _db.Atas.MaxAsync(a => (DateTime?)a.DataVigenciaInicial, cancellationToken);
        var totalAtas = await _db.Atas.CountAsync(cancellationToken);

        List<int> paginasFalhadas;
        lock (_lockPaginasFalhadas)
        {
            paginasFalhadas = _paginasFalhadas.OrderBy(p => p).ToList();
        }

        var paginasSucesso = _paginasProcessadas - _paginasComErro;

        return new SincronizacaoStatus
        {
            EmAndamento = _sincronizacaoEmAndamento,
            TotalPaginas = _totalPaginas,
            PaginasProcessadas = _paginasProcessadas,
            PaginasSucesso = paginasSucesso > 0 ? paginasSucesso : 0,
            PaginasPendentes = _sincronizacaoEmAndamento ? _totalPaginas - _paginasProcessadas : 0,
            PaginasComErro = _paginasComErro,
            TotalItensProcessados = _itensProcessados,
            UltimaAtualizacao = ultimaAtualizacao,
            DataUltimaAta = dataUltimaAta,
            TotalAtas = totalAtas,
            PeriodoAtual = _periodoAtual,
            PaginasFalhadas = paginasFalhadas
        };
    }

    public async Task LimparLogsAsync(CancellationToken cancellationToken)
    {
        await _db.SincronizacaoLogs.ExecuteDeleteAsync(cancellationToken);
    }

    public Task<int> ResetarPaginasTravadasAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }

    // ========== JSON HELPERS ==========

    private static string? GetStringOrNull(JsonElement json, string propertyName)
    {
        return json.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
            ? prop.GetString()
            : null;
    }

    private static int? GetIntOrNull(JsonElement json, string propertyName)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            if (prop.ValueKind == JsonValueKind.Number) return prop.GetInt32();
            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var val)) return val;
        }
        return null;
    }

    private static decimal? GetDecimalOrNull(JsonElement json, string propertyName)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            if (prop.ValueKind == JsonValueKind.Number) return prop.GetDecimal();
            if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var val)) return val;
        }
        return null;
    }

    private static DateTime? GetDateOrNull(JsonElement json, string propertyName)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            var dateStr = prop.GetString();
            if (DateTime.TryParse(dateStr, out var date)) return date;
        }
        return null;
    }

    private static bool GetBoolOrDefault(JsonElement json, string propertyName, bool defaultValue = false)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            if (prop.ValueKind == JsonValueKind.True) return true;
            if (prop.ValueKind == JsonValueKind.False) return false;
            if (prop.ValueKind == JsonValueKind.String)
            {
                var str = prop.GetString()?.ToLower();
                return str == "true" || str == "1" || str == "sim";
            }
        }
        return defaultValue;
    }
}
