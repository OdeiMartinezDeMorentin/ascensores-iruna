# AGENTS.md — AscensoresIruña

## Descripción del proyecto

Web colaborativa donde los usuarios pueden consultar y reportar el estado de los ascensores públicos de Pamplona. El objetivo es que cualquier persona pueda saber si un ascensor está operativo antes de desplazarse, evitando retrasos por averías imprevistas.

## Stack técnico

- **Backend:** ASP.NET Core Web API (.NET 10)
- **Frontend:** Angular 22 (standalone components, signals como patrón principal de reactividad)
- **Base de datos:** SQLite (Entity Framework Core)
- **Mapas (fase 2):** Leaflet con OpenStreetMap
- **Despliegue:** Contenedor Docker único (Angular compilado servido desde `wwwroot` de .NET). Objetivo: Railway o Fly.io.
- **Idioma del código:** Inglés para nombres de variables, clases, métodos y archivos.
- **Idioma del UI:** Español.

## Fases del proyecto

### Fase 1 — MVP (lista + reportar) [ACTUAL]
- Lista vertical de ascensores con icono de estado (verde/amarillo/rojo).
- Botón para reportar avería o cambio de estado en cada ascensor.
- Estado del ascensor calculado a partir del último reporte.
- Seed con los ascensores públicos de Pamplona predefinidos.
- No se requiere registro de usuario.

### Fase 2 — Mapa interactivo
- Mapa de Pamplona (Leaflet + OpenStreetMap) en la parte superior de la página.
- Iconos interactivos sobre cada ascensor: verde (operativo), amarillo (parcialmente operativo), rojo (averiado).
- Al tocar un icono se puede reportar el estado.
- Debajo del mapa se mantiene la lista vertical de todos los ascensores.

### Fase 3 — Historial y estadísticas
- Panel por ascensor con historial de estados/reportes.
- Estadísticas de disponibilidad por ascensor.

## Diseño UI final

```
+-----------------------------+
|          Mapa               |
|   (iconos por ascensor)     |
+-----------------------------+
|   Lista vertical de         |
|   ascensores con estado     |
+-----------------------------+
```

En la Fase 1 solo existe la lista. En la Fase 2 se añade el mapa encima.

## Modelos de datos

### Elevator
- Id (int, PK)
- Name (string, required) — nombre común del ascensor
- Location (string, required) — dirección o zona
- Latitude (double)
- Longitude (double) — necesarios para la Fase 2

### ElevatorStatus (enum)
- Operativo
- Parcial
- Averiado
- Desconocido — se muestra cuando no hay reportes

### StatusReport
- Id (int, PK)
- ElevatorId (int, FK)
- Status (enum: Operativo, Parcial, Averiado)
- ReportedAt (DateTime) — timestamp del reporte

### Estado del ascensor
El estado actual de un ascensor se determina por el `StatusReport` más reciente asociado a ese ascensor.

## Endpoints API

- `GET /api/elevators` — Lista todos los ascensores con su estado actual.
- `GET /api/elevators/{id}` — Detalle de un ascensor con su estado actual.
- `POST /api/elevators/{id}/reports` — Crea un nuevo reporte de estado para un ascensor.
  - Body: `{ "status": "Operativo"|"Parcial"|"Averiado" }`

## Estructura de carpetas (objetivo)

```
ascensoresiruña/
├── AGENTS.md
├── .gitignore
├── .dockerignore
├── Dockerfile
├── docker-compose.yml
├── README.md
├── src/
│   ├── backend/
│   │   ├── AscensoresIruna.Api/
│   │   │   ├── AscensoresIruna.Api.csproj
│   │   │   ├── Program.cs
│   │   │   ├── Controllers/
│   │   │   ├── Models/
│   │   │   ├── Data/
│   │   │   │   ├── AppDbContext.cs
│   │   │   │   └── SeedData.cs
│   │   │   └── DTOs/
│   │   ├── AscensoresIruna.Api.sln
│   │   └── ...
│   └── frontend/
│       ├── package.json
│       ├── angular.json
│       ├── src/
│       │   ├── app/
│       │   │   ├── components/
│       │   │   │   ├── elevator-list/
│       │   │   │   ├── elevator-card/
│       │   │   │   └── report-dialog/
│       │   │   ├── services/
│       │   │   ├── models/
│       │   │   └── app.component.ts
│       │   └── ...
│       └── ...
└── data/  (SQLite db en runtime, gitignored)
```

## Convenciones

- **Commits:** En inglés, formato convencional (`feat:`, `fix:`, `chore:`, etc.).
- **No añadir comentarios en el código** salvo que se pida explícitamente.
- **No hacer commit de secretos** ni archivos de base de datos.
- **Estilo código C#:** Seguir convenciones de .NET (PascalCase para clases/métodos, camelCase para variables locales/parámetros, llaves en nueva línea).
- **Estilo código Angular/TS:** Seguir Angular Style Guide (camelCase para servicios/métodos, PascalCase para componentes/interfaces).
- **Tests:** Escribir tests para la lógica del backend. Para el frontend, tests unitarios de servicios como mínimo.

## Comandos

### Backend (.NET)
```bash
cd src/backend/AscensoresIruna.Api
dotnet build
dotnet run
dotnet test
```

### Frontend (Angular)
```bash
cd src/frontend
npm install
npm start        # ng serve
npm run build    # ng build --configuration production
npm test         # ng test
npm run lint     # ng lint
```

### Docker
```bash
docker compose up --build
```

## Ascensores públicos de Pamplona (seed)

Incluir en el seed inicial los siguientes ascensores (lista no exhaustiva, ampliable):

- Ascensor Plaza del Castillo - Conexión con parking
- Ascensor Azucarera - Conexión calle Baja/Baja ikastola
- Ascensor Lindach - Conexión con Rochapea
- Ascensor Conde Oliveto - Conexión con Ensanche
- Ascensor Dominicales - Conexión zonamundi
- Ascensor Labrit - Conexión con barrio
- Ascensor San Valentín - Conexión con Iturrama
- Ascensor Yamaguchi - Conexión con San Juan
- Ascensor Ermitaña - Conexión con Ermitagaña
- Ascensor Mgica - Conexión con Segundo Ensanche

(Verificar y completar coordenadas GPS exactas en el seed.)

## Notas importantes

- No se exige registro de usuario para consultar ni reportar. En una fase futura se podría añadir.
- El estado de un ascensor se basa exclusivamente en reportes de usuarios (no hay integración con ayuntamiento).
- La base de datos SQLite es suficiente para una única instancia. Una migración a PostgreSQL sería directa cambiando la configuración de EF Core si se necesita escalabilidad.