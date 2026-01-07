import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Sparkles, Calendar } from 'lucide-react';
import { atasApi } from '../services/api';
import { AtaCard } from '../components/AtaCard';
import { Loading } from '../components/Loading';
import { EmptyState } from '../components/EmptyState';
import type { AtaResumo } from '../types';

const PERIODO_OPTIONS = [
  { value: 7, label: 'Últimos 7 dias' },
  { value: 15, label: 'Últimos 15 dias' },
  { value: 30, label: 'Últimos 30 dias' },
  { value: 60, label: 'Últimos 60 dias' },
  { value: 90, label: 'Últimos 90 dias' },
] as const;

export function NovasAtas() {
  const navigate = useNavigate();
  const [atas, setAtas] = useState<AtaResumo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [periodo, setPeriodo] = useState(30);

  const loadAtas = async () => {
    try {
      setLoading(true);
      setError(false);
      const data = await atasApi.novas(periodo);
      setAtas(data);
    } catch (err) {
      console.error('Erro ao carregar atas novas:', err);
      setError(true);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAtas();
  }, [periodo]);

  return (
    <div className="p-4 max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-lg bg-amber-100">
            <Sparkles className="text-amber-600" size={24} />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Novas Atas</h1>
            <p className="text-gray-500 text-sm">Atas registradas recentemente</p>
          </div>
        </div>

        {/* Seletor de período */}
        <div className="flex items-center gap-2">
          <Calendar size={18} className="text-gray-400" />
          <select
            value={periodo}
            onChange={(e) => setPeriodo(Number(e.target.value))}
            className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#00508C] focus:border-transparent text-sm"
          >
            {PERIODO_OPTIONS.map(opt => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        </div>
      </div>

      {/* Contagem */}
      {!loading && !error && atas.length > 0 && (
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-3">
          <p className="text-sm text-amber-800">
            <span className="font-semibold">{atas.length}</span> {atas.length === 1 ? 'ata nova' : 'atas novas'} nos últimos {periodo} dias
          </p>
        </div>
      )}

      {/* Lista de atas */}
      {loading ? (
        <Loading message="Carregando atas novas..." />
      ) : error ? (
        <EmptyState
          type="error"
          title="Ops! Algo deu errado"
          description="Não foi possível conectar ao servidor. Tente novamente mais tarde."
          onRetry={loadAtas}
        />
      ) : atas.length === 0 ? (
        <EmptyState
          type="no-data"
          title="Nenhuma ata nova"
          description={`Não há atas registradas nos últimos ${periodo} dias`}
        />
      ) : (
        <div className="space-y-3">
          {atas.map((ata) => (
            <div
              key={ata.id}
              onClick={() => navigate(`/ata/${ata.id}`)}
              className="cursor-pointer"
            >
              <AtaCard ata={ata} showItensPreview />
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
