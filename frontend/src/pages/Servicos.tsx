import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Filter, X, ExternalLink, Wrench } from 'lucide-react';
import { itensApi } from '../services/api';
import { Card, CardHeader, CardContent } from '../components/Card';
import { Loading } from '../components/Loading';
import { EmptyState } from '../components/EmptyState';
import type { ItemAlerta, FiltrosItem } from '../types';

const STATUS_OPTIONS = [
  { value: '', label: 'Todos os status' },
  { value: 'Critico', label: 'Crítico (até 30 dias)' },
  { value: 'Alerta', label: 'Alerta (31-60 dias)' },
  { value: 'Atencao', label: 'Atenção (61-120 dias)' },
  { value: 'Vigente', label: 'OK (mais de 120 dias)' },
  { value: 'Vencida', label: 'Vencida' },
] as const;

const STATUS_COLORS: Record<string, string> = {
  Vencida: 'bg-gray-800 text-white',
  Critico: 'bg-red-100 text-red-700',
  Alerta: 'bg-orange-100 text-orange-700',
  Atencao: 'bg-yellow-100 text-yellow-700',
  Vigente: 'bg-green-100 text-green-700',
};

const STATUS_LABELS: Record<string, string> = {
  Vencida: 'Vencida',
  Critico: 'Crítico',
  Alerta: 'Alerta',
  Atencao: 'Atenção',
  Vigente: 'OK',
};

export function Servicos() {
  const navigate = useNavigate();
  const [itens, setItens] = useState<ItemAlerta[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  // Padrão: não mostrar todos (começa dos críticos)
  const [filtros, setFiltros] = useState<FiltrosItem>({
    todos: false,
    tipoItem: 'Serviço' // Fixo para serviços
  });
  const [buscaInput, setBuscaInput] = useState('');
  const [mostrarFiltros, setMostrarFiltros] = useState(false);

  const carregarItens = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      // Se tem busca, permite ver todos (inclusive vencidos)
      const filtrosParaBusca = filtros.busca
        ? { ...filtros, todos: true }
        : filtros;
      const data = await itensApi.porStatus(filtrosParaBusca);
      setItens(data);
    } catch (err) {
      console.error('Erro ao carregar itens:', err);
      setError(true);
    } finally {
      setLoading(false);
    }
  }, [filtros]);

  useEffect(() => {
    carregarItens();
  }, [carregarItens]);

  const handleBuscar = () => {
    if (buscaInput.length >= 3 || buscaInput.length === 0) {
      setFiltros(prev => ({ ...prev, busca: buscaInput || undefined }));
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleBuscar();
    }
  };

  const limparFiltros = () => {
    setFiltros({ todos: false, tipoItem: 'Serviço' });
    setBuscaInput('');
  };

  const temFiltrosAtivos =
    filtros.status ||
    filtros.busca ||
    filtros.diasMin !== undefined ||
    filtros.diasMax !== undefined ||
    filtros.todos;

  const formatDate = (dateStr: string | null) => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('pt-BR');
  };

  const getDiasColor = (dias: number) => {
    if (dias < 0) return 'text-gray-600';
    if (dias <= 30) return 'text-red-600';
    if (dias <= 60) return 'text-orange-600';
    if (dias <= 120) return 'text-yellow-600';
    return 'text-green-600';
  };

  return (
    <div className="max-w-4xl mx-auto">
      {/* Header + Filtros Fixos */}
      <div className="sticky top-16 z-40 bg-gray-50 pt-4 pb-2 px-4 shadow-sm">
        {/* Header */}
        <div className="flex items-center gap-3 mb-4">
          <div className="p-2 rounded-lg bg-purple-100">
            <Wrench className="text-purple-600" size={24} />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Serviços</h1>
            <p className="text-gray-500 text-sm">Pesquisa de serviços em atas</p>
          </div>
        </div>

        {/* Filtros */}
        <Card>
          <CardContent className="p-4 space-y-4">
            {/* Barra de busca */}
            <div className="flex flex-col sm:flex-row gap-2">
              <div className="flex-1 relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={18} />
                <input
                  type="text"
                  placeholder="Buscar por descrição (ex: manutenção predial)..."
                  value={buscaInput}
                  onChange={(e) => setBuscaInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00508C] focus:border-transparent"
                />
              </div>
              <div className="flex gap-2">
                <button
                  onClick={handleBuscar}
                  className="flex-1 sm:flex-initial px-4 py-2 bg-[#00508C] text-white rounded-lg hover:bg-[#003d6b] transition-colors"
                >
                  Buscar
                </button>
                <button
                  onClick={() => setMostrarFiltros(!mostrarFiltros)}
                  className={`flex-1 sm:flex-initial px-4 py-2 rounded-lg border transition-colors flex items-center justify-center gap-2 ${
                    mostrarFiltros || temFiltrosAtivos
                      ? 'bg-[#00508C] text-white border-[#00508C]'
                      : 'bg-white text-gray-600 border-gray-300 hover:bg-gray-50'
                  }`}
                >
                  <Filter size={18} />
                  <span className="sm:inline">Filtros</span>
                </button>
              </div>
            </div>

            {/* Filtros expandidos */}
            {mostrarFiltros && (
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-4 border-t border-gray-200">
                {/* Status */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Status
                  </label>
                  <select
                    value={filtros.status || ''}
                    onChange={(e) => setFiltros(prev => ({ ...prev, status: e.target.value || undefined }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00508C] focus:border-transparent"
                  >
                    {STATUS_OPTIONS.map(opt => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </select>
                </div>

                {/* Dias mínimos */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Dias mínimo p/ vencer
                  </label>
                  <input
                    type="number"
                    placeholder="Ex: 0"
                    value={filtros.diasMin ?? ''}
                    onChange={(e) => setFiltros(prev => ({
                      ...prev,
                      diasMin: e.target.value ? parseInt(e.target.value) : undefined
                    }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00508C] focus:border-transparent"
                  />
                </div>

                {/* Dias máximos */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Dias máximo p/ vencer
                  </label>
                  <input
                    type="number"
                    placeholder="Ex: 30"
                    value={filtros.diasMax ?? ''}
                    onChange={(e) => setFiltros(prev => ({
                      ...prev,
                      diasMax: e.target.value ? parseInt(e.target.value) : undefined
                    }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00508C] focus:border-transparent"
                  />
                </div>

                {/* Checkbox ver todos + Limpar */}
                <div className="sm:col-span-3 flex items-center justify-between pt-2">
                  <label className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={filtros.todos || false}
                      onChange={(e) => setFiltros(prev => ({ ...prev, todos: e.target.checked }))}
                      className="w-4 h-4 rounded border-gray-300 text-[#00508C] focus:ring-[#00508C]"
                    />
                    <span className="text-sm text-gray-600">
                      Incluir vencidos (sem limite)
                    </span>
                  </label>
                  {temFiltrosAtivos && (
                    <button
                      onClick={limparFiltros}
                      className="flex items-center gap-1 text-sm text-red-600 hover:text-red-700"
                    >
                      <X size={16} />
                      Limpar filtros
                    </button>
                  )}
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Lista de itens */}
      <div className="p-4 space-y-6">
        {loading ? (
          <Loading message="Carregando serviços..." />
        ) : error ? (
          <EmptyState
            type="error"
            title="Ops! Algo deu errado"
            description="Não foi possível conectar ao servidor. Tente novamente mais tarde."
            onRetry={carregarItens}
          />
        ) : itens.length === 0 ? (
          <EmptyState
            type="no-items"
            title="Nenhum serviço encontrado"
            description={temFiltrosAtivos ? "Tente ajustar os filtros" : "Não há serviços com ata"}
          />
        ) : (
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="font-semibold text-gray-900">
                    {itens.length} {itens.length === 1 ? 'serviço encontrado' : 'serviços encontrados'}
                  </h2>
                  <p className="text-sm text-gray-500">
                    Ordenado por data de vigência (mais próximos primeiro)
                  </p>
                </div>
              </div>
            </CardHeader>
            <CardContent className="p-0">
              <div className="divide-y divide-gray-100">
                {itens.map((item) => (
                  <div
                    key={item.codigoItem}
                    className="p-4 hover:bg-gray-50 cursor-pointer transition-colors"
                    onClick={() => navigate(`/item/${item.codigoItem}`)}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1 flex-wrap">
                          <span className={`text-xs font-medium px-2 py-0.5 rounded ${STATUS_COLORS[item.statusVigencia] || 'bg-gray-100 text-gray-600'}`}>
                            {STATUS_LABELS[item.statusVigencia] || item.statusVigencia}
                          </span>
                          <span className="text-xs text-gray-400">#{item.codigoItem}</span>
                        </div>
                        <p className="font-medium text-gray-900 line-clamp-2">
                          {item.descricaoPrincipal || 'Sem descrição'}
                        </p>
                        <p className="text-xs text-gray-500 mt-1">
                          Ata {item.numeroAtaVigente} • {item.fornecedor || 'Fornecedor não informado'}
                        </p>
                        {item.linkPncp && (
                          <a
                            href={item.linkPncp}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-1 text-xs text-[#00508C] hover:underline mt-1"
                            onClick={(e) => e.stopPropagation()}
                          >
                            <ExternalLink size={12} />
                            Baixar no PNCP
                          </a>
                        )}
                      </div>
                      <div className="text-right shrink-0">
                        <p className="text-xs text-gray-500">
                          {item.diasParaVencer < 0 ? 'Venceu em' : 'Vence em'}
                        </p>
                        <p className="text-sm font-medium text-gray-900">
                          {formatDate(item.dataVigenciaFinal)}
                        </p>
                        <p className={`text-xs font-medium ${getDiasColor(item.diasParaVencer)}`}>
                          {item.diasParaVencer < 0
                            ? `${Math.abs(item.diasParaVencer)} dias atrás`
                            : `${item.diasParaVencer} dias`
                          }
                        </p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
