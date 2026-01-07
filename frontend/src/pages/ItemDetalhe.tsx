import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Package, CheckCircle, XCircle, Tag, Hash } from 'lucide-react';
import { itensApi } from '../services/api';
import { Card, CardHeader, CardContent } from '../components/Card';
import { AtaCard } from '../components/AtaCard';
import { Loading } from '../components/Loading';
import { EmptyState } from '../components/EmptyState';
import type { ItemDetalhe as ItemDetalheType } from '../types';

export function ItemDetalhe() {
  const { codigoItem } = useParams<{ codigoItem: string }>();
  const navigate = useNavigate();
  const [item, setItem] = useState<ItemDetalheType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadItem = async () => {
      if (!codigoItem) return;
      try {
        setLoading(true);
        setError(null);
        const data = await itensApi.obter(parseInt(codigoItem));
        setItem(data);
      } catch (err) {
        setError('Item não encontrado');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    loadItem();
  }, [codigoItem]);

  if (loading) {
    return <Loading message="Carregando item..." />;
  }

  if (error || !item) {
    return (
      <div className="p-4 max-w-3xl mx-auto">
        <button
          onClick={() => navigate(-1)}
          className="flex items-center gap-2 text-gray-600 hover:text-gray-900 mb-4"
        >
          <ArrowLeft size={20} />
          Voltar
        </button>
        <Card>
          <CardContent>
            <EmptyState
              type="no-items"
              title="Item não encontrado"
              description="Não conseguimos localizar este item. Ele pode ter sido removido ou o código está incorreto."
            />
          </CardContent>
        </Card>
      </div>
    );
  }

  const temAtaVigente = item.atasVigentes.length > 0;

  return (
    <div className="p-4 max-w-3xl mx-auto space-y-4">
      {/* Back button */}
      <button
        onClick={() => navigate(-1)}
        className="flex items-center gap-2 text-gray-600 hover:text-gray-900"
      >
        <ArrowLeft size={20} />
        Voltar
      </button>

      {/* Item Header Card */}
      <Card>
        <CardContent>
          <div className="flex items-start gap-4">
            <div className={`p-3 rounded-xl shrink-0 ${temAtaVigente ? 'bg-green-100' : 'bg-red-100'}`}>
              <Package className={temAtaVigente ? 'text-green-600' : 'text-red-600'} size={32} />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 flex-wrap mb-2">
                <span className="text-sm font-medium px-2 py-1 rounded bg-gray-100 text-gray-600">
                  {item.tipoItem}
                </span>
                <span className="flex items-center gap-1 text-sm text-gray-500">
                  <Hash size={14} />
                  {item.codigoItem}
                </span>
              </div>
              <h1 className="text-xl sm:text-2xl font-bold text-gray-900">
                {item.descricaoPrincipal || 'Sem descrição principal'}
              </h1>

              {/* Status */}
              <div className="mt-4 flex items-center gap-2">
                {temAtaVigente ? (
                  <>
                    <CheckCircle className="text-green-500" size={24} />
                    <span className="text-lg font-semibold text-green-700">
                      {item.atasVigentes.length} ata{item.atasVigentes.length !== 1 ? 's' : ''} vigente{item.atasVigentes.length !== 1 ? 's' : ''}
                    </span>
                  </>
                ) : (
                  <>
                    <XCircle className="text-red-500" size={24} />
                    <span className="text-lg font-semibold text-red-700">
                      Sem ata vigente
                    </span>
                  </>
                )}
              </div>

              {/* PDM */}
              {item.nomePdm && (
                <div className="mt-3 flex items-center gap-2 text-sm text-gray-600">
                  <Tag size={16} />
                  <span>PDM: {item.nomePdm}</span>
                </div>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Todas as descrições */}
      {item.todasDescricoes.length > 1 && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-900">
              Descrições Encontradas ({item.todasDescricoes.length})
            </h2>
          </CardHeader>
          <CardContent className="p-0">
            <ul className="divide-y divide-gray-100">
              {item.todasDescricoes.map((desc, idx) => (
                <li key={idx} className="px-4 py-3 text-sm text-gray-700">
                  {desc}
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      {/* Atas Vigentes */}
      <Card>
        <CardHeader>
          <h2 className="font-semibold text-gray-900">
            Atas Vigentes
          </h2>
        </CardHeader>
        <CardContent className="p-0">
          {item.atasVigentes.length === 0 ? (
            <EmptyState
              type="no-data"
              title="Nenhuma ata vigente"
              description="Este item não possui ata de registro de preço vigente no momento"
            />
          ) : (
            <div className="divide-y divide-gray-100">
              {item.atasVigentes.map((ata) => (
                <div key={ata.id} className="p-4">
                  <AtaCard ata={ata} showItem />
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Atas Vencidas */}
      {item.atasVencidas.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-900">
              Atas Vencidas ({item.atasVencidas.length})
            </h2>
          </CardHeader>
          <CardContent className="p-0">
            <div className="divide-y divide-gray-100">
              {item.atasVencidas.map((ata) => (
                <div key={ata.id} className="p-4">
                  <AtaCard ata={ata} showItem />
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
