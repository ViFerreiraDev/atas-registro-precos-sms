import { Search, X, Loader2 } from 'lucide-react';
import { useState, useRef, useEffect } from 'react';

interface SearchInputProps {
  placeholder?: string;
  onSearch: (query: string) => void;
  loading?: boolean;
  minLength?: number;
  autoFocus?: boolean;
}

export function SearchInput({
  placeholder = 'Pesquisar...',
  onSearch,
  loading = false,
  minLength = 3,
  autoFocus = false,
}: SearchInputProps) {
  const [value, setValue] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);

  // Auto focus
  useEffect(() => {
    if (autoFocus && inputRef.current) {
      inputRef.current.focus();
    }
  }, [autoFocus]);

  const handleSearch = () => {
    if (value.length >= minLength) {
      onSearch(value);
    } else if (value.length === 0) {
      onSearch('');
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  const handleClear = () => {
    setValue('');
    onSearch('');
    inputRef.current?.focus();
  };

  return (
    <div className="relative">
      <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
        {loading ? (
          <Loader2 className="h-5 w-5 text-gray-400 animate-spin" />
        ) : (
          <Search className="h-5 w-5 text-gray-400" />
        )}
      </div>
      <input
        ref={inputRef}
        type="text"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        className="w-full pl-12 pr-24 py-4 text-lg border-2 border-gray-200 rounded-xl focus:border-[#00508C] focus:ring-2 focus:ring-[#00508C]/20 outline-none transition-all"
      />
      <div className="absolute inset-y-0 right-0 flex items-center gap-1 pr-2">
        {value && (
          <button
            onClick={handleClear}
            className="p-2 text-gray-400 hover:text-gray-600"
            title="Limpar"
          >
            <X className="h-5 w-5" />
          </button>
        )}
        <button
          onClick={handleSearch}
          disabled={loading || (value.length > 0 && value.length < minLength)}
          className="px-3 py-2 bg-[#00508C] text-white rounded-lg hover:bg-[#003d6b] disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          title="Buscar"
        >
          <Search className="h-4 w-4" />
        </button>
      </div>
      {value.length > 0 && value.length < minLength && (
        <p className="absolute -bottom-6 left-0 text-sm text-gray-500">
          Digite pelo menos {minLength} caracteres
        </p>
      )}
    </div>
  );
}
