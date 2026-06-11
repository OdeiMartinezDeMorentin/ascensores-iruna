# AGENTS.md — AscensoresIruña

## Descripción del proyecto

Web colaborativa donde los usuarios pueden consultar y reportar el estado de los ascensores públicos de Pamplona. El objetivo es que cualquier persona pueda saber si un ascensor está operativo antes de desplazarse, evitando retrasos por averías imprevistas.

## Stack técnico

- **Backend:** ASP.NET Core Web API (.NET 10)
- **Frontend:** Angular 22 (standalone components, signals como patrón principal de reactividad)
- **Base de datos:** SQLite (Entity Framework Core, migraciones)
- **Mapas:** Leaflet con OpenStreetMap
- **Despliegue:** Contenedor Docker único (Angular compilado servido desde `wwwroot` de .NET). Objetivo: Railway o Fly.io.
- **Idioma del código:** Inglés para nombres de variables, clases, métodos y archivos.
- **Idioma del UI:** Español.

## Fases del proyecto

### Fase 1 — MVP (lista + reportar) [COMPLETADA]
- ✅ Lista vertical de ascensores con icono de estado (verde/amarillo/rojo).
- ✅ Botón para reportar avería o cambio de estado en cada ascensor.
- ✅ Estado del ascensor calculado a partir de reportes (mayoría ponderada con decaimiento temporal y trust score).
- ✅ Seed con los ascensores públicos de Pamplona predefinidos.
- ✅ No se requiere registro de usuario.

### Fase 2 — Mapa interactivo [COMPLETADA]
- ✅ Mapa de Pamplona (Leaflet + OpenStreetMap) en la parte superior de la página.
- ✅ Iconos interactivos sobre cada ascensor: verde (operativo), amarillo (parcialmente operativo), rojo (averiado).
- ✅ Hover/click en icono muestra popup con info y botón de reportar.
- ✅ Debajo del mapa se mantiene la lista vertical de todos los ascensores.

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

## Modelos de datos

### Elevator
- Id (int, PK)
- Name (string, required)
- Location (string, required)
- Latitude (double)
- Longitude (double)

### ElevatorStatus (enum)
- Operativo
- Parcial
- Averiado
- Desconocido — se muestra cuando no hay reportes

### StatusReport
- Id (int, PK)
- ElevatorId (int, FK)
- Status (enum: Operativo, Parcial, Averiado)
- ReportedAt (DateTime) — timestamp en zona horaria de España
- IpAddressHash (string) — HMAC-SHA256 de la IP del usuario

### ReporterIp
- IpAddressHash (string, PK) — HMAC-SHA256 de la IP
- TrustScore (double, default 1.0, rango [0.1, 3.0])
- Confirmations (int)
- Contradictions (int)
- LastSeenAt (DateTime)

### Estado del ascensor — Algoritmo de mayoría ponderada
El estado actual se calcula con los reportes de las últimas 2 horas:
1. Cada reporte tiene un peso = multiplicador_temporal × trust_score_IP
2. Multiplicadores temporales: 0-20 min → ×3, 20-60 min → ×2, 60-120 min → ×1
3. El estado con mayor peso total es el mostrado
4. Excepción: el primer reporte de un ascensor (estado Desconocido) se acepta directamente sin pesar

### Trust score
- IP nueva empieza con trust 1.0 (neutral)
- Cada confirmación (otra IP reporta el mismo estado en 30 min) suma 0.2
- Cada contradicción (otra IP reporta estado distinto en 30 min) resta 0.3
- Fórmula: clamp(1.0 + 0.2 × confirmaciones - 0.3 × contradicciones, 0.1, 3.0)
- Las IPs se almacenan hasheadas con HMAC-SHA256 usando un secreto del servidor

### Rate limiting
- 1 reporte por ascensor por IP cada 10 minutos
- Máximo 3 ascensores distintos por IP cada 10 minutos
- Posibilidad de editar el reporte dentro de los 10 minutos (PUT)

## Endpoints API

- `GET /api/elevators` — Lista todos los ascensores con su estado actual (ponderado) y `canReport`.
- `GET /api/elevators/{id}` — Detalle de un ascensor con su estado actual y `canReport`.
- `POST /api/elevators/{id}/reports` — Crea un nuevo reporte de estado.
  - Body: `{ "status": "Operativo"|"Parcial"|"Averiado" }`
  - Devuelve 429 si se excede el rate limit.
- `PUT /api/elevators/{id}/reports/latest` — Edita el último reporte de la IP en ese ascensor (dentro de 10 min).
  - Body: `{ "status": "Operativo"|"Parcial"|"Averiado" }`
- `GET /api/elevators/{id}/reports/my-latest` — Devuelve el último reporte de la IP en ese ascensor (204 si no hay).

## Estructura de carpetas

```
ascensores-iruna/
├── AGENTS.md
├── .gitignore
├── src/
│   ├── backend/
│   │   ├── AscensoresIruna.Api/
│   │   │   ├── AscensoresIruna.Api.csproj
│   │   │   ├── Program.cs
│   │   │   ├── Controllers/
│   │   │   │   └── ElevatorsController.cs
│   │   │   ├── Models/
│   │   │   │   ├── Elevator.cs
│   │   │   │   ├── ElevatorStatus.cs
│   │   │   │   ├── ReporterIp.cs
│   │   │   │   └── StatusReport.cs
│   │   │   ├── Data/
│   │   │   │   ├── AppDbContext.cs
│   │   │   │   └── SeedData.cs
│   │   │   ├── DTOs/
│   │   │   │   ├── CreateReportDto.cs
│   │   │   │   ├── ElevatorDto.cs
│   │   │   │   ├── MyLatestReportDto.cs
│   │   │   │   ├── StatusReportDto.cs
│   │   │   │   └── UpdateReportDto.cs
│   │   │   ├── Services/
│   │   │   │   ├── ElevatorStatusService.cs
│   │   │   │   ├── IpHashService.cs
│   │   │   │   └── TrustScoreService.cs
│   │   │   └── Migrations/
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
│       │   │   │   ├── elevator-map/
│       │   │   │   └── report-dialog/
│       │   │   ├── services/
│       │   │   │   └── elevator.service.ts
│       │   │   ├── models/
│       │   │   │   └── elevator.model.ts
│       │   │   └── app.component.ts
│       │   └── ...
│       └── ...
└── data/  (SQLite db en runtime, gitignored)
```

## Convenciones

- **Commits:** En inglés, formato convencional (`feat:`, `fix:`, `chore:`, etc.).
- **No añadir comentarios en el código** salvo que se pida explícitamente.
- **No hacer commit de secretos** ni archivos de base de datos.
- **appsettings.Development.json** está en `.gitignore` — contiene el HMAC secret y otros valores locales.
- **Estilo código C#:** Seguir convenciones de .NET (PascalCase para clases/métodos, camelCase para variables locales/parámetros, llaves en nueva línea).
- **Estilo código Angular/TS:** Seguir Angular Style Guide (camelCase para servicios/métodos, PascalCase para componentes/interfaces).
- **Tests:** Escribir tests para la lógica del backend. Para el frontend, tests unitarios de servicios como mínimo.

## Comandos

### Backend (.NET)
```bash
cd src/backend/AscensoresIruna.Api
dotnet build
dotnet run
dotnet ef database update   # Aplicar migraciones
dotnet ef migrations add <Name>  # Crear nueva migración
```

### Frontend (Angular)
```bash
cd src/frontend
npm install
npm start        # ng serve
npm run build    # ng build --configuration production
```

### Docker
```bash
docker compose up --build
```

## Ascensores públicos de Pamplona (seed)

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

## Notas importantes

- No se exige registro de usuario para consultar ni reportar. En una fase futura se podría añadir.
- El estado de un ascensor se basa en un algoritmo de mayoría ponderada con trust score y decaimiento temporal.
- Las IPs se almacenan hasheadas con HMAC-SHA256 para proteger la privacidad (GDPR).
- La base de datos SQLite es suficiente para una única instancia. Una migración a PostgreSQL sería directa cambiando la configuración de EF Core si se necesita escalabilidad.
- Se usa EF Core Migrations (no EnsureCreated) para gestionar cambios de esquema.