import type { ReactNode } from 'react';
import { Card } from './Card';

interface StatCardProps {
  title: string;
  value: number | string;
  icon: ReactNode;
  color?: 'blue' | 'green' | 'yellow' | 'red' | 'cyan' | 'gray';
  subtitle?: string;
  onClick?: () => void;
}

const colorClasses = {
  blue: 'bg-blue-50 text-blue-600',
  green: 'bg-green-50 text-green-600',
  yellow: 'bg-yellow-50 text-yellow-600',
  red: 'bg-red-50 text-red-600',
  cyan: 'bg-cyan-50 text-cyan-600',
  gray: 'bg-gray-50 text-gray-600',
};

export function StatCard({ title, value, icon, color = 'blue', subtitle, onClick }: StatCardProps) {
  return (
    <Card hover={!!onClick} onClick={onClick}>
      <div className="p-4 sm:p-6">
        <div className="flex items-start justify-between">
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-gray-500 truncate">{title}</p>
            <p className="mt-1 text-2xl sm:text-3xl font-bold text-gray-900">{value}</p>
            {subtitle && (
              <p className="mt-1 text-sm text-gray-500">{subtitle}</p>
            )}
          </div>
          <div className={`p-3 rounded-xl ${colorClasses[color]}`}>
            {icon}
          </div>
        </div>
      </div>
    </Card>
  );
}
