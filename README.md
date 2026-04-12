# TrackUniversity 🚌

Monitoreo en tiempo real de rutas de transporte universitario.

## Arquitectura

```
[Estudiante] → [UI Astro] ──HTTP/JSON──→ [Backend .NET]  ──MySQL──→ [Base de datos]
                                               ↑
                                          subscribe MQTT
                                               ↑
                               [Broker Mosquitto / HiveMQ]
                                               ↑
                                    publish MQTT cada 5s
                                               ↑
                               [Simulador Python (bus GPS)]
```

| Componente | Tecnología | Puerto |
|---|---|---|
| Frontend | Astro + React + Leaflet | 4321 |
| Backend | .NET 8 ASP.NET Core + Worker | 5000 |
| Base de datos | MySQL 8 | 3306 |
| Broker (local) | Mosquitto 2 | 1883 / 9001(WS) |

---

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js ≥ 18](https://nodejs.org/)
- [Python 3.11+](https://www.python.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) *(para MySQL + Mosquitto locales)*

---

## Inicio rápido (local con Docker)

### 1. Levantar MySQL + Mosquitto

```bash
docker-compose up -d
```

Espera ~15 s hasta que el contenedor `trackuniversity-db` responda.

### 2. Backend .NET

```bash
cd backend/TrackUniversity
dotnet restore
dotnet run
# API disponible en http://localhost:5000
```

Al arrancar, `.NET` crea las tablas automáticamente y siembra 2 buses en la BD.

### 3. Frontend Astro

```bash
cd frontend
cp .env.example .env        # ya tiene PUBLIC_API_URL=http://localhost:5000
npm install
npm run dev
# UI disponible en http://localhost:4321
```

### 4. Simulador GPS

```bash
cd simulator
pip install -r requirements.txt
python bus_simulator.py
# Publica coordenadas en universidad/bus/ruta1 cada 5 s
```

Verás el marcador del bus moverse en el mapa cada ~5 segundos.

---

## Usar HiveMQ Cloud (broker en la nube) en lugar de Mosquitto local

HiveMQ Cloud tiene un plan gratuito que no requiere ninguna instalación.

### Pasos

1. Crear cuenta en <https://console.hivemq.cloud> → crear un **Free Cluster**.
2. En el cluster, ir a **Access Management** → crear credenciales (usuario + contraseña).
3. Copiar el **Cluster URL** (algo como `abc123.s2.eu.hivemq.cloud`).

### Configurar el backend

Edita `backend/TrackUniversity/appsettings.json`:

```json
"Mqtt": {
  "Host": "abc123.s2.eu.hivemq.cloud",
  "Port": 8883,
  "UseTls": true,
  "Username": "tu_usuario",
  "Password": "tu_contraseña"
}
```

### Configurar el simulador

```bash
# PowerShell
$env:MQTT_HOST     = "abc123.s2.eu.hivemq.cloud"
$env:MQTT_PORT     = "8883"
$env:MQTT_USE_TLS  = "true"
$env:MQTT_USERNAME = "tu_usuario"
$env:MQTT_PASSWORD = "tu_contraseña"
python bus_simulator.py

# Bash
MQTT_HOST=abc123.s2.eu.hivemq.cloud MQTT_PORT=8883 \
MQTT_USE_TLS=true MQTT_USERNAME=tu_usuario MQTT_PASSWORD=tu_contraseña \
python bus_simulator.py
```

> Con HiveMQ puedes **detener Mosquitto y MySQL en Docker**; sólo necesitarás el contenedor de MySQL para la BD.

---

## Estructura del proyecto

```
TrackUniversity/
├── docker-compose.yml          # MySQL + Mosquitto locales
├── mosquitto/
│   └── config/mosquitto.conf   # Configuración del broker
│
├── simulator/
│   ├── bus_simulator.py        # Script Python: publica GPS falso por MQTT
│   └── requirements.txt
│
├── backend/
│   └── TrackUniversity/
│       ├── Controllers/
│       │   └── BusController.cs       # GET /api/bus, /api/bus/{id}/position, /history
│       ├── Data/
│       │   └── AppDbContext.cs        # Entity Framework (MySQL)
│       ├── Models/
│       │   ├── Bus.cs
│       │   └── BusReading.cs
│       ├── Services/
│       │   └── MqttWorkerService.cs   # Background Service: subscribe MQTT → guarda en BD
│       ├── Program.cs
│       ├── appsettings.json           # Config local (Mosquitto + MySQL Docker)
│       └── appsettings.HiveMQ.json   # Ejemplo config HiveMQ Cloud
│
└── frontend/
    ├── src/
    │   ├── pages/
    │   │   └── index.astro            # Página principal (contenido estático + isla)
    │   └── components/
    │       └── BusMap.jsx             # 🏝 Isla React: mapa Leaflet con polling
    ├── astro.config.mjs
    └── .env.example
```

---

## API del backend

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/bus` | Lista todos los buses con su última posición |
| GET | `/api/bus/{id}/position` | Posición actual de un bus |
| GET | `/api/bus/{id}/history?limit=50` | Historial de lecturas GPS |

---

## Tópico MQTT

| Tópico | Publicado por | Suscrito por |
|--------|---------------|--------------|
| `universidad/bus/ruta1` | Simulador Python | Backend .NET |
| `universidad/bus/ruta2` | (ampliar) | Backend .NET |

Formato del mensaje:
```json
{ "lat": 4.628400, "lng": -74.064100 }
```

---

## Cómo justificar el stack ante el profesor

| Decisión | Justificación |
|----------|---------------|
| **Pub/Sub** | El bus publica sin saber cuántos clientes están conectados. Desacoplamiento total productor/consumidor. |
| **MQTT** | Protocolo ligero (cabecera mínima) diseñado para IoT. QoS 1 garantiza entrega al menos una vez. |
| **Worker Service .NET** | Background Service que escucha MQTT indefinidamente sin bloquear el hilo del servidor HTTP. |
| **Entity Framework** | Abstracción ORM que evita SQL manual; `EnsureCreated()` genera el schema en arranque. |
| **MySQL** | Almacenamiento histórico de coordenadas con índice compuesto `(BusId, Timestamp)` para consultas eficientes. |
| **Astro Islands** | El 90 % del HTML es estático (0 JS). Sólo el mapa se hidrata → menor bundle, mejor rendimiento. |
| **Leaflet / OpenStreetMap** | Gratuito, sin API key, funciona offline con tiles cacheados. |
