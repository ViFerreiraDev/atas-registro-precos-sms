import { Search, FileX, PackageX, WifiOff, RefreshCw } from 'lucide-react';

interface EmptyStateProps {
  type: 'search' | 'no-data' | 'no-items' | 'error';
  title: string;
  description?: string;
  onRetry?: () => void;
}

const icons = {
  search: Search,
  'no-data': FileX,
  'no-items': PackageX,
  'error': WifiOff,
};

const backgroundColors = {
  search: 'bg-gray-100',
  'no-data': 'bg-gray-100',
  'no-items': 'bg-gray-100',
  'error': 'bg-orange-50',
};

const iconColors = {
  search: 'text-gray-400',
  'no-data': 'text-gray-400',
  'no-items': 'text-gray-400',
  'error': 'text-orange-400',
};

export function EmptyState({ type, title, description, onRetry }: EmptyStateProps) {
  const Icon = icons[type];

  return (
    <div className="flex flex-col items-center justify-center py-12 px-4 text-center">
      <div className={`p-4 rounded-full ${backgroundColors[type]} mb-4`}>
        <Icon className={`w-8 h-8 ${iconColors[type]}`} />
      </div>
      <h3 className="text-lg font-medium text-gray-900">{title}</h3>
      {description && (
        <p className="mt-2 text-sm text-gray-500 max-w-sm">{description}</p>
      )}
      {onRetry && (
        <button
          onClick={onRetry}
          className="mt-4 flex items-center gap-2 px-4 py-2.5 bg-[#00508C] text-white rounded-xl hover:bg-[#003d6a] transition-colors font-medium"
        >
          <RefreshCw size={18} />
          Tentar novamente
        </button>
      )}
    </div>
  );
}
