import { Loader2 } from 'lucide-react';

interface LoadingProps {
  message?: string;
}

export function Loading({ message = 'Carregando...' }: LoadingProps) {
  return (
    <div className="flex flex-col items-center justify-center py-12">
      <Loader2 className="w-8 h-8 text-[#00508C] animate-spin" />
      <p className="mt-4 text-sm text-gray-500">{message}</p>
    </div>
  );
}
