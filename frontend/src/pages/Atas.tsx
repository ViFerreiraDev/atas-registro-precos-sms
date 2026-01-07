import { useState, useEffect, useCallback } from 'react';
import { atasApi } from '../services/api';
import { SearchInput } from '../components/SearchInput';
import { AtaCard } from '../components/AtaCard';
import { Loading } from '../components/Loading';
import { EmptyState } from '../components/EmptyState';
import type { AtaResumo } from '../types';

export function Atas() {
  const [atas, setAtas] = useState<AtaResumo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [searchMode, setSearchMode] = useState(false);

  const loadAtas = async () => {
    try {
      setLoading(true);
      setError(false);
      const data = await atasApi.vigentes(undefined, 100);
      setAtas(data);
      setSearchMode(false);
    } catch (err) {
      console.error('Erro ao carregar atas:', err);
      setError(true);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = useCallback(async (query: string) => {
    if (!query) {
      loadAtas();
      return;
    }
    try {
      setLoading(true);
      setError(false);
      const data = await atasApi.pesquisar(query);
      setAtas(data);
      setSearchMode(true);
    } catch (err) {
      console.error('Erro na pesquisa:', err);
      setError(true);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadAtas();
  }, []);

  return (
    <div className="p-4 max-w-4xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Atas de Registro de Preço</h1>
        <p className="text-gray-500">Lista de atas vigentes</p>
      </div>

      <SearchInput
        placeholder="Pesquisar por número da ata ou fornecedor..."
        onSearch={handleSearch}
        loading={loading}
        minLength={1}
      />

      {loading ? (
        <Loading message="Carregando atas..." />
      ) : error ? (
        <EmptyState
          type="error"
          title="Ops! Algo deu errado"
          description="Não foi possível conectar ao servidor. Tente novamente mais tarde."
          onRetry={loadAtas}
        />
      ) : atas.length === 0 ? (
        <EmptyState
          type={searchMode ? 'search' : 'no-data'}
          title={searchMode ? 'Nenhuma ata encontrada' : 'Nenhuma ata vigente'}
          description={searchMode ? 'Tente outro número' : 'Execute a sincronização para importar dados'}
        />
      ) : (
        <div className="space-y-3">
          {atas.map((ata) => (
            <AtaCard key={ata.id} ata={ata} showItensPreview />
          ))}
        </div>
      )}
    </div>
  );
}
