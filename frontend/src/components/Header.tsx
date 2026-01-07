import { Link, useLocation } from 'react-router-dom';
import { Home, FileText, AlertTriangle, Menu, X, Package, Wrench, Settings, Sparkles } from 'lucide-react';
import { useState } from 'react';

export function Header() {
  const location = useLocation();
  const [menuOpen, setMenuOpen] = useState(false);

  const isActive = (path: string) => location.pathname === path;

  const navItems = [
    { path: '/', icon: Home, label: 'Dashboard' },
    { path: '/materiais', icon: Package, label: 'Materiais' },
    { path: '/servicos', icon: Wrench, label: 'Serviços' },
    { path: '/atas', icon: FileText, label: 'Atas' },
    { path: '/novas', icon: Sparkles, label: 'Novas' },
    { path: '/alertas', icon: AlertTriangle, label: 'Alertas' },
    { path: '/configuracoes', icon: Settings, label: 'Config' },
  ];

  return (
    <header className="bg-[#00508C] text-white shadow-lg sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <Link to="/" className="flex items-center gap-3">
            <img src="https://saude.prefeitura.rio/wp-content/uploads/sites/47/2025/02/Logo_PCRJ-Saude_HorizontalSUS_UmaCor-Branco.png" alt="SMS-Rio" className="h-10 w-auto" />
            <div className="hidden lg:block border-l border-white/30 pl-4">
              <div className="font-semibold text-sm leading-tight">Sistema de Atas</div>
              <div className="text-xs opacity-80 leading-tight">Registro de Preços</div>
            </div>
          </Link>

          {/* Desktop Nav */}
          <nav className="hidden md:flex items-center gap-1">
            {navItems.map(({ path, icon: Icon, label }) => (
              <Link
                key={path}
                to={path}
                className={`flex items-center gap-2 px-4 py-2 rounded-lg transition-colors ${
                  isActive(path)
                    ? 'bg-white/20 text-white'
                    : 'text-white/80 hover:bg-white/10 hover:text-white'
                }`}
              >
                <Icon size={18} />
                <span>{label}</span>
              </Link>
            ))}
          </nav>

          {/* Mobile Menu Button */}
          <button
            onClick={() => setMenuOpen(!menuOpen)}
            className="md:hidden p-2 rounded-lg hover:bg-white/10"
          >
            {menuOpen ? <X size={24} /> : <Menu size={24} />}
          </button>
        </div>

        {/* Mobile Nav */}
        {menuOpen && (
          <nav className="md:hidden pb-4 border-t border-white/20 pt-4">
            {navItems.map(({ path, icon: Icon, label }) => (
              <Link
                key={path}
                to={path}
                onClick={() => setMenuOpen(false)}
                className={`flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                  isActive(path)
                    ? 'bg-white/20 text-white'
                    : 'text-white/80 hover:bg-white/10'
                }`}
              >
                <Icon size={20} />
                <span>{label}</span>
              </Link>
            ))}
          </nav>
        )}
      </div>
    </header>
  );
}
