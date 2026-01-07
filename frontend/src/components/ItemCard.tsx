import { useNavigate } from 'react-router-dom';
import { Package, CheckCircle, XCircle } from 'lucide-react';
import { Card } from './Card';
import type { ItemPesquisa } from '../types';

interface ItemCardProps {
  item: ItemPesquisa;
}

export function ItemCard({ item }: ItemCardProps) {
  const navigate = useNavigate();
  const temAta = item.totalAtasVigentes > 0;

  return (
    <Card
      hover
      onClick={() => navigate(`/item/${item.codigoItem}`)}
      className="overflow-hidden"
    >
      <div className="p-4">
        <div className="flex items-start gap-3">
          <div className={`p-2 rounded-lg shrink-0 ${temAta ? 'bg-green-100' : 'bg-red-100'}`}>
            <Package className={temAta ? 'text-green-600' : 'text-red-600'} size={24} />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="text-xs font-medium px-2 py-0.5 rounded bg-gray-100 text-gray-600">
                {item.tipoItem}
              </span>
              <span className="text-xs text-gray-400">
                #{item.codigoItem}
              </span>
            </div>
            <h3 className="mt-1 font-medium text-gray-900 line-clamp-2">
              {item.descricaoPrincipal || 'Sem descrição'}
            </h3>
            {item.outrasDescricoes.length > 0 && (
              <p className="mt-1 text-xs text-gray-500 line-clamp-1">
                Também: {item.outrasDescricoes.join(', ')}
              </p>
            )}
          </div>
        </div>

        <div className="mt-3 pt-3 border-t border-gray-100 flex items-center justify-between">
          <div className="flex items-center gap-2">
            {temAta ? (
              <>
                <CheckCircle className="text-green-500" size={18} />
                <span className="text-sm font-medium text-green-700">
                  {item.totalAtasVigentes} ata{item.totalAtasVigentes !== 1 ? 's' : ''} vigente{item.totalAtasVigentes !== 1 ? 's' : ''}
                </span>
              </>
            ) : (
              <>
                <XCircle className="text-red-500" size={18} />
                <span className="text-sm font-medium text-red-700">
                  Sem ata vigente
                </span>
              </>
            )}
          </div>
          <span className="text-sm text-[#00508C] font-medium">
            Ver detalhes →
          </span>
        </div>
      </div>
    </Card>
  );
}
