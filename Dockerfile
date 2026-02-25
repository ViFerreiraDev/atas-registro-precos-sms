# ============================================
# Stage 1: Build Frontend (React + Vite)
# ============================================
FROM node:22-alpine AS build-frontend
WORKDIR /app/frontend

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

# ============================================
# Stage 2: Build Backend (.NET 9)
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build-backend
WORKDIR /src

COPY backend/AtasApi.csproj ./
RUN dotnet restore

COPY backend/ ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ============================================
# Stage 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Copy published backend
COPY --from=build-backend /app/publish ./

# Copy frontend build to wwwroot
COPY --from=build-frontend /app/frontend/dist ./wwwroot/

# Environment
ENV ASPNETCORE_URLS=http://+:8889
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8889

ENTRYPOINT ["dotnet", "AtasApi.dll"]
