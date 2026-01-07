import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Header } from './components/Header';
import { Dashboard } from './pages/Dashboard';
import { Materiais } from './pages/Materiais';
import { Servicos } from './pages/Servicos';
import { Atas } from './pages/Atas';
import { NovasAtas } from './pages/NovasAtas';
import { Alertas } from './pages/Alertas';
import { Configuracoes } from './pages/Configuracoes';
import { ItemDetalhe } from './pages/ItemDetalhe';
import { AtaDetalhe } from './pages/AtaDetalhe';

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-gray-50">
        <Header />
        <main>
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/materiais" element={<Materiais />} />
            <Route path="/servicos" element={<Servicos />} />
            <Route path="/atas" element={<Atas />} />
            <Route path="/novas" element={<NovasAtas />} />
            <Route path="/alertas" element={<Alertas />} />
            <Route path="/configuracoes" element={<Configuracoes />} />
            <Route path="/item/:codigoItem" element={<ItemDetalhe />} />
            <Route path="/ata/:id" element={<AtaDetalhe />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}

export default App;
