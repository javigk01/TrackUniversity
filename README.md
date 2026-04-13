# TrackUniversity

Monitoreo en tiempo real de rutas de transporte universitario.

Sistema que permite visualizar la ubicación, velocidad y estado de los buses universitarios sobre un mapa interactivo, usando MQTT para telemetría en tiempo real.

---

## Stack tecnológico

| Componente | Tecnología | Puerto |
|---|---|---|
| Frontend | Astro 4 + React 18 + Leaflet | 4321 |
| Backend | .NET 8 ASP.NET Core + Worker Service | 5000 |
| Base de datos | MySQL 8 | 3306 |
| Broker MQTT | Mosquitto 2 | 1883 (TCP) / 9001 (WS) |
| Simulador GPS | Python 3 + paho-mqtt | — |

---

## Estructura del proyecto

```
TrackUniversity/
├── .gitignore
├── docker-compose.yml
├── mosquitto/
│   └── config/
│       └── mosquitto.conf          # Configuración del broker
├── simulator/
│   ├── bus_simulator.py            # Simula GPS y velocidad, publica por MQTT
│   ├── requirements.txt
│   └── Dockerfile
├── backend/
│   └── TrackUniversity/
│       ├── Controllers/
│       │   ├── BusController.cs    # GET /api/bus  /api/bus/{id}  /position  /history
│       │   └── RoutesController.cs # GET /api/routes  /api/routes/{id}  /buses
│       ├── Data/
│       │   └── AppDbContext.cs     # EF Core (MySQL)
│       ├── Models/
│       │   ├── Bus.cs              # Entidad bus (placa, capacidad, última posición)
│       │   ├── BusReading.cs       # Historial de telemetría
│       │   ├── Route.cs            # Ruta (origen, destino, código MQTT)
│       │   └── RouteStop.cs        # Paradas de una ruta
│       ├── Services/
│       │   └── MqttWorkerService.cs # Suscriptor MQTT → persiste en BD
│       ├── Program.cs              # Composición + datos semilla
│       ├── appsettings.json        # Config local
│       ├── TrackUniversity.csproj
│       └── Dockerfile
└── frontend/
    ├── src/
    │   ├── pages/
    │   │   └── index.astro         # Página principal
    │   └── components/
    │       └── BusMap.jsx          # Isla React: mapa con marcadores en tiempo real
    ├── .env.example                # Plantilla de variables de entorno
    ├── astro.config.mjs
    ├── package.json
    └── Dockerfile
```

---

## Archivos excluidos por .gitignore (debes crearlos tú)

Estos archivos **no están en el repositorio** y son necesarios para ejecutar el proyecto:

### `frontend/.env`

Cópialo desde la plantilla:

```powershell
# Opción A — Docker completo: el navegador llama al backend en localhost:5000
copy frontend\.env.example frontend\.env

# Opción B — Local híbrido: igual, PUBLIC_API_URL=http://localhost:5000
copy frontend\.env.example frontend\.env
```

Contenido del archivo:

```env
PUBLIC_API_URL=http://localhost:5000
```

> Sin este archivo Astro lanza un error al compilar, porque `import.meta.env.PUBLIC_API_URL` queda `undefined`.

---

## Cómo correr el proyecto

Hay dos modos. Usa el que prefieras.

---

### Opción A — Docker completo (todo en contenedores)

Requiere: **Docker Desktop corriendo**.

```powershell
docker compose up -d --build
```

Esto levanta todos los servicios:
1. `mysql` — base de datos
2. `mosquitto` — broker MQTT
3. `backend` — API .NET
4. `frontend` — UI Astro compilada con Nginx
5. `simulator-ruta1..ruta6` — simuladores GPS por ruta

Señales de que todo está listo en los logs:

```
trackuniversity-backend   | [MQTT] Conectado a mosquitto:1883
trackuniversity-backend   | Datos semilla insertados
trackuniversity-simulator-ruta1 | [SIM] [0001] universidad/bus/TUB-001 → {"lat":...}
```

Acceder en:
- Frontend: http://localhost:4321
- API: http://localhost:5000/api/bus

Para detener:

```powershell
docker compose down
```

Para detener y borrar los volúmenes (BD limpia):

```powershell
docker compose down -v
```

---

### Opción B — Demo compartida con ngrok (link público temporal)

Útil para mostrar el proyecto a otras personas sin necesidad de un servidor.
Requiere: **Docker Desktop corriendo** + [ngrok](https://ngrok.com) instalado y con cuenta creada.

> El nginx del frontend hace proxy de `/api/` al backend internamente, por lo que **solo se necesita un túnel** apuntando al puerto del frontend.

```powershell
# 1. Levantar todos los contenedores
docker compose up -d --build

# 2. Exponer el frontend públicamente (en otra terminal)
ngrok http 4321
```

Ngrok mostrará una URL del tipo:
```
Forwarding   https://xxxx-xxxx-xxxx.ngrok-free.app -> http://localhost:4321
```

Comparte esa URL. La app completa (mapa, gráficas, API) funcionará a través de ese único link.

**Notas:**
- La URL cambia cada vez que se reinicia ngrok (plan gratis)
- Los visitantes verán una pantalla de advertencia de ngrok la primera vez; deben hacer clic en "Visit Site"
- El panel de administración de ngrok está en http://localhost:4040

---

### Opción C — Local híbrido (MySQL + Mosquitto en Docker, el resto local)

Requiere: **Docker Desktop corriendo** + .NET 8 SDK + Node.js ≥ 18 + Python 3 (`py` en Windows).

#### Paso 1 — Levantar MySQL y Mosquitto

```powershell
docker-compose up mysql mosquitto -d
```

Espera ~15 segundos hasta que ambos contenedores estén `healthy`:

```powershell
docker ps
# trackuniversity-db     (healthy)
# trackuniversity-broker (healthy)
```

#### Paso 2 — Backend .NET

Abre una terminal en `backend/TrackUniversity/`:

```powershell
cd backend\TrackUniversity
dotnet restore
dotnet run
```

Espera ver:

```
[MQTT] Conectado a localhost:1883
Datos semilla insertados
Now listening on: http://localhost:5000
```

> La primera vez crea las tablas y siembra 2 rutas, 9 paradas y 2 buses automáticamente.

#### Paso 3 — Frontend Astro

Abre **otra terminal** en `frontend/`:

```powershell
cd frontend

# Solo la primera vez:
copy .env.example .env
npm install

# Siempre:
npm run dev
```

Acceder en: http://localhost:4321

#### Paso 4 — Simulador GPS (una terminal por bus)

Abre **otra terminal** en `simulator/`:

```powershell
cd simulator

# Solo la primera vez:
py -m pip install -r requirements.txt

# Bus 1:
$env:MQTT_HOST="localhost"; $env:BUS_PLATE="TUB-001"; $env:BUS_ROUTE="ruta1"; py bus_simulator.py
```

Para un segundo bus, abre **otra terminal**:

```powershell
cd simulator
$env:MQTT_HOST="localhost"; $env:BUS_PLATE="TUB-002"; $env:BUS_ROUTE="ruta2"; py bus_simulator.py
```

Verás en consola:

```
[MQTT] Conectando a localhost:1883 ...
[SIM] Bus 'TUB-001' en ruta 'ruta1'
[SIM] [0001] universidad/bus/TUB-001 → {"lat": 4.628400, "lng": -74.064100, "speed": 32.5}
[SIM] [0002] universidad/bus/TUB-001 → {"lat": 4.628510, "lng": -74.063980, "speed": 31.8}
```

El mapa en http://localhost:4321 actualizará la posición del bus cada 5 segundos.

---

## Endpoints del API

### Buses

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/bus` | Todos los buses con info de ruta |
| GET | `/api/bus/{id}` | Detalle completo de un bus |
| GET | `/api/bus/{id}/position` | Última posición y velocidad |
| GET | `/api/bus/{id}/history?limit=50` | Historial de telemetría |

### Rutas

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/routes` | Todas las rutas con paradas y buses |
| GET | `/api/routes/{id}` | Detalle de una ruta |
| GET | `/api/routes/{id}/buses` | Buses activos en una ruta |

---

## Tópicos MQTT

El simulador publica y el backend .NET suscribe al patrón:

```
universidad/bus/{PLACA}
```

Ejemplos:
- `universidad/bus/TUB-001`
- `universidad/bus/TUB-002`

Payload JSON:

```json
{ "lat": 4.628400, "lng": -74.064100, "speed": 32.5 }
```

---

## Variables de entorno

### Frontend (`frontend/.env`) — **no está en el repo**

| Variable | Valor por defecto | Descripción |
|---|---|---|
| `PUBLIC_API_URL` | `http://localhost:5000` | URL base del backend |

### Simulador

| Variable | Default | Descripción |
|---|---|---|
| `MQTT_HOST` | `mosquitto` | Host del broker |
| `MQTT_PORT` | `1883` | Puerto TCP |
| `BUS_PLATE` | `TUB-001` | Placa del bus a simular |
| `BUS_ROUTE` | `ruta1` | Ruta a simular (`ruta1` o `ruta2`) |

### Backend (en Docker lo inyecta `docker-compose.yml`; en local usa `appsettings.json`)

| Variable | Descripción |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión MySQL |
| `Mqtt__Host` | Host del broker MQTT |
| `Mqtt__Port` | Puerto del broker MQTT |

---

## Datos semilla

Al arrancar por primera vez, el backend inserta automáticamente:

- **Ruta 1** — Entrada Principal → Bosa Terminal (5 paradas, ~45 min)
  - Bus: **TUB-001** (capacidad 40)
- **Ruta 2** — Campus Sur → Portal Norte (4 paradas, ~35 min)
  - Bus: **TUB-002** (capacidad 35)
