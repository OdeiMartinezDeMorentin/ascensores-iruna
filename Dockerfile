# --- Etapa 1: Build de Angular ---
FROM node:22-alpine AS build-frontend
WORKDIR /app/frontend

# Instalar dependencias
COPY src/frontend/package*.json ./
RUN npm ci

# Copiar el código fuente y compilar
COPY src/frontend/ ./
RUN npx ng build --configuration production

# --- Etapa 2: Build de .NET ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-backend
WORKDIR /app/backend

# Restaurar dependencias
COPY src/backend/AscensoresIruna.Api/AscensoresIruna.Api.csproj ./AscensoresIruna.Api/
RUN dotnet restore ./AscensoresIruna.Api/AscensoresIruna.Api.csproj

# Copiar el código fuente y publicar
COPY src/backend/ ./
RUN dotnet publish ./AscensoresIruna.Api/AscensoresIruna.Api.csproj -c Release -o /app/out

# --- Etapa 3: Imagen Final de Producción ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copiar los binarios del backend
COPY --from=build-backend /app/out .

# Copiar el build de Angular a la carpeta wwwroot de .NET
# *Nota: Ajusta 'frontend/browser' si el output path en tu angular.json es distinto*
COPY --from=build-frontend /app/frontend/dist/frontend/browser ./wwwroot

# Configurar variables de entorno por defecto
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "AscensoresIruna.Api.dll"]