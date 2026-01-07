import { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { AlertTriangle, Clock, AlertCircle, FileX } from 'lucide-react';
import { atasApi, itensApi } from '../services/api';
import { Card, CardHeader, CardContent } from '../components/Card';
import { AtaCard } from '../components/AtaCard';
import { Loading } from '../components/Loading';
import { EmptyState } from '../components/EmptyState';
import type { AtaResumo, ItemSemAta } from '../types';

type Tab = 'criticas' | 'alerta' | 'atencao' | 'encerradas' | 'sem-ata';

const tabs: { id: Tab; label: string; icon: typeof AlertTriangle }[] = [
  { id: 'criticas', label: 'Críticas', icon: AlertTriangle },
  { id: 'alerta', label: 'Alerta', icon: AlertCircle },
  { id: 'atencao', label: 'Atenção', icon: Clock },
  { id: 'encerradas', label: 'Encerradas', icon: FileX },
  { id: 'sem-ata', label: 'Sem Ata', icon: FileX },
];

export function Alertas() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const initialTab = (searchParams.get('tab') as Tab) ||
    (searchParams.get('status')?.toLowerCase() as Tab) || 'criticas';

  const [activeTab, setActiveTab] = useState<Tab>(initialTab);
  const [atas, setAtas] = useState<AtaResumo[]>([]);
  const [itensSemAta, setItensSemAta] = useState<ItemSemAta[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  const loadData = async () => {
    setLoading(true);
    setError(false);
    try {
      if (activeTab === 'sem-ata') {
        const data = await itensApi.semAta(50);
        setItensSemAta(data);
      } else if (activeTab === 'encerradas') {
        const data = await atasApi.recemEncerradas(15);
        setAtas(data);
      } else {
        const statusMap: Record<string, string> = {
          criticas: 'Critico',
          alerta: 'Alerta',
          atencao: 'Atencao',
        };
        const data = await atasApi.vigentes(statusMap[activeTab], 100);
        setAtas(data);
      }
    } catch (err) {
      console.error('Erro ao carregar dados:', err);
      setError(true);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [activeTab]);

  const formatDate = (dateStr: string | null) => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('pt-BR');
  };

  return (
    <div className="p-4 max-w-4xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Alertas</h1>
        <p className="text-gray-500">Monitoramento de atas e itens</p>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 overflow-x-auto pb-2 -mx-4 px-4 sm:mx-0 sm:px-0">
        {tabs.map(({ id, label, icon: Icon }) => (
          <button
            key={id}
            onClick={() => setActiveTab(id)}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium text-sm whitespace-nowrap transition-colors ${
              activeTab === id
                ? 'bg-[#00508C] text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            <Icon size={16} />
            {label}
          </button>
        ))}
      </div>

      {/* Content */}
      {loading ? (
        <Loading message="Carregando..." />
      ) : error ? (
        <EmptyState
          type="error"
          title="Ops! Algo deu errado"
          description="Não foi possível conectar ao servidor. Tente novamente mais tarde."
          onRetry={loadData}
        />
      ) : activeTab === 'sem-ata' ? (
        itensSemAta.length === 0 ? (
          <EmptyState
            type="no-items"
            title="Nenhum item sem ata"
            description="Todos os itens possuem ata vigente"
          />
        ) : (
          <Card>
            <CardHeader>
              <h2 className="font-semibold text-gray-900">
                Itens sem Ata Vigente ({itensSemAta.length})
              </h2>
            </CardHeader>
            <CardContent className="p-0">
              <div className="divide-y divide-gray-100">
                {itensSemAta.map((item) => (
                  <div
                    key={item.codigoItem}
                    className="p-4 hover:bg-gray-50 cursor-pointer transition-colors"
                    onClick={() => navigate(`/item/${item.codigoItem}`)}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-xs font-medium px-2 py-0.5 rounded bg-gray-100 text-gray-600">
                            {item.tipoItem}
                          </span>
                          <span className="text-xs text-gray-400">#{item.codigoItem}</span>
                        </div>
                        <p className="font-medium text-gray-900">
                          {item.descricaoPrincipal || 'Sem descrição'}
                        </p>
                      </div>
                      <div className="text-right shrink-0">
                        <p className="text-xs text-gray-500">Última ata</p>
                        <p className="text-sm font-medium text-red-600">
                          {formatDate(item.ultimaAtaVencida)}
                        </p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )
      ) : atas.length === 0 ? (
        <EmptyState
          type="no-data"
          title={`Nenhuma ata ${activeTab === 'encerradas' ? 'encerrada recentemente' : 'nesta categoria'}`}
        />
      ) : (
        <div className="space-y-3">
          {atas.map((ata) => (
            <AtaCard key={ata.id} ata={ata} />
          ))}
        </div>
      )}
    </div>
  );
}
