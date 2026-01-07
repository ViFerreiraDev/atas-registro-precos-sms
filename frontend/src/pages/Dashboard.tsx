import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { FileText, AlertTriangle, AlertCircle, Clock, Package, PackageX } from 'lucide-react';
import { dashboardApi } from '../services/api';
import { StatCard } from '../components/StatCard';
import { Card, CardHeader, CardContent } from '../components/Card';
import { AtaCard } from '../components/AtaCard';
import { Loading } from '../components/Loading';
import { EmptyState } from '../components/EmptyState';
import type { Dashboard as DashboardData } from '../types';

export function Dashboard() {
  const navigate = useNavigate();
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const dashboard = await dashboardApi.obter();
      setData(dashboard);
    } catch (err) {
      setError('Erro ao carregar dados. Verifique se o backend está rodando.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  if (loading) {
    return <Loading message="Carregando dashboard..." />;
  }

  if (error) {
    return (
      <div className="p-4">
        <Card>
          <CardContent>
            <EmptyState
              type="error"
              title="Ops! Algo deu errado"
              description="Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente."
              onRetry={loadData}
            />
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!data) return null;

  return (
    <div className="p-4 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-500">Visão geral das atas de registro de preços</p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Atas Vigentes"
          value={data.totalAtasVigentes}
          icon={<FileText size={24} />}
          color="blue"
          onClick={() => navigate('/atas')}
        />
        <StatCard
          title="Críticas (30d)"
          value={data.atasCriticas}
          icon={<AlertTriangle size={24} />}
          color="red"
          onClick={() => navigate('/alertas?status=Critico')}
        />
        <StatCard
          title="Alerta (60d)"
          value={data.atasAlerta}
          icon={<AlertCircle size={24} />}
          color="yellow"
          onClick={() => navigate('/alertas?status=Alerta')}
        />
        <StatCard
          title="Atenção (120d)"
          value={data.atasAtencao}
          icon={<Clock size={24} />}
          color="cyan"
          onClick={() => navigate('/alertas?status=Atencao')}
        />
      </div>

      {/* Itens stats */}
      <div className="grid grid-cols-2 gap-4">
        <StatCard
          title="Itens com Ata"
          value={data.totalItensComAta}
          icon={<Package size={24} />}
          color="green"
          subtitle="Cobertos por ata vigente"
        />
        <StatCard
          title="Itens sem Ata"
          value={data.totalItensSemAta}
          icon={<PackageX size={24} />}
          color="gray"
          subtitle="Sem cobertura atual"
          onClick={() => navigate('/alertas?tab=sem-ata')}
        />
      </div>

      {/* Próximas a vencer */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h2 className="font-semibold text-gray-900">Próximas a Vencer</h2>
            <button
              onClick={() => navigate('/alertas')}
              className="text-sm text-[#00508C] hover:underline"
            >
              Ver todas →
            </button>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {data.proximasVencer.length === 0 ? (
            <EmptyState
              type="no-data"
              title="Nenhuma ata crítica"
              description="Não há atas vencendo nos próximos 30 dias"
            />
          ) : (
            <div className="divide-y divide-gray-100">
              {data.proximasVencer.map((ata) => (
                <div key={ata.id} className="p-4 hover:bg-gray-50 transition-colors">
                  <AtaCard ata={ata} />
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
