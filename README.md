# Ascensores Iruña

Una aplicación web para consultar el estado de los ascensores públicos de Pamplona y compartir información útil sobre su funcionamiento.

![Vista previa de Ascensores Iruña](./docs/preview.svg)

## ¿Qué puedes hacer?

- Consultar en el mapa dónde están los ascensores públicos.
- Revisar su estado y los últimos reportes.
- Buscar un ascensor por nombre.
- Enviar o actualizar un reporte.
- Usar la web desde el móvil o el ordenador.

## Tecnologías

- **Frontend:** Angular 22 y TypeScript
- **Backend:** ASP.NET Core 10
- **Base de datos:** SQLite
- **Despliegue:** Docker y Docker Compose

## Ejecutar con Docker

Abre Docker Desktop y espera a que el motor esté en marcha. Desde la carpeta raíz del repositorio, ejecuta:

```powershell
docker compose up --build
```

Después, abre [http://localhost:8080](http://localhost:8080).

Para detener la aplicación, pulsa `Ctrl+C` y ejecuta:

```powershell
docker compose down
```

## Ejecutar en local

Necesitas Node.js y npm para el frontend, y .NET 10 SDK para el backend. Abre dos terminales desde la raíz del repositorio.

**Terminal 1 — backend:**

```powershell
cd src/backend/AscensoresIruna.Api
dotnet run
```

**Terminal 2 — frontend:**

```powershell
cd src/frontend
npm install
npm start
```

El frontend estará disponible normalmente en [http://localhost:4200](http://localhost:4200).

## Estructura

```text
├── docs/                 # Recursos del README
├── src/
│   ├── backend/          # API ASP.NET Core y pruebas
│   └── frontend/         # Aplicación Angular
├── Dockerfile
└── docker-compose.yaml
```

## Contacto

Para sugerencias, la aplicación muestra el correo de contacto en su cabecera.
