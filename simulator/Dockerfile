FROM python:3.12-slim

WORKDIR /app

COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

COPY bus_simulator.py ./

# Variables de entorno con valores por defecto (sobreescritos en docker-compose.yml)
ENV MQTT_HOST=mosquitto
ENV MQTT_PORT=1883
ENV MQTT_USE_TLS=false
ENV MQTT_USERNAME=
ENV MQTT_PASSWORD=

CMD ["python", "bus_simulator.py"]
