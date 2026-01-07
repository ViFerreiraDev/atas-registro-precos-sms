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
    private static readonly List<int> _paginasFalhadas = new();
    private static readonly object _lockPaginasFalhadas = new();
    private static CancellationTokenSource? _cancellationTokenSource;

    private const string CODIGO_UNIDADE = "986001";
    private const string DATA_VIGENCIA_MIN = "2000-01-01";
    private const string DATA_VIGENCIA_MAX = "2050-01-01";
    private const int ITENS_POR_PAGINA = 500;
    private const int INTERVALO_ENTRE_PAGINAS_MS = 1000;
    private const int MAX_TENTATIVAS = 3;
    private const int TEMPO_ESPERA_RETRY_MS = 5000;

    public SincronizacaoService(AtasDbContext db, IHttpClientFactory httpClientFactory, ILogger<SincronizacaoService> logger)
    {
        _db = db;
        _httpClient = httpClientFactory.CreateClient("SincronizacaoClient");
        _logger = logger;
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

    public async Task<SincronizacaoResult> SincronizarAsync(CancellationToken cancellationToken)
    {
        if (_sincronizacaoEmAndamento)
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem = "Sincronização já em andamento"
            };
        }

        bool acquired = await _syncSemaphore.WaitAsync(0, cancellationToken);
        if (!acquired)
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem = "Sincronização já em andamento"
            };
        }

        try
        {
            _sincronizacaoEmAndamento = true;
            _paginasProcessadas = 0;
            _totalPaginas = 0;
            _itensProcessados = 0;
            _paginasComErro = 0;

            // Criar token combinado para permitir cancelamento
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;

            var resultado = new SincronizacaoResult();

            _logger.LogInformation("Iniciando sincronização com endpoint dadosabertos.compras.gov.br - UASG {Uasg}", CODIGO_UNIDADE);

            // Buscar primeira página para saber total
            var primeiraResposta = await BuscarPaginaComRetryAsync(1, token);
            if (primeiraResposta == null)
            {
                resultado.Sucesso = false;
                resultado.Mensagem = "Erro ao conectar com a API";
                return resultado;
            }

            _totalPaginas = primeiraResposta.RootElement.GetProperty("totalPaginas").GetInt32();
            int totalRegistros = primeiraResposta.RootElement.GetProperty("totalRegistros").GetInt32();

            if (totalRegistros == 0)
            {
                resultado.Sucesso = true;
                resultado.Mensagem = "Nenhum registro encontrado";
                return resultado;
            }

            resultado.TotalPaginas = _totalPaginas;
            _logger.LogInformation("Total: {TotalRegistros} registros em {TotalPaginas} páginas", totalRegistros, _totalPaginas);

            // Processar primeira página
            var (itens, atasNovas, itensNovos) = await ProcessarPaginaAsync(primeiraResposta, token);
            resultado.ItensProcessados += itens;
            resultado.AtasNovas += atasNovas;
            resultado.ItensNovos += itensNovos;
            _itensProcessados += itens;
            _paginasProcessadas++;
            resultado.PaginasProcessadas++;

            // Processar páginas restantes
            for (int pagina = 2; pagina <= _totalPaginas; pagina++)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    var json = await BuscarPaginaComRetryAsync(pagina, token);
                    if (json != null)
                    {
                        (itens, atasNovas, itensNovos) = await ProcessarPaginaAsync(json, token);
                        resultado.ItensProcessados += itens;
                        resultado.AtasNovas += atasNovas;
                        resultado.ItensNovos += itensNovos;
                        _itensProcessados += itens;
                    }
                    else
                    {
                        resultado.Erros++;
                        _paginasComErro++;
                        _logger.LogWarning("Página {Pagina} ignorada após falhas", pagina);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Sincronização cancelada pelo usuário na página {Pagina}", pagina);
                    resultado.Mensagem = "Sincronização cancelada pelo usuário";
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar página {Pagina}", pagina);
                    resultado.Erros++;
                    _paginasComErro++;
                }

                // Sempre atualiza o progresso (mesmo se falhou)
                _paginasProcessadas++;
                resultado.PaginasProcessadas++;

                // Intervalo entre páginas
                if (!token.IsCancellationRequested)
                {
                    await Task.Delay(INTERVALO_ENTRE_PAGINAS_MS, token);
                }
            }

            if (!token.IsCancellationRequested)
            {
                resultado.Sucesso = resultado.Erros == 0;
                resultado.Mensagem = resultado.Sucesso
                    ? $"Sincronização concluída: {resultado.ItensProcessados} itens processados ({resultado.ItensNovos} novos)"
                    : $"Sincronização concluída com {resultado.Erros} erro(s) em {_paginasComErro} página(s)";
            }
            else
            {
                resultado.Sucesso = false;
                resultado.Mensagem = $"Sincronização cancelada. {resultado.ItensProcessados} itens processados antes da parada.";
            }

            // Registrar última atualização
            await RegistrarUltimaAtualizacaoAsync(CancellationToken.None);

            return resultado;
        }
        finally
        {
            _sincronizacaoEmAndamento = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _syncSemaphore.Release();
        }
    }

    /// <summary>
    /// Sincronização paralela - dispara 1 requisição por segundo em threads separadas
    /// </summary>
    public async Task<SincronizacaoResult> SincronizarParaleloAsync(SincronizacaoConfig config, CancellationToken cancellationToken)
    {
        if (_sincronizacaoEmAndamento)
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem = "Sincronização já em andamento"
            };
        }

        bool acquired = await _syncSemaphore.WaitAsync(0, cancellationToken);
        if (!acquired)
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem = "Sincronização já em andamento"
            };
        }

        try
        {
            _sincronizacaoEmAndamento = true;
            _paginasProcessadas = 0;
            _totalPaginas = 0;
            _itensProcessados = 0;
            _paginasComErro = 0;
            lock (_lockPaginasFalhadas) { _paginasFalhadas.Clear(); }

            // Criar token combinado para permitir cancelamento
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;

            var resultado = new SincronizacaoResult();

            _logger.LogInformation("Iniciando sincronização PARALELA - Intervalo: {Intervalo}ms, MaxConcorrência: {Max}",
                config.IntervaloEntreRequisicoesMs, config.MaxConcorrencia);

            // Buscar primeira página para saber total
            var primeiraResposta = await BuscarPaginaComRetryAsync(1, token);
            if (primeiraResposta == null)
            {
                resultado.Sucesso = false;
                resultado.Mensagem = "Erro ao conectar com a API";
                return resultado;
            }

            _totalPaginas = primeiraResposta.RootElement.GetProperty("totalPaginas").GetInt32();
            int totalRegistros = primeiraResposta.RootElement.GetProperty("totalRegistros").GetInt32();

            if (totalRegistros == 0)
            {
                resultado.Sucesso = true;
                resultado.Mensagem = "Nenhum registro encontrado";
                return resultado;
            }

            resultado.TotalPaginas = _totalPaginas;
            _logger.LogInformation("Total: {TotalRegistros} registros em {TotalPaginas} páginas", totalRegistros, _totalPaginas);

            // Processar primeira página
            var (itens, atasNovas, itensNovos) = await ProcessarPaginaAsync(primeiraResposta, token);
            Interlocked.Add(ref _itensProcessados, itens);
            Interlocked.Increment(ref _paginasProcessadas);
            resultado.ItensProcessados += itens;
            resultado.AtasNovas += atasNovas;
            resultado.ItensNovos += itensNovos;
            resultado.PaginasProcessadas++;

            // Semáforo para controlar concorrência máxima
            using var concurrencySemaphore = new SemaphoreSlim(config.MaxConcorrencia, config.MaxConcorrencia);
            var tasks = new List<Task<(int pagina, int itens, int atasNovas, int itensNovos, bool sucesso)>>();

            // Disparar requisições com intervalo fixo
            for (int pagina = 2; pagina <= _totalPaginas; pagina++)
            {
                if (token.IsCancellationRequested) break;

                var paginaAtual = pagina;

                // Aguardar slot disponível
                await concurrencySemaphore.WaitAsync(token);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        return await ProcessarPaginaParalelaAsync(paginaAtual, concurrencySemaphore, token);
                    }
                    catch (OperationCanceledException)
                    {
                        concurrencySemaphore.Release();
                        return (paginaAtual, 0, 0, 0, false);
                    }
                    catch
                    {
                        concurrencySemaphore.Release();
                        throw;
                    }
                }, token);

                tasks.Add(task);

                // Intervalo entre disparos (não espera a resposta)
                if (pagina < _totalPaginas && !token.IsCancellationRequested)
                {
                    try { await Task.Delay(config.IntervaloEntreRequisicoesMs, token); }
                    catch (OperationCanceledException) { break; }
                }
            }

            // Aguardar todas as tasks terminarem
            _logger.LogInformation("Aguardando {Count} requisições em paralelo terminarem...", tasks.Count);
            try
            {
                var results = await Task.WhenAll(tasks);

                // Consolidar resultados
                foreach (var (pagina, itensProc, atasN, itensN, sucesso) in results)
                {
                    resultado.ItensProcessados += itensProc;
                    resultado.AtasNovas += atasN;
                    resultado.ItensNovos += itensN;
                    resultado.PaginasProcessadas++;

                    if (!sucesso)
                    {
                        resultado.Erros++;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Sincronização paralela cancelada pelo usuário");
            }

            if (!token.IsCancellationRequested)
            {
                resultado.Sucesso = resultado.Erros == 0;
                resultado.Mensagem = resultado.Sucesso
                    ? $"Sincronização paralela concluída: {resultado.ItensProcessados} itens ({resultado.ItensNovos} novos)"
                    : $"Sincronização concluída com {resultado.Erros} erro(s) em {_paginasComErro} página(s)";
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
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _syncSemaphore.Release();
        }
    }

    private async Task<(int pagina, int itens, int atasNovas, int itensNovos, bool sucesso)> ProcessarPaginaParalelaAsync(
        int pagina, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        try
        {
            var json = await BuscarPaginaComRetryAsync(pagina, cancellationToken);
            if (json != null)
            {
                // Criar novo escopo de DbContext para thread safety
                var (itens, atasNovas, itensNovos) = await ProcessarPaginaAsync(json, cancellationToken);
                Interlocked.Add(ref _itensProcessados, itens);
                Interlocked.Increment(ref _paginasProcessadas);
                _logger.LogInformation("Página {Pagina} processada: {Itens} itens", pagina, itens);
                return (pagina, itens, atasNovas, itensNovos, true);
            }
            else
            {
                Interlocked.Increment(ref _paginasComErro);
                Interlocked.Increment(ref _paginasProcessadas);
                lock (_lockPaginasFalhadas) { _paginasFalhadas.Add(pagina); }
                _logger.LogWarning("Página {Pagina} falhou após tentativas", pagina);
                return (pagina, 0, 0, 0, false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar página {Pagina}", pagina);
            Interlocked.Increment(ref _paginasComErro);
            Interlocked.Increment(ref _paginasProcessadas);
            lock (_lockPaginasFalhadas) { _paginasFalhadas.Add(pagina); }
            return (pagina, 0, 0, 0, false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Continuar sincronização - reprocessa apenas as páginas que falharam
    /// </summary>
    public async Task<SincronizacaoResult> ContinuarSincronizacaoAsync(CancellationToken cancellationToken)
    {
        List<int> paginasParaProcessar;
        lock (_lockPaginasFalhadas)
        {
            // Ordenar do menor para o maior número de página
            paginasParaProcessar = _paginasFalhadas.OrderBy(p => p).ToList();
        }

        if (paginasParaProcessar.Count == 0)
        {
            return new SincronizacaoResult
            {
                Sucesso = true,
                Mensagem = "Nenhuma página pendente para reprocessar"
            };
        }

        if (_sincronizacaoEmAndamento)
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem = "Sincronização já em andamento"
            };
        }

        bool acquired = await _syncSemaphore.WaitAsync(0, cancellationToken);
        if (!acquired)
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem = "Sincronização já em andamento"
            };
        }

        try
        {
            _sincronizacaoEmAndamento = true;

            // Criar token combinado para permitir cancelamento
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;

            var resultado = new SincronizacaoResult();
            resultado.TotalPaginas = paginasParaProcessar.Count;

            _logger.LogInformation("Continuando sincronização: {Count} páginas para reprocessar: {Paginas}",
                paginasParaProcessar.Count, string.Join(", ", paginasParaProcessar));

            var paginasSucesso = new List<int>();

            foreach (var pagina in paginasParaProcessar)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    var json = await BuscarPaginaComRetryAsync(pagina, token);
                    if (json != null)
                    {
                        var (itens, atasNovas, itensNovos) = await ProcessarPaginaAsync(json, token);
                        resultado.ItensProcessados += itens;
                        resultado.AtasNovas += atasNovas;
                        resultado.ItensNovos += itensNovos;
                        resultado.PaginasProcessadas++;
                        paginasSucesso.Add(pagina);
                        _itensProcessados += itens;
                        _logger.LogInformation("Página {Pagina} reprocessada com sucesso: {Itens} itens", pagina, itens);
                    }
                    else
                    {
                        resultado.Erros++;
                        resultado.PaginasProcessadas++;
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Reprocessamento cancelado pelo usuário na página {Pagina}", pagina);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao reprocessar página {Pagina}", pagina);
                    resultado.Erros++;
                    resultado.PaginasProcessadas++;
                }

                if (!token.IsCancellationRequested)
                {
                    try { await Task.Delay(INTERVALO_ENTRE_PAGINAS_MS, token); }
                    catch (OperationCanceledException) { break; }
                }
            }

            // Remover páginas que foram processadas com sucesso
            lock (_lockPaginasFalhadas)
            {
                foreach (var pagina in paginasSucesso)
                {
                    _paginasFalhadas.Remove(pagina);
                }
                _paginasComErro = _paginasFalhadas.Count;
            }

            if (!token.IsCancellationRequested)
            {
                resultado.Sucesso = resultado.Erros == 0;
                resultado.Mensagem = resultado.Sucesso
                    ? $"Reprocessamento concluído: {resultado.ItensProcessados} itens ({resultado.ItensNovos} novos)"
                    : $"Reprocessamento concluído com {resultado.Erros} erro(s). {_paginasFalhadas.Count} página(s) ainda pendente(s)";
            }
            else
            {
                resultado.Sucesso = false;
                resultado.Mensagem = $"Reprocessamento cancelado. {resultado.ItensProcessados} itens processados antes da parada.";
            }

            if (resultado.ItensProcessados > 0)
            {
                await RegistrarUltimaAtualizacaoAsync(CancellationToken.None);
            }

            return resultado;
        }
        finally
        {
            _sincronizacaoEmAndamento = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _syncSemaphore.Release();
        }
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

    private async Task<JsonDocument?> BuscarPaginaComRetryAsync(int pagina, CancellationToken cancellationToken, string? dataMin = null, string? dataMax = null)
    {
        var vigenciaMin = dataMin ?? DATA_VIGENCIA_MIN;
        var vigenciaMax = dataMax ?? DATA_VIGENCIA_MAX;

        for (int tentativa = 1; tentativa <= MAX_TENTATIVAS; tentativa++)
        {
            try
            {
                var url = $"https://dadosabertos.compras.gov.br/modulo-arp/2_consultarARPItem?" +
                          $"pagina={pagina}&tamanhoPagina={ITENS_POR_PAGINA}" +
                          $"&codigoUnidadeGerenciadora={CODIGO_UNIDADE}" +
                          $"&dataVigenciaInicialMin={vigenciaMin}&dataVigenciaInicialMax={vigenciaMax}";

                _logger.LogInformation("Buscando página {Pagina} (tentativa {Tentativa}/{MaxTentativas})", pagina, tentativa, MAX_TENTATIVAS);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(45));

                var response = await _httpClient.GetAsync(url, timeoutCts.Token);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                _logger.LogInformation("Página {Pagina} recebida com sucesso", pagina);
                return JsonDocument.Parse(content);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout na tentativa {Tentativa} da página {Pagina}", tentativa, pagina);

                if (tentativa < MAX_TENTATIVAS)
                {
                    _logger.LogInformation("Aguardando {Segundos}s antes de tentar novamente...", TEMPO_ESPERA_RETRY_MS / 1000);
                    await Task.Delay(TEMPO_ESPERA_RETRY_MS, cancellationToken);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Erro HTTP na tentativa {Tentativa} da página {Pagina}: {Mensagem}", tentativa, pagina, ex.Message);

                if (tentativa < MAX_TENTATIVAS)
                {
                    await Task.Delay(TEMPO_ESPERA_RETRY_MS, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro na tentativa {Tentativa} da página {Pagina}", tentativa, pagina);

                if (tentativa < MAX_TENTATIVAS)
                {
                    await Task.Delay(TEMPO_ESPERA_RETRY_MS, cancellationToken);
                }
            }
        }

        _logger.LogError("Falha ao buscar página {Pagina} após {MaxTentativas} tentativas", pagina, MAX_TENTATIVAS);
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

        // Primeiro, garantir que a Ata existe
        var numeroControlePncpAta = GetStringOrNull(itemJson, "numeroControlePncpAta");
        if (string.IsNullOrEmpty(numeroControlePncpAta))
        {
            _logger.LogWarning("Item sem numeroControlePncpAta, ignorando");
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

        // Processar o Item (material ou serviço)
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
                // Se o item existia sem descrição principal, atualiza com a descrição atual
                item.DescricaoPrincipal = descricaoItem;
                await _db.SaveChangesAsync(cancellationToken);
            }

            // Adicionar descrição na tabela de descrições se não existir
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

            // Criar/Atualizar relação Ata-Item
            var ataItem = await _db.AtaItens
                .FirstOrDefaultAsync(ai => ai.AtaId == ata.Id && ai.CodigoItem == codigoItem.Value, cancellationToken);

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
                ataItem.NumeroItem = GetStringOrNull(itemJson, "numeroItem");
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
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetInt32();
            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var val))
                return val;
        }
        return null;
    }

    private static decimal? GetDecimalOrNull(JsonElement json, string propertyName)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDecimal();
            if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var val))
                return val;
        }
        return null;
    }

    private static DateTime? GetDateOrNull(JsonElement json, string propertyName)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
        {
            var dateStr = prop.GetString();
            if (DateTime.TryParse(dateStr, out var date))
                return date;
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

    public async Task<SincronizacaoStatus> ObterStatusAsync(CancellationToken cancellationToken)
    {
        DateTime? ultimaAtualizacao = null;
        var config = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "ultima_sincronizacao", cancellationToken);
        if (config != null && DateTime.TryParse(config.Valor, out var dt))
        {
            ultimaAtualizacao = dt;
        }

        // Buscar data da última ata e total de atas
        var dataUltimaAta = await _db.Atas.MaxAsync(a => (DateTime?)a.DataVigenciaInicial, cancellationToken);
        var totalAtas = await _db.Atas.CountAsync(cancellationToken);

        List<int> paginasFalhadas;
        lock (_lockPaginasFalhadas)
        {
            // Ordenar do menor para o maior número de página
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
            PaginasFalhadas = paginasFalhadas
        };
    }

    /// <summary>
    /// Sincronização incremental - busca apenas atas novas a partir da última data de vigência inicial
    /// </summary>
    public async Task<SincronizacaoResult> SincronizarIncrementalAsync(CancellationToken cancellationToken)
    {
        // Buscar a data mais recente de vigência inicial
        var ultimaData = await _db.Atas.MaxAsync(a => (DateTime?)a.DataVigenciaInicial, cancellationToken);

        // Se não tem atas, fazer sync completo
        if (ultimaData == null)
        {
            _logger.LogInformation("Nenhuma ata encontrada. Iniciando sincronização completa.");
            return await SincronizarAsync(cancellationToken);
        }

        // Usar a data encontrada -1 dia como margem de segurança até 1 ano no futuro
        var dataInicio = ultimaData.Value.AddDays(-1).ToString("yyyy-MM-dd");
        var dataFim = DateTime.Today.AddDays(365).ToString("yyyy-MM-dd");

        _logger.LogInformation("Iniciando sincronização incremental de {DataInicio} até {DataFim}", dataInicio, dataFim);

        return await SincronizarComDatasAsync(dataInicio, dataFim, cancellationToken);
    }

    /// <summary>
    /// Sincronização com intervalo de datas específico
    /// </summary>
    private async Task<SincronizacaoResult> SincronizarComDatasAsync(string dataMin, string dataMax, CancellationToken cancellationToken)
    {
        if (_sincronizacaoEmAndamento)
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem = "Sincronização já em andamento"
            };
        }

        bool acquired = await _syncSemaphore.WaitAsync(0, cancellationToken);
        if (!acquired)
        {
            return new SincronizacaoResult
            {
                Sucesso = false,
                Mensagem = "Sincronização já em andamento"
            };
        }

        try
        {
            _sincronizacaoEmAndamento = true;
            _paginasProcessadas = 0;
            _totalPaginas = 0;
            _itensProcessados = 0;
            _paginasComErro = 0;

            var resultado = new SincronizacaoResult();

            _logger.LogInformation("Sincronizando atas de {DataMin} até {DataMax}", dataMin, dataMax);

            // Buscar primeira página para saber total
            var primeiraResposta = await BuscarPaginaComRetryAsync(1, cancellationToken, dataMin, dataMax);
            if (primeiraResposta == null)
            {
                resultado.Sucesso = false;
                resultado.Mensagem = "Erro ao conectar com a API";
                return resultado;
            }

            _totalPaginas = primeiraResposta.RootElement.GetProperty("totalPaginas").GetInt32();
            int totalRegistros = primeiraResposta.RootElement.GetProperty("totalRegistros").GetInt32();

            if (totalRegistros == 0)
            {
                resultado.Sucesso = true;
                resultado.Mensagem = "Sistema já está atualizado. Nenhuma nova ata encontrada.";
                return resultado;
            }

            resultado.TotalPaginas = _totalPaginas;
            _logger.LogInformation("Encontrados {TotalRegistros} registros em {TotalPaginas} páginas", totalRegistros, _totalPaginas);

            // Processar primeira página
            var (itens, atasNovas, itensNovos) = await ProcessarPaginaAsync(primeiraResposta, cancellationToken);
            resultado.ItensProcessados += itens;
            resultado.AtasNovas += atasNovas;
            resultado.ItensNovos += itensNovos;
            _itensProcessados += itens;
            _paginasProcessadas++;
            resultado.PaginasProcessadas++;

            // Processar páginas restantes
            for (int pagina = 2; pagina <= _totalPaginas; pagina++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var json = await BuscarPaginaComRetryAsync(pagina, cancellationToken, dataMin, dataMax);
                    if (json != null)
                    {
                        (itens, atasNovas, itensNovos) = await ProcessarPaginaAsync(json, cancellationToken);
                        resultado.ItensProcessados += itens;
                        resultado.AtasNovas += atasNovas;
                        resultado.ItensNovos += itensNovos;
                        _itensProcessados += itens;
                    }
                    else
                    {
                        resultado.Erros++;
                        _paginasComErro++;
                        _logger.LogWarning("Página {Pagina} ignorada após falhas", pagina);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar página {Pagina}", pagina);
                    resultado.Erros++;
                    _paginasComErro++;
                }

                // Sempre atualiza o progresso (mesmo se falhou)
                _paginasProcessadas++;
                resultado.PaginasProcessadas++;

                // Intervalo entre páginas
                await Task.Delay(INTERVALO_ENTRE_PAGINAS_MS, cancellationToken);
            }

            resultado.Sucesso = resultado.Erros == 0;
            resultado.Mensagem = resultado.Sucesso
                ? $"Atualização concluída: {resultado.ItensProcessados} itens processados ({resultado.ItensNovos} novos)"
                : $"Atualização concluída com {resultado.Erros} erro(s) em {_paginasComErro} página(s)";

            // Registrar última atualização
            await RegistrarUltimaAtualizacaoAsync(cancellationToken);

            return resultado;
        }
        finally
        {
            _sincronizacaoEmAndamento = false;
            _syncSemaphore.Release();
        }
    }

    public async Task LimparLogsAsync(CancellationToken cancellationToken)
    {
        await _db.SincronizacaoLogs.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> ResetarPaginasTravadasAsync(CancellationToken cancellationToken)
    {
        // Não tem mais páginas travadas nesse modelo
        return 0;
    }
}
