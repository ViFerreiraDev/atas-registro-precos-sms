import { useState, useEffect, useCallback } from 'react';
import { Settings, RefreshCw, AlertCircle, CheckCircle2, Clock, Trash2, Calendar, FileText, Download, Zap, Play, StopCircle } from 'lucide-react';
import { sincronizacaoApi } from '../services/api';
import { Card, CardHeader, CardContent } from '../components/Card';
import { Loading } from '../components/Loading';
import { useModal } from '../components/Modal';
import type { SincronizacaoStatus, SincronizacaoResult } from '../types';

export function Configuracoes() {
  const [status, setStatus] = useState<SincronizacaoStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [syncing, setSyncing] = useState(false);
  const [resultado, setResultado] = useState<SincronizacaoResult | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  const { showSuccess, showError, showConfirm, Modal } = useModal();

  const carregarStatus = useCallback(async () => {
    setLoadError(false);
    try {
      const data = await sincronizacaoApi.status();
      setStatus(data);

      if (data.emAndamento) {
        setSyncing(true);
      } else {
        setSyncing(false);
      }
    } catch (err) {
      console.error('Erro ao carregar status:', err);
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    carregarStatus();
  }, [carregarStatus]);

  // Polling enquanto sincronizacao estiver em andamento
  useEffect(() => {
    if (!syncing) return;

    const interval = setInterval(() => {
      carregarStatus();
    }, 1500);

    return () => clearInterval(interval);
  }, [syncing, carregarStatus]);

  // Buscar novas atas (incremental)
  const handleAtualizar = async () => {
    if (syncing) return;

    setSyncing(true);
    setResultado(null);
    setErro(null);

    try {
      const result = await sincronizacaoApi.atualizar();
      setResultado(result);
      carregarStatus();
    } catch (err: any) {
      setErro(err.response?.data?.message || 'Erro ao atualizar. Verifique o console.');
      console.error('Erro na atualizacao:', err);
    } finally {
      setSyncing(false);
    }
  };

  // Sincronizacao completa
  const handleSincronizarCompleto = () => {
    showConfirm(
      'Sincronizacao Completa',
      'Esta operacao vai reprocessar TODAS as atas desde 2000. Isso pode demorar varios minutos. Use apenas se precisar reconstruir a base. Deseja continuar?',
      async () => {
        setSyncing(true);
        setResultado(null);
        setErro(null);

        try {
          const result = await sincronizacaoApi.sincronizar();
          setResultado(result);
          carregarStatus();
        } catch (err: any) {
          setErro(err.response?.data?.message || 'Erro ao sincronizar. Verifique o console.');
          console.error('Erro na sincronizacao:', err);
        } finally {
          setSyncing(false);
        }
      },
      'warning'
    );
  };

  const handleLimparDados = () => {
    showConfirm(
      'Limpar Todos os Dados',
      'ATENCAO: Isso vai apagar TODAS as atas, itens e descricoes. Esta acao nao pode ser desfeita. Deseja continuar?',
      async () => {
        try {
          const result = await sincronizacaoApi.resetarDados();
          showSuccess('Dados Limpos', result.message);
          carregarStatus();
        } catch (err) {
          console.error('Erro ao limpar dados:', err);
          showError('Erro', 'Nao foi possivel limpar os dados. Tente novamente.');
        }
      },
      'error'
    );
  };

  // Sincronizacao paralela (mais rapida)
  const handleSincronizarParalelo = () => {
    showConfirm(
      'Sincronizacao Paralela',
      'Esta opcao dispara multiplas requisicoes em paralelo (1 por segundo). E mais rapida mas consome mais recursos. Deseja continuar?',
      async () => {
        setSyncing(true);
        setResultado(null);
        setErro(null);

        try {
          const result = await sincronizacaoApi.sincronizarParalelo({
            modoParalelo: true,
            intervaloEntreRequisicoesMs: 1000,
            maxConcorrencia: 10
          });
          setResultado(result);
          carregarStatus();
        } catch (err: any) {
          setErro(err.response?.data?.message || 'Erro na sincronizacao paralela.');
          console.error('Erro na sincronizacao paralela:', err);
        } finally {
          setSyncing(false);
        }
      },
      'warning'
    );
  };

  // Continuar sincronizacao (reprocessar paginas falhadas)
  const handleContinuar = async () => {
    if (syncing) return;

    setSyncing(true);
    setResultado(null);
    setErro(null);

    try {
      const result = await sincronizacaoApi.continuar();
      setResultado(result);
      carregarStatus();
    } catch (err: any) {
      setErro(err.response?.data?.message || 'Erro ao continuar sincronizacao.');
      console.error('Erro ao continuar:', err);
    } finally {
      setSyncing(false);
    }
  };

  // Parar sincronizacao
  const handleParar = async () => {
    try {
      const result = await sincronizacaoApi.parar();
      showSuccess('Sincronizacao Parada', result.message);
      carregarStatus();
    } catch (err: any) {
      if (err.response?.status === 400) {
        showError('Aviso', err.response?.data?.message || 'Nenhuma sincronizacao em andamento.');
      } else {
        showError('Erro', 'Nao foi possivel parar a sincronizacao.');
      }
    }
  };

  const formatDate = (dateStr: string | null) => {
    if (!dateStr) return 'Nunca';
    return new Date(dateStr).toLocaleString('pt-BR');
  };

  const formatDateShort = (dateStr: string | null) => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('pt-BR');
  };

  const getProgressPercent = () => {
    if (!status || status.totalPaginas === 0) return 0;
    // Usar paginasSucesso para mostrar progresso real (páginas processadas com sucesso)
    return Math.round((status.paginasSucesso / status.totalPaginas) * 100);
  };

  if (loading) {
    return <Loading message="Carregando configuracoes..." />;
  }

  if (loadError) {
    return (
      <div className="p-4 max-w-4xl mx-auto">
        <Card>
          <CardContent>
            <div className="flex flex-col items-center justify-center py-12 px-4 text-center">
              <div className="w-16 h-16 rounded-full bg-orange-50 flex items-center justify-center mb-4">
                <AlertCircle className="text-orange-500" size={32} />
              </div>
              <h3 className="text-lg font-semibold text-gray-900 mb-2">Ops! Algo deu errado</h3>
              <p className="text-gray-500 mb-4">Não foi possível conectar ao servidor. Tente novamente mais tarde.</p>
              <button
                onClick={carregarStatus}
                className="flex items-center gap-2 px-4 py-2.5 bg-[#00508C] text-white rounded-xl hover:bg-[#003d6b] transition-colors"
              >
                <RefreshCw size={18} />
                Tentar novamente
              </button>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="p-4 max-w-4xl mx-auto space-y-6">
      <Modal />

      {/* Header */}
      <div className="flex items-center gap-3">
        <div className="p-2 rounded-lg bg-gray-100">
          <Settings className="text-gray-600" size={24} />
        </div>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Configuracoes</h1>
          <p className="text-gray-500 text-sm">Gerenciamento de sincronizacao e sistema</p>
        </div>
      </div>

      {/* Status da Sincronizacao */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h2 className="font-semibold text-gray-900">Sincronizacao com PNCP</h2>
            {syncing && (
              <span className="flex items-center gap-2 text-sm text-[#00508C]">
                <RefreshCw className="animate-spin" size={16} />
                Em andamento...
              </span>
            )}
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Informacoes fixas */}
          <div className="p-3 bg-gray-50 rounded-lg">
            <p className="text-sm text-gray-600">
              <span className="font-medium text-gray-700">Fonte:</span> dadosabertos.compras.gov.br
            </p>
          </div>

          {/* Status atual - Grid com mais informacoes */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <div className="bg-gray-50 rounded-lg p-3">
              <p className="text-xs text-gray-500 mb-1">Status</p>
              <div className="flex items-center gap-2">
                {syncing ? (
                  <>
                    <RefreshCw className="text-[#00508C] animate-spin" size={18} />
                    <span className="font-medium text-[#00508C]">Sincronizando</span>
                  </>
                ) : status?.ultimaAtualizacao ? (
                  <>
                    <CheckCircle2 className="text-green-600" size={18} />
                    <span className="font-medium text-green-600">Atualizado</span>
                  </>
                ) : (
                  <>
                    <Clock className="text-yellow-600" size={18} />
                    <span className="font-medium text-yellow-600">Aguardando</span>
                  </>
                )}
              </div>
            </div>

            <div className="bg-gray-50 rounded-lg p-3">
              <p className="text-xs text-gray-500 mb-1 flex items-center gap-1">
                <Calendar size={12} />
                Ultima Ata
              </p>
              <p className="font-medium text-gray-900">
                {formatDateShort(status?.dataUltimaAta || null)}
              </p>
            </div>

            <div className="bg-gray-50 rounded-lg p-3">
              <p className="text-xs text-gray-500 mb-1 flex items-center gap-1">
                <FileText size={12} />
                Total de Atas
              </p>
              <p className="font-medium text-gray-900">
                {status?.totalAtas?.toLocaleString('pt-BR') || 0}
              </p>
            </div>

            <div className="bg-gray-50 rounded-lg p-3">
              <p className="text-xs text-gray-500 mb-1">Ultima Atualizacao</p>
              <p className="font-medium text-gray-900 text-sm">
                {formatDate(status?.ultimaAtualizacao || null)}
              </p>
            </div>
          </div>

          {/* Barra de progresso (durante sincronizacao) */}
          {syncing && status && status.totalPaginas > 0 && (
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-gray-600">
                  Tentando pagina {status.paginasProcessadas} de {status.totalPaginas}
                  {status.paginasComErro > 0 && (
                    <span className="text-green-600 ml-2">
                      ({status.paginasSucesso} com sucesso)
                    </span>
                  )}
                </span>
                <span className="font-medium text-gray-900">{getProgressPercent()}%</span>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-4">
                <div
                  className="h-4 rounded-full bg-[#00508C] transition-all duration-500 flex items-center justify-center"
                  style={{ width: `${Math.max(getProgressPercent(), 5)}%` }}
                >
                  {getProgressPercent() > 10 && (
                    <span className="text-xs text-white font-medium">{getProgressPercent()}%</span>
                  )}
                </div>
              </div>
              <div className="flex justify-between text-xs text-gray-500">
                <span>{status.totalItensProcessados.toLocaleString('pt-BR')} itens processados</span>
                <span>{status.paginasPendentes} paginas restantes</span>
              </div>
              <button
                onClick={handleParar}
                className="w-full flex items-center justify-center gap-2 px-3 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors text-sm font-medium mt-2"
              >
                <StopCircle size={16} />
                Parar Sincronizacao
              </button>
            </div>
          )}

          {/* Alertas de erros e paginas falhadas */}
          {(status?.paginasComErro || 0) > 0 && (
            <div className="p-3 bg-red-50 border border-red-200 rounded-lg space-y-2">
              <div className="flex items-center gap-2">
                <AlertCircle className="text-red-600" size={18} />
                <span className="text-sm text-red-800">
                  {status?.paginasComErro} pagina(s) com erro durante o processamento
                </span>
              </div>
              {status?.paginasFalhadas && status.paginasFalhadas.length > 0 && (
                <div className="text-xs text-red-700">
                  Paginas: {status.paginasFalhadas.join(', ')}
                </div>
              )}
              <button
                onClick={handleContinuar}
                disabled={syncing}
                className="w-full flex items-center justify-center gap-2 px-3 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50 text-sm font-medium"
              >
                <Play size={16} />
                Continuar (reprocessar paginas falhadas)
              </button>
            </div>
          )}

          {/* Resultado da ultima sincronizacao */}
          {resultado && (
            <div className={`p-4 rounded-lg border ${resultado.sucesso ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200'}`}>
              <div className="flex items-center gap-2 mb-2">
                {resultado.sucesso ? (
                  <CheckCircle2 className="text-green-600" size={18} />
                ) : (
                  <AlertCircle className="text-red-600" size={18} />
                )}
                <span className={`font-medium ${resultado.sucesso ? 'text-green-800' : 'text-red-800'}`}>
                  {resultado.mensagem}
                </span>
              </div>
              {resultado.totalPaginas > 0 && (
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-sm">
                  <div>
                    <span className="text-gray-600">Paginas:</span>{' '}
                    <span className="font-medium">{resultado.paginasProcessadas} / {resultado.totalPaginas}</span>
                  </div>
                  <div>
                    <span className="text-gray-600">Itens:</span>{' '}
                    <span className="font-medium">{resultado.itensProcessados.toLocaleString('pt-BR')}</span>
                  </div>
                  <div>
                    <span className="text-gray-600">Novos:</span>{' '}
                    <span className="font-medium text-green-600">{resultado.itensNovos?.toLocaleString('pt-BR') || 0}</span>
                  </div>
                  <div>
                    <span className="text-gray-600">Erros:</span>{' '}
                    <span className={`font-medium ${resultado.erros > 0 ? 'text-red-600' : 'text-green-600'}`}>
                      {resultado.erros}
                    </span>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Erro */}
          {erro && (
            <div className="flex items-center gap-2 p-3 bg-red-50 border border-red-200 rounded-lg">
              <AlertCircle className="text-red-600" size={18} />
              <span className="text-sm text-red-800">{erro}</span>
            </div>
          )}

          {/* Botao principal - Buscar Novas Atas */}
          <button
            onClick={handleAtualizar}
            disabled={syncing}
            className="w-full flex items-center justify-center gap-2 px-4 py-3 bg-[#F7941D] text-white rounded-lg hover:bg-[#e08519] transition-colors disabled:opacity-50 disabled:cursor-not-allowed font-medium"
          >
            <Download className={syncing ? 'animate-pulse' : ''} size={20} />
            {syncing ? 'Buscando novas atas...' : 'Buscar Novas Atas'}
          </button>

          {status?.dataUltimaAta && !syncing && (
            <p className="text-xs text-gray-500 text-center -mt-2">
              Busca atas a partir de {formatDateShort(status.dataUltimaAta)}
            </p>
          )}

          {/* Botoes secundarios */}
          <div className="space-y-3 pt-2 border-t border-gray-100">
            {/* Sincronizacao paralela - destaque */}
            <button
              onClick={handleSincronizarParalelo}
              disabled={syncing}
              className="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium"
            >
              <Zap className={syncing ? 'animate-pulse' : ''} size={16} />
              Sincronizacao Paralela (mais rapida)
            </button>

            <div className="flex flex-col sm:flex-row gap-3">
              <button
                onClick={handleSincronizarCompleto}
                disabled={syncing}
                className="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm"
              >
                <RefreshCw className={syncing ? 'animate-spin' : ''} size={16} />
                Sincronizacao Sequencial
              </button>

              <button
                onClick={handleLimparDados}
                disabled={syncing}
                className="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 bg-red-50 text-red-700 rounded-lg hover:bg-red-100 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm"
              >
                <Trash2 size={16} />
                Limpar Todos os Dados
              </button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Informacoes do Sistema */}
      <Card>
        <CardHeader>
          <h2 className="font-semibold text-gray-900">Sobre o Sistema</h2>
        </CardHeader>
        <CardContent>
          <div className="space-y-2 text-sm text-gray-600">
            <p>
              <span className="font-medium text-gray-900">Sistema de Atas</span> - Gerenciamento de Registro de Precos
            </p>
            <p>
              Secretaria Municipal de Saude - Prefeitura da Cidade do Rio de Janeiro
            </p>
            <p className="mt-3">
              Desenvolvido pela <span className="font-medium text-gray-900">Equipe de Desenvolvimento da S/SUBG</span>
            </p>
            <p className="text-xs text-gray-400 mt-4">
              Dados sincronizados do Portal Nacional de Contratacoes Publicas (PNCP) via API dados abertos
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
