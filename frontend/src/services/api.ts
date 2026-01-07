import axios from 'axios';
import type { ItemPesquisa, ItemDetalhe, AtaResumo, AtaDetalhe, Dashboard, ItemSemAta, ItemAlerta, FiltrosItem, SincronizacaoResult, SincronizacaoStatus } from '../types';

const api = axios.create({
  baseURL: '/api',
});

export const itensApi = {
  pesquisar: async (q: string, apenasVigentes = true): Promise<ItemPesquisa[]> => {
    const { data } = await api.get('/itens/pesquisar', { params: { q, apenasVigentes } });
    return data;
  },

  obter: async (codigoItem: number): Promise<ItemDetalhe> => {
    const { data } = await api.get(`/itens/${codigoItem}`);
    return data;
  },

  semAta: async (limite = 50): Promise<ItemSemAta[]> => {
    const { data } = await api.get('/itens/sem-ata', { params: { limite } });
    return data;
  },

  porStatus: async (filtros: FiltrosItem = {}): Promise<ItemAlerta[]> => {
    const { data } = await api.get('/itens/por-status', { params: filtros });
    return data;
  },
};

export const atasApi = {
  vigentes: async (status?: string, limite = 100): Promise<AtaResumo[]> => {
    const { data } = await api.get('/atas/vigentes', { params: { status, limite } });
    return data;
  },

  novas: async (dias = 30): Promise<AtaResumo[]> => {
    const { data } = await api.get('/atas/novas', { params: { dias } });
    return data;
  },

  recemEncerradas: async (dias = 15): Promise<AtaResumo[]> => {
    const { data } = await api.get('/atas/recem-encerradas', { params: { dias } });
    return data;
  },

  obter: async (id: number): Promise<AtaDetalhe> => {
    const { data } = await api.get(`/atas/${id}`);
    return data;
  },

  pesquisar: async (q: string): Promise<AtaResumo[]> => {
    const { data } = await api.get('/atas/pesquisar', { params: { q } });
    return data;
  },
};

export const dashboardApi = {
  obter: async (): Promise<Dashboard> => {
    const { data } = await api.get('/dashboard');
    return data;
  },

  historico: async (meses = 12): Promise<{ mes: string; vigentes: number }[]> => {
    const { data } = await api.get('/dashboard/historico', { params: { meses } });
    return data;
  },
};

export interface SincronizacaoConfig {
  modoParalelo?: boolean;
  intervaloEntreRequisicoesMs?: number;
  maxConcorrencia?: number;
}

export const sincronizacaoApi = {
  sincronizar: async (): Promise<SincronizacaoResult> => {
    const { data } = await api.post('/sincronizacao');
    return data;
  },

  sincronizarParalelo: async (config?: SincronizacaoConfig): Promise<SincronizacaoResult> => {
    const { data } = await api.post('/sincronizacao/paralelo', config || {
      modoParalelo: true,
      intervaloEntreRequisicoesMs: 1000,
      maxConcorrencia: 10
    });
    return data;
  },

  continuar: async (): Promise<SincronizacaoResult> => {
    const { data } = await api.post('/sincronizacao/continuar');
    return data;
  },

  atualizar: async (): Promise<SincronizacaoResult> => {
    const { data } = await api.post('/sincronizacao/atualizar');
    return data;
  },

  status: async (): Promise<SincronizacaoStatus> => {
    const { data } = await api.get('/sincronizacao/status');
    return data;
  },

  parar: async (): Promise<{ message: string }> => {
    const { data } = await api.post('/sincronizacao/parar');
    return data;
  },

  resetarTravadas: async (): Promise<{ message: string }> => {
    const { data } = await api.post('/sincronizacao/resetar-travadas');
    return data;
  },

  limparLogs: async (): Promise<{ message: string }> => {
    const { data } = await api.delete('/sincronizacao/logs');
    return data;
  },

  resetarDados: async (): Promise<{ message: string }> => {
    const { data } = await api.delete('/sincronizacao/reset');
    return data;
  },
};
