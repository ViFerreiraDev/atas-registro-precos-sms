export interface ItemPesquisa {
  codigoItem: number;
  tipoItem: string;
  descricaoPrincipal: string | null;
  totalAtasVigentes: number;
  totalAtasVencidas: number;
  outrasDescricoes: string[];
}

export interface ItemDetalhe {
  codigoItem: number;
  tipoItem: string;
  descricaoPrincipal: string | null;
  codigoPdm: number | null;
  nomePdm: string | null;
  todasDescricoes: string[];
  atasVigentes: AtaResumo[];
  atasVencidas: AtaResumo[];
}

export interface ItemPreview {
  codigoItem: number;
  descricao: string | null;
  tipoItem: string | null;
}

export interface AtaResumo {
  id: number;
  numeroAta: string;
  dataVigenciaFinal: string;
  diasParaVencer: number;
  statusVigencia: 'Vigente' | 'Atencao' | 'Alerta' | 'Critico' | 'Vencida';
  linkPncp: string | null;
  fornecedor: string | null;
  valorUnitario: number | null;
  descricaoItemOriginal: string | null;
  totalItens: number;
  valorTotal: number;
  itensPreview?: ItemPreview[];
}

export interface AtaDetalhe {
  id: number;
  numeroAta: string;
  nomeUnidadeGerenciadora: string | null;
  nomeModalidadeCompra: string | null;
  dataAssinatura: string | null;
  dataVigenciaInicial: string;
  dataVigenciaFinal: string;
  diasParaVencer: number;
  statusVigencia: string;
  linkPncp: string | null;
  itens: AtaItem[];
}

export interface AtaItem {
  codigoItem: number;
  descricaoItemOriginal: string | null;
  tipoItem: string | null;
  fornecedor: string | null;
  valorUnitario: number | null;
  quantidadeHomologadaItem: number | null;
  quantidadeEmpenhada: number | null;
}

export interface Dashboard {
  totalAtasVigentes: number;
  atasCriticas: number;
  atasAlerta: number;
  atasAtencao: number;
  totalItensComAta: number;
  totalItensSemAta: number;
  proximasVencer: AtaResumo[];
}

export interface ItemSemAta {
  codigoItem: number;
  tipoItem: string;
  descricaoPrincipal: string | null;
  ultimaAtaVencida: string | null;
}

export interface ItemAlerta {
  codigoItem: number;
  tipoItem: string;
  descricaoPrincipal: string | null;
  numeroAtaVigente: string;
  dataVigenciaFinal: string;
  diasParaVencer: number;
  statusVigencia: 'Vigente' | 'Atencao' | 'Alerta' | 'Critico' | 'Vencida';
  linkPncp: string | null;
  fornecedor: string | null;
  valorUnitario: number | null;
}

export interface FiltrosItem {
  status?: string;
  tipoItem?: string;
  busca?: string;
  diasMin?: number;
  diasMax?: number;
  todos?: boolean;
}

export interface SincronizacaoResult {
  sucesso: boolean;
  mensagem: string;
  paginasProcessadas: number;
  totalPaginas: number;
  itensProcessados: number;
  atasNovas: number;
  itensNovos: number;
  erros: number;
}

export interface SincronizacaoStatus {
  emAndamento: boolean;
  paginasProcessadas: number;
  paginasSucesso: number;
  totalPaginas: number;
  paginasPendentes: number;
  paginasComErro: number;
  totalItensProcessados: number;
  ultimaAtualizacao: string | null;
  dataUltimaAta: string | null;
  totalAtas: number;
  paginasFalhadas: number[];
}
