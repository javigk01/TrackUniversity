/**
 * BusMap.jsx — Isla interactiva de Astro
 *
 * Única parte de la página que ejecuta JavaScript en el navegador.
 * Hace polling al backend cada 5 segundos y mueve los marcadores en el mapa.
 *
 * Tecnologías: React 18 + react-leaflet 4 + Leaflet 1.9
 */
import { useEffect, useState, useCallback } from 'react';
import { MapContainer, TileLayer, Marker, Popup, Polyline } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

// ── Fix íconos de Leaflet en bundlers (Vite/Webpack) ──────────────────────────
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl:       'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl:     'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

// Ícono personalizado para el bus (color diferenciado)
const busIcon = new L.Icon({
  iconUrl:       'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-blue.png',
  shadowUrl:     'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
  iconSize:      [25, 41],
  iconAnchor:    [12, 41],
  popupAnchor:   [1, -34],
  shadowSize:    [41, 41],
});

// ── Configuración ─────────────────────────────────────────────────────────────
const API_URL        = import.meta.env.PUBLIC_API_URL ?? 'http://localhost:5000';
const POLL_INTERVAL  = 5000; // ms — igual que el simulador
const DEFAULT_CENTER = [4.6284, -74.0641]; // Ajustar a tu universidad
const DEFAULT_ZOOM   = 14;

// ── Componente principal ──────────────────────────────────────────────────────
export default function BusMap() {
  const [buses,      setBuses]      = useState([]);
  const [lastUpdate, setLastUpdate] = useState(null);
  const [error,      setError]      = useState(null);

  const fetchBuses = useCallback(async () => {
    try {
      const res  = await fetch(`${API_URL}/api/bus`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setBuses(data);
      setLastUpdate(new Date().toLocaleTimeString('es-CO'));
      setError(null);
    } catch (err) {
      setError('No se pudo conectar con el backend. ¿Está corriendo en localhost:5000?');
      console.error('[BusMap]', err);
    }
  }, []);

  useEffect(() => {
    fetchBuses();
    const id = setInterval(fetchBuses, POLL_INTERVAL);
    return () => clearInterval(id);
  }, [fetchBuses]);

  const activeBuses = buses.filter(b => b.lastLatitude && b.lastLongitude);

  return (
    <div style={{ fontFamily: 'inherit' }}>

      {/* ── Status bar ── */}
      <div style={{
        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
        marginBottom: '0.5rem', fontSize: '0.8rem', color: '#718096',
      }}>
        <span>
          {activeBuses.length > 0
            ? `${activeBuses.length} bus${activeBuses.length > 1 ? 'es' : ''} activo${activeBuses.length > 1 ? 's' : ''}`
            : 'Sin posición disponible aún'}
        </span>
        {lastUpdate && <span>Última actualización: {lastUpdate}</span>}
      </div>

      {/* ── Error banner ── */}
      {error && (
        <div style={{
          background: '#fff5f5', border: '1px solid #fed7d7', borderRadius: '6px',
          padding: '0.5rem 0.75rem', marginBottom: '0.5rem', fontSize: '0.8rem', color: '#c53030',
        }}>
          ⚠️ {error}
        </div>
      )}

      {/* ── Mapa Leaflet ── */}
      <MapContainer
        center={DEFAULT_CENTER}
        zoom={DEFAULT_ZOOM}
        scrollWheelZoom={true}
        style={{ height: '460px', width: '100%', borderRadius: '8px', zIndex: 0 }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        {activeBuses.map(bus => (
          <Marker
            key={bus.id}
            position={[bus.lastLatitude, bus.lastLongitude]}
            icon={busIcon}
          >
            <Popup>
              <strong style={{ fontSize: '0.9rem' }}>{bus.name}</strong>
              <br />
              <span style={{ fontSize: '0.8rem', color: '#4a5568' }}>Ruta: {bus.route}</span>
              <br />
              <span style={{ fontSize: '0.75rem', color: '#718096' }}>
                {bus.lastUpdated
                  ? `Actualizado: ${new Date(bus.lastUpdated).toLocaleTimeString('es-CO')}`
                  : 'Sin datos de tiempo'}
              </span>
            </Popup>
          </Marker>
        ))}
      </MapContainer>

      {/* ── Leyenda ── */}
      <div style={{
        marginTop: '0.5rem', fontSize: '0.75rem', color: '#a0aec0', textAlign: 'right',
      }}>
        Actualización automática cada {POLL_INTERVAL / 1000}s · API: {API_URL}
      </div>
    </div>
  );
}
