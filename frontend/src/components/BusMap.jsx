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
  const [buses,        setBuses]        = useState([]);
  const [routes,       setRoutes]       = useState([]);
  const [selectedRoute, setSelectedRoute] = useState(null);
  const [lastUpdate,   setLastUpdate]   = useState(null);
  const [error,        setError]        = useState(null);

  // Ícono de bus personalizado (bus2.svg)
  const busSvgIcon = new L.Icon({
    iconUrl:       '/bus2.svg',
    iconSize:      [40, 40],
    iconAnchor:    [20, 40],
    popupAnchor:   [0, -40],
  });

  const fetchRoutes = useCallback(async () => {
    try {
      const res = await fetch(`${API_URL}/api/routes`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setRoutes(data);
    } catch (err) {
      console.error('[BusMap] Error fetching routes:', err);
    }
  }, []);

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

  // Obtener rutas una sola vez
  useEffect(() => {
    fetchRoutes();
  }, [fetchRoutes]);

  // Poll buses cada 5 segundos
  useEffect(() => {
    fetchBuses();
    const id = setInterval(fetchBuses, POLL_INTERVAL);
    return () => clearInterval(id);
  }, [fetchBuses]);

  // Filtrar buses por ruta seleccionada (para el mapa)
  const filteredBuses = selectedRoute 
    ? buses.filter(b => b.route?.routeCode === selectedRoute)
    : buses;

  const activeBuses = filteredBuses.filter(b => b.lastLatitude && b.lastLongitude);

  // Calcular todas las métricas GLOBALES (independiente de filtro de ruta)
  useEffect(() => {
    const allActiveBuses = buses.filter(b => b.lastLatitude && b.lastLongitude && b.route?.routeCode);
    
    // Contar solo rutas UNICAS activas (en lugar de contar todos los buses duplicados)
    const uniqueActiveRoutes = new Set(allActiveBuses.map(b => b.route?.routeCode));
    const totalActive = uniqueActiveRoutes.size; // Contar solo rutas únicas
    
    const totalPassengers = allActiveBuses.reduce((sum, bus) => sum + (bus.currentPassengers || 0), 0);
    const avgSpeed = allActiveBuses.length > 0
      ? Math.round(allActiveBuses.reduce((sum, bus) => sum + (bus.lastSpeed || 0), 0) / allActiveBuses.length)
      : 0;

    // Obtener ocupación desde la BD Y GUARDARLA
    const fetchOccupancy = async () => {
      try {
        // Calcular ocupación por ruta basándose en pasajeros reales de la BD
        const occupancyByRoute = routes.map(route => {
          const routeBuses = buses.filter(b => b.route?.routeCode === route.routeCode);
          const totalPassengers = routeBuses.reduce((sum, bus) => sum + (bus.currentPassengers || 0), 0);
          const totalCapacity = routeBuses.reduce((sum, bus) => sum + (bus.capacity || 0), 0);
          
          return {
            routeCode: route.routeCode,
            routeName: route.name,
            activeBuses: routeBuses.filter(b => b.lastLatitude && b.lastLongitude).length,
            totalBuses: routeBuses.length,
            occupancyPercent: totalCapacity > 0 ? (totalPassengers / totalCapacity) * 100 : 0,
          };
        });
        
        console.log('[BusMap] Calculated occupancy:', occupancyByRoute.length, 'routes', occupancyByRoute);
        
        // Guardar ocupancia en la BD
        const response = await fetch(`${API_URL}/api/occupancyhistory`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(occupancyByRoute),
        });
        
        if (!response.ok) {
          const errorText = await response.text();
          console.error('[BusMap] Error saving occupancy:', response.status, errorText);
        } else {
          console.log('[BusMap] Occupancy saved successfully at', new Date().toISOString());
        }
        
        const occupancyEl = document.getElementById('occupancy-data');
        if (occupancyEl) occupancyEl.textContent = JSON.stringify(occupancyByRoute);
      } catch (err) {
        console.error('[BusMap] Error calculating occupancy:', err);
      }
    };

    // Actualizar métricas del DOM
    const activeEl = document.getElementById('active-buses');
    const passengersEl = document.getElementById('total-passengers');
    const speedEl = document.getElementById('avg-speed');

    if (activeEl) activeEl.textContent = totalActive;
    if (passengersEl) passengersEl.textContent = totalPassengers;
    if (speedEl) speedEl.textContent = avgSpeed;
    
    // Obtener ocupación desde la BD cada vez que se actualizan los buses
    fetchOccupancy();
  }, [buses]);

  return (
    <div style={{ fontFamily: 'inherit' }}>

      {/* ── Selector de ruta ── */}
      <div style={{
        marginBottom: '1rem', padding: '0.75rem', background: '#f7fafc',
        borderRadius: '6px', border: '1px solid #e2e8f0',
      }}>
        <label style={{
          display: 'block', fontSize: '0.85rem', fontWeight: 600,
          marginBottom: '0.5rem', color: '#2d3748',
        }}>
          Selecciona una Ruta ({routes.length} disponibles)
        </label>
        <select
          value={selectedRoute || ''}
          onChange={(e) => setSelectedRoute(e.target.value || null)}
          style={{
            width: '100%', padding: '0.6rem', fontSize: '0.9rem',
            border: '2px solid #cbd5e0', borderRadius: '6px',
            backgroundColor: 'white', color: '#2d3748', cursor: 'pointer',
          }}
        >
          <option value="">📍 Ver TODAS las rutas</option>
          {routes.map((route) => (
            <option key={route.id} value={route.routeCode}>
              🚌 {route.name}
            </option>
          ))}
        </select>
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
            icon={busSvgIcon}
          >
            <Popup>
              <strong style={{ fontSize: '0.9rem', color: '#1565C0' }}>{bus.name}</strong>
              <br />
              <span style={{ fontSize: '0.8rem', color: '#4a5568' }}>📍 Placa: {bus.plate}</span>
              <br />
              <span style={{ fontSize: '0.8rem', color: '#4a5568' }}>🛣️ Ruta: {bus.route?.routeCode || 'N/A'}</span>
              <br />
              <span style={{ fontSize: '0.75rem', color: '#718096' }}>
                {bus.lastUpdated
                  ? `🕐 ${new Date(bus.lastUpdated).toLocaleTimeString('es-CO')}`
                  : 'Sin datos'}
              </span>
            </Popup>
          </Marker>
        ))}
      </MapContainer>

      {/* ── Leyenda ── */}
      <div style={{
        marginTop: '0.5rem', fontSize: '0.75rem', color: '#a0aec0', textAlign: 'right',
      }}>
        Auto-actualización cada {POLL_INTERVAL / 1000}s · Buses: {buses.length} total
      </div>
    </div>
  );
}
