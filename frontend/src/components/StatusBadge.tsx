interface StatusBadgeProps {
  status: string;
  dias?: number;
  size?: 'sm' | 'md' | 'lg';
}

const statusConfig: Record<string, { bg: string; text: string; label: string }> = {
  Vigente: { bg: 'bg-green-500', text: 'text-white', label: 'Vigente' },
  Atencao: { bg: 'bg-cyan-500', text: 'text-white', label: 'Atenção' },
  Alerta: { bg: 'bg-yellow-400', text: 'text-gray-900', label: 'Alerta' },
  Critico: { bg: 'bg-red-500', text: 'text-white', label: 'Crítico' },
  Vencida: { bg: 'bg-gray-500', text: 'text-white', label: 'Vencida' },
};

export function StatusBadge({ status, dias, size = 'md' }: StatusBadgeProps) {
  const config = statusConfig[status] || statusConfig.Vigente;

  const sizeClasses = {
    sm: 'text-xs px-2 py-0.5',
    md: 'text-sm px-3 py-1',
    lg: 'text-base px-4 py-1.5',
  };

  return (
    <span className={`inline-flex items-center gap-1 rounded-full font-medium ${config.bg} ${config.text} ${sizeClasses[size]}`}>
      {config.label}
      {dias !== undefined && dias > 0 && (
        <span className="opacity-80">({dias}d)</span>
      )}
    </span>
  );
}
