import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, FileText, Calendar, Building2, Hash, ExternalLink, Share2, Loader2 } from 'lucide-react';
import { atasApi } from '../services/api';
import { Card, CardHeader, CardContent } from '../components/Card';
import { StatusBadge } from '../components/StatusBadge';
import { Loading } from '../components/Loading';
import { EmptyState } from '../components/EmptyState';
import type { AtaDetalhe as AtaDetalheType } from '../types';

export function AtaDetalhe() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [ata, setAta] = useState<AtaDetalheType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadAta = async () => {
      if (!id) return;
      try {
        setLoading(true);
        setError(null);
        const data = await atasApi.obter(parseInt(id));
        setAta(data);
      } catch (err) {
        setError('Ata não encontrada');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    loadAta();
  }, [id]);

  const formatDate = (dateStr: string | null) => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('pt-BR');
  };

  const formatCurrency = (value: number | null) => {
    if (value === null) return '-';
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  };

  const [sharing, setSharing] = useState(false);

  const handleShare = async () => {
    if (!ata?.linkPncp) return;

    setSharing(true);
    try {
      // Baixa o PDF
      const response = await fetch(ata.linkPncp);
      const blob = await response.blob();

      // Cria o arquivo para compartilhar
      const fileName = `Ata_${ata.numeroAta.replace('/', '-')}.pdf`;
      const file = new File([blob], fileName, { type: 'application/pdf' });

      const shareData = {
        title: `Ata ${ata.numeroAta}`,
        text: `Ata de Registro de Preço ${ata.numeroAta} - ${ata.nomeUnidadeGerenciadora || 'PREF.MUN.DO RIO DE JANEIRO'}`,
        files: [file]
      };

      // Verifica se o dispositivo suporta compartilhamento de arquivos
      if (navigator.share && navigator.canShare(shareData)) {
        await navigator.share(shareData);
      } else {
        // Fallback: baixa o arquivo diretamente
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
      }
    } catch (err) {
      if ((err as Error).name !== 'AbortError') {
        console.error('Erro ao compartilhar:', err);
        // Fallback: abre o link
        window.open(ata.linkPncp, '_blank');
      }
    } finally {
      setSharing(false);
    }
  };

  if (loading) {
    return <Loading message="Carregando ata..." />;
  }

  if (error || !ata) {
    return (
      <div className="p-4 max-w-4xl mx-auto">
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
              type="no-data"
              title="Ata não encontrada"
              description="Não conseguimos localizar esta ata. Ela pode ter sido removida ou o código está incorreto."
            />
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="p-4 max-w-4xl mx-auto space-y-4">
      {/* Back button */}
      <button
        onClick={() => navigate(-1)}
        className="flex items-center gap-2 text-gray-600 hover:text-gray-900"
      >
        <ArrowLeft size={20} />
        Voltar
      </button>

      {/* Header Card */}
      <Card>
        <CardContent>
          <div className="flex items-start gap-4">
            <div className="p-3 rounded-xl bg-blue-100 shrink-0">
              <FileText className="text-blue-600" size={32} />
            </div>
            <div className="flex-1">
              <div className="flex items-center gap-3 flex-wrap">
                <h1 className="text-xl sm:text-2xl font-bold text-gray-900">
                  Ata {ata.numeroAta}
                </h1>
                <StatusBadge status={ata.statusVigencia} dias={ata.diasParaVencer} size="md" />
              </div>
              {ata.nomeUnidadeGerenciadora && (
                <p className="text-gray-600 mt-1">{ata.nomeUnidadeGerenciadora}</p>
              )}
            </div>
          </div>

          {/* Dates */}
          <div className="mt-6 grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
              <Calendar className="text-gray-400" size={20} />
              <div>
                <p className="text-xs text-gray-500">Assinatura</p>
                <p className="font-medium">{formatDate(ata.dataAssinatura)}</p>
              </div>
            </div>
            <div className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
              <Calendar className="text-green-500" size={20} />
              <div>
                <p className="text-xs text-gray-500">Início Vigência</p>
                <p className="font-medium">{formatDate(ata.dataVigenciaInicial)}</p>
              </div>
            </div>
            <div className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg">
              <Calendar className="text-red-500" size={20} />
              <div>
                <p className="text-xs text-gray-500">Fim Vigência</p>
                <p className="font-medium">{formatDate(ata.dataVigenciaFinal)}</p>
              </div>
            </div>
          </div>

          {/* Botões PNCP */}
          {ata.linkPncp && (
            <div className="mt-4 flex flex-col sm:flex-row gap-2 sm:justify-center">
              <a
                href={ata.linkPncp}
                target="_blank"
                rel="noopener noreferrer"
                className="flex-1 sm:flex-initial inline-flex items-center justify-center gap-2 px-4 py-2 bg-[#00508C] text-white rounded-lg hover:bg-[#003d6b] transition-colors"
              >
                <ExternalLink size={18} />
                Baixar Ata no PNCP
              </a>
              <button
                onClick={handleShare}
                disabled={sharing}
                className="flex-1 sm:flex-initial inline-flex items-center justify-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {sharing ? (
                  <Loader2 size={18} className="animate-spin" />
                ) : (
                  <Share2 size={18} />
                )}
                {sharing ? 'Preparando...' : 'Compartilhar'}
              </button>
            </div>
          )}
        </CardContent>

      </Card>

      {/* Itens da Ata */}
      <Card>
        <CardHeader>
          <h2 className="font-semibold text-gray-900">
            Itens da Ata ({ata.itens.length})
          </h2>
        </CardHeader>
        <CardContent className="p-0">
          {ata.itens.length === 0 ? (
            <EmptyState
              type="no-items"
              title="Nenhum item encontrado"
            />
          ) : (
            <div className="overflow-x-auto">
              {/* Mobile: Cards */}
              <div className="sm:hidden divide-y divide-gray-100">
                {ata.itens.map((item, idx) => (
                  <div
                    key={idx}
                    className="p-4 hover:bg-gray-50 cursor-pointer"
                    onClick={() => navigate(`/item/${item.codigoItem}`)}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 text-xs text-gray-500 mb-1">
                          <Hash size={12} />
                          {item.codigoItem}
                          <span className="px-1.5 py-0.5 bg-gray-100 rounded text-gray-600">
                            {item.tipoItem}
                          </span>
                        </div>
                        <p className="text-sm font-medium text-gray-900 line-clamp-2">
                          {item.descricaoItemOriginal || 'Sem descrição'}
                        </p>
                        {item.fornecedor && (
                          <p className="text-xs text-gray-500 mt-1 flex items-center gap-1">
                            <Building2 size={12} />
                            {item.fornecedor}
                          </p>
                        )}
                      </div>
                      <div className="text-right shrink-0">
                        <p className="font-semibold text-gray-900">
                          {formatCurrency(item.valorUnitario)}
                        </p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>

              {/* Desktop: Table */}
              <table className="hidden sm:table w-full">
                <thead className="bg-gray-50 text-left">
                  <tr>
                    <th className="px-4 py-3 text-xs font-medium text-gray-500 uppercase">Código</th>
                    <th className="px-4 py-3 text-xs font-medium text-gray-500 uppercase">Descrição</th>
                    <th className="px-4 py-3 text-xs font-medium text-gray-500 uppercase">Fornecedor</th>
                    <th className="px-4 py-3 text-xs font-medium text-gray-500 uppercase text-right">Valor Unit.</th>
                    <th className="px-4 py-3 text-xs font-medium text-gray-500 uppercase text-right">Qtd.</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {ata.itens.map((item, idx) => (
                    <tr
                      key={idx}
                      className="hover:bg-gray-50 cursor-pointer"
                      onClick={() => navigate(`/item/${item.codigoItem}`)}
                    >
                      <td className="px-4 py-3">
                        <span className="text-sm font-medium text-[#00508C]">
                          {item.codigoItem}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <p className="text-sm text-gray-900 line-clamp-2">
                          {item.descricaoItemOriginal || '-'}
                        </p>
                      </td>
                      <td className="px-4 py-3">
                        <p className="text-sm text-gray-600 truncate max-w-[200px]">
                          {item.fornecedor || '-'}
                        </p>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <span className="text-sm font-medium">
                          {formatCurrency(item.valorUnitario)}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right text-sm text-gray-600">
                        {item.quantidadeHomologadaItem?.toLocaleString('pt-BR') || '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
