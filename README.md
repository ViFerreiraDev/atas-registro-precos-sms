# Atas de Registro de Precos SMS

Sistema de gerenciamento de Atas de Registro de Precos para licitacoes da Secretaria Municipal de Saude.

## Sobre o Projeto

Este sistema permite o acompanhamento e gestao de atas de registro de precos, oferecendo:

- Dashboard com visao geral e alertas de vigencia
- Consulta e pesquisa de atas e itens
- Controle de materiais e servicos
- Alertas automaticos por faixas de vigencia
- Sincronizacao com banco de dados externo

### Faixas de Alerta de Vigencia

| Status    | Dias para Vencer |
|-----------|------------------|
| Critico   | 0 a 30 dias      |
| Alerta    | 31 a 60 dias     |
| Atencao   | 61 a 120 dias    |
| Vigente   | mais de 120 dias |
| Vencida   | ja venceu        |

## Tecnologias

### Backend
- .NET 9
- Entity Framework Core
- PostgreSQL

### Frontend
- React 19
- TypeScript
- Vite
- Tailwind CSS
- React Router

## Pre-requisitos

Antes de comecar, voce precisa ter instalado:

- [Node.js](https://nodejs.org/) (v18 ou superior)
- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/) (ou acesso a um servidor PostgreSQL)

## Instalacao

### 1. Clone o repositorio

```bash
git clone https://github.com/ViFerreiraDev/atas-registro-precos-sms.git
cd atas-registro-precos-sms
```

### 2. Instale as dependencias do frontend

```bash
npm install
```

### 3. Configure o banco de dados

O projeto utiliza .NET User Secrets para armazenar credenciais de forma segura.

```bash
cd backend

# Inicializar User Secrets (se ainda nao foi inicializado)
dotnet user-secrets init

# Configurar a string de conexao
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=SEU_HOST;Port=5432;Database=siga;Username=SEU_USUARIO;Password=SUA_SENHA"

cd ..
```

> **Nota**: Os secrets sao armazenados localmente em `%APPDATA%\Microsoft\UserSecrets\atas-registro-precos-sms\secrets.json` e nao sao commitados no repositorio.

### 4. Execute as migrations (se necessario)

```bash
cd backend
dotnet ef database update
cd ..
```

## Como Executar

### Desenvolvimento (Backend + Frontend simultaneamente)

```bash
npm run dev
```

### Apenas Frontend

```bash
npm run dev:frontend
```

### Apenas Backend

```bash
npm run dev:backend
```

### Build para Producao

```bash
npm run build
```

## Portas

| Servico  | Porta |
|----------|-------|
| Backend  | 8889  |
| Frontend | 8888  |

## Estrutura do Projeto

```
atas-registro-precos-sms/
├── backend/                 # API .NET 9
│   ├── Controllers/         # Endpoints da API
│   ├── Data/                # DbContext e configuracoes
│   ├── Migrations/          # Migrations do EF Core
│   ├── Models/              # Entidades e DTOs
│   ├── Services/            # Logica de negocio
│   ├── appsettings.example.json  # Template de configuracoes
│   └── Program.cs           # Entry point
│
├── frontend/                # Aplicacao React
│   ├── src/
│   │   ├── components/      # Componentes reutilizaveis
│   │   ├── pages/           # Paginas da aplicacao
│   │   ├── services/        # Integracao com API
│   │   └── types/           # Tipos TypeScript
│   ├── index.html
│   └── vite.config.ts
│
├── package.json             # Configuracao do monorepo
└── README.md
```

## Funcionalidades

- **Dashboard**: Visao geral com estatisticas e alertas
- **Atas**: Listagem e detalhes de atas de registro de preco
- **Materiais**: Consulta de itens do tipo material
- **Servicos**: Consulta de itens do tipo servico
- **Pesquisa**: Busca avancada de atas e itens
- **Alertas**: Notificacoes de atas proximas do vencimento
- **Configuracoes**: Parametros do sistema

## Scripts Disponiveis

| Comando              | Descricao                              |
|----------------------|----------------------------------------|
| `npm run dev`        | Inicia backend e frontend              |
| `npm run dev:frontend` | Inicia apenas o frontend             |
| `npm run dev:backend`  | Inicia apenas o backend              |
| `npm run build`      | Gera build de producao do frontend     |
| `npm run lint`       | Executa linter no frontend             |

## API Endpoints

A API esta disponivel em `http://localhost:8889/api/`

- `GET /api/atas` - Lista todas as atas
- `GET /api/atas/{id}` - Detalhes de uma ata
- `GET /api/itens` - Lista todos os itens
- `GET /api/dashboard` - Dados do dashboard
- `POST /api/sincronizacao` - Sincroniza dados externos

## Licenca

Este projeto e privado e de uso interno da Secretaria Municipal de Saude.
