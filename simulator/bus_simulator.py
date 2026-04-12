"""
bus_simulator.py
Simula el GPS y telemetría de un bus universitario y publica al broker MQTT
cada 5 segundos.

Uso:
  python bus_simulator.py

Variables de entorno:
  MQTT_HOST     — host del broker         (default: mosquitto)
  MQTT_PORT     — puerto del broker        (default: 1883)
  BUS_PLATE     — placa del bus a simular  (default: TUB-001)
  BUS_ROUTE     — clave de ruta            (default: ruta1)

Tópico publicado: universidad/bus/{BUS_PLATE}
Payload:          {"lat": 4.628400, "lng": -74.064100, "speed": 35.5}
"""

import json
import math
import os
import random
import time

import paho.mqtt.client as mqtt

# ── Configuración ──────────────────────────────────────────────────────────────
BROKER_HOST = os.getenv("MQTT_HOST", "mosquitto")
BROKER_PORT = int(os.getenv("MQTT_PORT", "1883"))
BUS_PLATE   = os.getenv("BUS_PLATE", "TUB-001")
BUS_ROUTE   = os.getenv("BUS_ROUTE", "ruta1")

PUBLISH_INTERVAL = 5  # segundos

# ── Waypoints por ruta ─────────────────────────────────────────────────────────
ROUTES: dict[str, list[dict]] = {
    "ruta1": [
        {"lat": 4.6284, "lng": -74.0641},  # Entrada Principal
        {"lat": 4.6301, "lng": -74.0660},  # Biblioteca Central
        {"lat": 4.6325, "lng": -74.0672},  # Parada Norte
        {"lat": 4.6348, "lng": -74.0658},  # Cruce Intermedio
        {"lat": 4.6361, "lng": -74.0630},  # Terminal Norte
        {"lat": 4.6340, "lng": -74.0605},  # Retorno
        {"lat": 4.6310, "lng": -74.0610},  # Biblioteca (vuelta)
        {"lat": 4.6284, "lng": -74.0641},  # Entrada Principal (cierre)
    ],
    "ruta2": [
        {"lat": 4.6100, "lng": -74.1100},  # Campus Sur
        {"lat": 4.6120, "lng": -74.1080},  # Cafetería
        {"lat": 4.6200, "lng": -74.1050},  # Kennedy
        {"lat": 4.6250, "lng": -74.1800},  # Bosa Terminal
        {"lat": 4.6200, "lng": -74.1050},  # Kennedy (vuelta)
        {"lat": 4.6120, "lng": -74.1080},  # Cafetería (vuelta)
        {"lat": 4.6100, "lng": -74.1100},  # Campus Sur (cierre)
    ],
}

INTERPOLATION_STEPS = 20


# ── Utilidades ─────────────────────────────────────────────────────────────────
def interpolate_route(waypoints: list[dict], steps: int) -> list[dict]:
    """Genera una secuencia suavizada de coordenadas entre los waypoints."""
    points: list[dict] = []
    for i in range(len(waypoints) - 1):
        start = waypoints[i]
        end   = waypoints[i + 1]
        for step in range(steps):
            t = step / steps
            points.append({
                "lat": start["lat"] + t * (end["lat"] - start["lat"]),
                "lng": start["lng"] + t * (end["lng"] - start["lng"]),
            })
    return points


def estimate_speed(prev: dict | None, curr: dict, interval_s: float) -> float:
    """Calcula velocidad aproximada en km/h entre dos puntos GPS."""
    if prev is None:
        return round(random.uniform(25, 45), 1)
    dlat = math.radians(curr["lat"] - prev["lat"])
    dlng = math.radians(curr["lng"] - prev["lng"])
    a = math.sin(dlat / 2) ** 2 + math.cos(math.radians(prev["lat"])) \
        * math.cos(math.radians(curr["lat"])) * math.sin(dlng / 2) ** 2
    distance_km = 6371 * 2 * math.asin(math.sqrt(a))
    speed = (distance_km / interval_s) * 3600
    # Añadir variación realista (semáforos, frenadas)
    speed = max(0.0, speed + random.uniform(-5, 5))
    return round(speed, 1)


# ── Callbacks MQTT ─────────────────────────────────────────────────────────────
def on_connect(client, userdata, flags, reason_code, properties):
    if reason_code == 0:
        print(f"[MQTT] Conectado al broker {BROKER_HOST}:{BROKER_PORT}")
    else:
        print(f"[MQTT] Error de conexión, código: {reason_code}")


def on_disconnect(client, userdata, flags, reason_code, properties):
    print(f"[MQTT] Desconectado (código {reason_code}). Reintentando...")


# ── Main ───────────────────────────────────────────────────────────────────────
def main():
    waypoints = ROUTES.get(BUS_ROUTE)
    if waypoints is None:
        print(f"[SIM] Ruta '{BUS_ROUTE}' no definida. Rutas disponibles: {list(ROUTES.keys())}")
        return

    client = mqtt.Client(
        mqtt.CallbackAPIVersion.VERSION2,
        client_id=f"trackuniversity-simulator-{BUS_PLATE}",
    )
    client.on_connect    = on_connect
    client.on_disconnect = on_disconnect

    if USE_TLS:
        print("[MQTT] Usando TLS (HiveMQ Cloud)")
        client.tls_set(tls_version=ssl.PROTOCOL_TLS_CLIENT)
        client.username_pw_set(USERNAME, PASSWORD)
        port = int(os.getenv("MQTT_PORT", "8883"))
    else:
        port = BROKER_PORT

    print(f"[MQTT] Conectando a {BROKER_HOST}:{port} ...")
    client.connect(BROKER_HOST, port, keepalive=60)
    client.loop_start()

    time.sleep(1.5)

    topic        = f"universidad/bus/{BUS_PLATE}"
    route_points = interpolate_route(waypoints, INTERPOLATION_STEPS)
    idx          = 0
    prev_point   = None

    print(f"[SIM] Bus '{BUS_PLATE}' en ruta '{BUS_ROUTE}'")
    print(f"[SIM] Publicando en '{topic}' cada {PUBLISH_INTERVAL}s. Ctrl+C para detener.\n")

    try:
        while True:
            point = route_points[idx % len(route_points)]
            speed = estimate_speed(prev_point, point, PUBLISH_INTERVAL)

            payload = json.dumps({
                "lat":   round(point["lat"], 6),
                "lng":   round(point["lng"], 6),
                "speed": speed,
            })

            result = client.publish(topic, payload, qos=1)
            print(f"[SIM] [{idx + 1:04d}] {topic} → {payload}  (mid={result.mid})")

            prev_point = point
            idx       += 1
            time.sleep(PUBLISH_INTERVAL)

    except KeyboardInterrupt:
        print("\n[SIM] Simulador detenido.")
    finally:
        client.loop_stop()
        client.disconnect()


if __name__ == "__main__":
    main()
BROKER_PORT = int(os.getenv("MQTT_PORT", "1883"))
USE_TLS     = os.getenv("MQTT_USE_TLS", "false").lower() == "true"
USERNAME    = os.getenv("MQTT_USERNAME", "")
PASSWORD    = os.getenv("MQTT_PASSWORD", "")

PUBLISH_INTERVAL = 5  # segundos entre publicaciones

# ── Waypoints de la ruta ───────────────────────────────────────────────────────
# Ajusta estas coordenadas a las calles reales de tu universidad.
# La ruta es un polígono cerrado: el último punto vuelve al primero.
ROUTES = {
    "ruta1": [
        {"lat": 4.6284,  "lng": -74.0641},  # Entrada campus principal
        {"lat": 4.6301,  "lng": -74.0660},  # Av. El Dorado
        {"lat": 4.6325,  "lng": -74.0672},  # Parada norte
        {"lat": 4.6348,  "lng": -74.0658},  # Cruce intermedio
        {"lat": 4.6361,  "lng": -74.0630},  # Parada sur-este
        {"lat": 4.6340,  "lng": -74.0605},  # Retorno
        {"lat": 4.6310,  "lng": -74.0610},  # Parada biblioteca
        {"lat": 4.6284,  "lng": -74.0641},  # Vuelve a entrada
    ],
}

INTERPOLATION_STEPS = 20  # puntos suavizados entre cada waypoint


# ── Funciones de utilidad ──────────────────────────────────────────────────────
def interpolate_route(waypoints: list[dict], steps: int) -> list[dict]:
    """Genera una secuencia suavizada de coordenadas entre los waypoints."""
    points: list[dict] = []
    for i in range(len(waypoints) - 1):
        start = waypoints[i]
        end   = waypoints[i + 1]
        for step in range(steps):
            t = step / steps
            points.append({
                "lat": start["lat"] + t * (end["lat"] - start["lat"]),
                "lng": start["lng"] + t * (end["lng"] - start["lng"]),
            })
    return points


# ── Callbacks MQTT ─────────────────────────────────────────────────────────────
def on_connect(client, userdata, flags, reason_code, properties):
    if reason_code == 0:
        print(f"[MQTT] Conectado al broker {BROKER_HOST}:{BROKER_PORT}")
    else:
        print(f"[MQTT] Error de conexión, código: {reason_code}")


def on_disconnect(client, userdata, flags, reason_code, properties):
    print(f"[MQTT] Desconectado (código {reason_code}). Reintentando...")


def on_publish(client, userdata, mid, reason_code, properties):
    pass  # confirmación silenciosa


# ── Main ───────────────────────────────────────────────────────────────────────
def main():
    client = mqtt.Client(
        mqtt.CallbackAPIVersion.VERSION2,
        client_id="trackuniversity-simulator-ruta1",
    )
    client.on_connect    = on_connect
    client.on_disconnect = on_disconnect
    client.on_publish    = on_publish

    if USE_TLS:
        print("[MQTT] Usando TLS (HiveMQ Cloud)")
        client.tls_set(tls_version=ssl.PROTOCOL_TLS_CLIENT)
        client.username_pw_set(USERNAME, PASSWORD)
        port = int(os.getenv("MQTT_PORT", "8883"))
    else:
        port = BROKER_PORT

    print(f"[MQTT] Conectando a {BROKER_HOST}:{port} ...")
    client.connect(BROKER_HOST, port, keepalive=60)
    client.loop_start()

    time.sleep(1.5)  # esperar confirmación de conexión

    idx          = 0
    route_points = interpolate_route(ROUTES["ruta1"], INTERPOLATION_STEPS)
    topic        = "universidad/bus/ruta1"

    print(f"[SIM]  Publicando en '{topic}' cada {PUBLISH_INTERVAL}s. Ctrl+C para detener.\n")

    try:
        while True:
            point   = route_points[idx % len(route_points)]
            payload = json.dumps({"lat": round(point["lat"], 6), "lng": round(point["lng"], 6)})

            result = client.publish(topic, payload, qos=1)
            print(f"[SIM]  [{idx + 1:04d}] {topic} → {payload}  (mid={result.mid})")

            idx += 1
            time.sleep(PUBLISH_INTERVAL)

    except KeyboardInterrupt:
        print("\n[SIM]  Simulador detenido.")
    finally:
        client.loop_stop()
        client.disconnect()


if __name__ == "__main__":
    main()
