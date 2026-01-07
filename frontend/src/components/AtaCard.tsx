import { useNavigate } from 'react-router-dom';
import { FileText, Calendar, Building2, ExternalLink, Package, DollarSign, Box, Wrench } from 'lucide-react';
import { Card } from './Card';
import { StatusBadge } from './StatusBadge';
import type { AtaResumo } from '../types';

interface AtaCardProps {
  ata: AtaResumo;
  showItem?: boolean;
  showItensPreview?: boolean;
}

export function AtaCard({ ata, showItem = false, showItensPreview = false }: AtaCardProps) {
  const navigate = useNavigate();

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('pt-BR');
  };

  const formatCurrency = (value: number | null) => {
    if (value === null) return '-';
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  };

  const itensRestantes = (ata.totalItens || 0) - (ata.itensPreview?.length || 0);

  return (
    <Card
      hover
      onClick={() => navigate(`/ata/${ata.id}`)}
    >
      <div className="p-4">
        {/* Cabeçalho: Número da Ata + Status */}
        <div className="flex items-center justify-between gap-3">
          <h3 className="font-semibold text-gray-900">
            Ata {ata.numeroAta}
          </h3>
          <StatusBadge status={ata.statusVigencia} dias={ata.diasParaVencer} size="sm" />
        </div>

        {/* Preview dos itens */}
        {showItensPreview && ata.itensPreview && ata.itensPreview.length > 0 && (
          <div className="mt-2 pt-2 border-t border-gray-100 space-y-1">
            {ata.itensPreview.map((item, index) => (
              <div key={index} className="flex items-start gap-2 text-sm">
                {item.tipoItem === 'Material' ? (
                  <Box size={14} className="text-blue-500 shrink-0 mt-0.5" />
                ) : (
                  <Wrench size={14} className="text-purple-500 shrink-0 mt-0.5" />
                )}
                <span className="text-gray-600 line-clamp-1">{item.descricao || `Item ${item.codigoItem}`}</span>
              </div>
            ))}
            {itensRestantes > 0 && (
              <span className="text-xs text-gray-400 ml-5">(+{itensRestantes} {itensRestantes === 1 ? 'item' : 'itens'})</span>
            )}
          </div>
        )}

        {/* Descritivo do Item (área completa) */}
        {showItem && ata.descricaoItemOriginal && (
          <div className="mt-2 pt-2 border-t border-gray-100 flex items-start gap-3">
            <div className="p-2 rounded-lg bg-blue-100 shrink-0">
              <FileText className="text-blue-600" size={20} />
            </div>
            <p className="text-sm text-gray-700">
              {ata.descricaoItemOriginal}
            </p>
          </div>
        )}

        {/* Informações: Vencimento, Itens, Valor Total */}
        <div className="mt-3 pt-3 border-t border-gray-100 grid grid-cols-2 gap-2 text-sm">
          <div className="flex items-center gap-2 text-gray-600">
            <Calendar size={16} className="shrink-0" />
            <span>Vence: {formatDate(ata.dataVigenciaFinal)}</span>
          </div>
          <div className="flex items-center gap-2 text-gray-600 justify-end">
            <Package size={16} className="shrink-0" />
            <span>{ata.totalItens || 0} {(ata.totalItens || 0) === 1 ? 'item' : 'itens'}</span>
          </div>
          {ata.valorTotal > 0 && (
            <div className="col-span-2 flex items-center gap-2 text-gray-900 font-medium">
              <DollarSign size={16} className="shrink-0 text-green-600" />
              <span>Valor Total: {formatCurrency(ata.valorTotal)}</span>
            </div>
          )}
          {ata.fornecedor && (
            <div className="col-span-2 flex items-center gap-2 text-gray-600">
              <Building2 size={16} className="shrink-0" />
              <span className="truncate">{ata.fornecedor}</span>
            </div>
          )}
          {ata.linkPncp && (
            <div className="col-span-2">
              <a
                href={ata.linkPncp}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1 text-xs text-[#00508C] hover:underline"
                onClick={(e) => e.stopPropagation()}
              >
                <ExternalLink size={12} />
                Baixar no PNCP
              </a>
            </div>
          )}
        </div>
      </div>
    </Card>
  );
}
