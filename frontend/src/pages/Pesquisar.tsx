import { useState, useCallback } from 'react';
import { Search } from 'lucide-react';
import { itensApi } from '../services/api';
import { SearchInput } from '../components/SearchInput';
import { ItemCard } from '../components/ItemCard';
import { EmptyState } from '../components/EmptyState';
import { Loading } from '../components/Loading';
import type { ItemPesquisa } from '../types';

export function Pesquisar() {
  const [items, setItems] = useState<ItemPesquisa[]>([]);
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);
  const [incluirSemAta, setIncluirSemAta] = useState(false);

  const handleSearch = useCallback(async (query: string) => {
    if (!query) {
      setItems([]);
      setSearched(false);
      return;
    }

    try {
      setLoading(true);
      const results = await itensApi.pesquisar(query, !incluirSemAta);
      setItems(results);
      setSearched(true);
    } catch (err) {
      console.error('Erro na pesquisa:', err);
    } finally {
      setLoading(false);
    }
  }, [incluirSemAta]);

  const itensComAta = items.filter(i => i.totalAtasVigentes > 0);
  const itensSemAta = items.filter(i => i.totalAtasVigentes === 0);

  return (
    <div className="min-h-[calc(100vh-4rem)] bg-gradient-to-b from-[#00508C]/5 to-transparent">
      <div className="max-w-3xl mx-auto px-4 py-8">
        {/* Hero Section */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-[#00508C]/10 mb-4">
            <Search className="w-8 h-8 text-[#00508C]" />
          </div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">
            Pesquisar Item
          </h1>
          <p className="mt-2 text-gray-600">
            Digite o nome do medicamento ou material para verificar se existe ata vigente
          </p>
        </div>

        {/* Search Box */}
        <div className="mb-6">
          <SearchInput
            placeholder="Ex: dipirona, seringa, luva..."
            onSearch={handleSearch}
            loading={loading}
            autoFocus
          />
        </div>

        {/* Filter */}
        <div className="flex items-center justify-center gap-2 mb-6">
          <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
            <input
              type="checkbox"
              checked={incluirSemAta}
              onChange={(e) => setIncluirSemAta(e.target.checked)}
              className="w-4 h-4 rounded border-gray-300 text-[#00508C] focus:ring-[#00508C]"
            />
            Incluir itens sem ata vigente
          </label>
        </div>

        {/* Results */}
        {loading ? (
          <Loading message="Pesquisando..." />
        ) : searched ? (
          items.length === 0 ? (
            <EmptyState
              type="search"
              title="Nenhum item encontrado"
              description="Tente buscar com outras palavras-chave"
            />
          ) : (
            <div className="space-y-6">
              {/* Itens COM ata */}
              {itensComAta.length > 0 && (
                <div>
                  <h2 className="text-lg font-semibold text-green-700 mb-3 flex items-center gap-2">
                    <span className="w-2 h-2 rounded-full bg-green-500" />
                    Com ata vigente ({itensComAta.length})
                  </h2>
                  <div className="space-y-3">
                    {itensComAta.map((item) => (
                      <ItemCard key={item.codigoItem} item={item} />
                    ))}
                  </div>
                </div>
              )}

              {/* Itens SEM ata */}
              {itensSemAta.length > 0 && (
                <div>
                  <h2 className="text-lg font-semibold text-red-700 mb-3 flex items-center gap-2">
                    <span className="w-2 h-2 rounded-full bg-red-500" />
                    Sem ata vigente ({itensSemAta.length})
                  </h2>
                  <div className="space-y-3">
                    {itensSemAta.map((item) => (
                      <ItemCard key={item.codigoItem} item={item} />
                    ))}
                  </div>
                </div>
              )}

              {/* Summary */}
              <div className="text-center text-sm text-gray-500 py-4">
                {items.length} resultado{items.length !== 1 ? 's' : ''} encontrado{items.length !== 1 ? 's' : ''}
              </div>
            </div>
          )
        ) : (
          <div className="text-center py-12">
            <div className="inline-flex items-center gap-2 text-gray-400">
              <Search size={20} />
              <span>Digite para pesquisar</span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
