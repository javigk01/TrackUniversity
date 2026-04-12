import { useEffect, useState } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

const COLORS = ['#2563eb', '#dc2626', '#16a34a', '#ea580c', '#7c3aed', '#0891b2'];
const API_URL = import.meta.env.PUBLIC_API_URL ?? 'http://localhost:5000';

export default function OccupancyChart() {
  const [chartData, setChartData] = useState([]);
  const [visibleRoutes, setVisibleRoutes] = useState({});
  const [routes, setRoutes] = useState([]);

  // Cargar datos históricos de la BD 
  useEffect(() => {
    const loadHistoricalData = async () => {
      try {
        const res = await fetch(`${API_URL}/api/occupancyhistory/latest?hours=1&limit=200`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const history = await res.json();

        if (history.length === 0) {
          console.log('[OccupancyChart] No historical data available yet');
          return;
        }

        // Agrupar por timestamp para consolidar múltiples rutas
        const groupedByTime = {};
        const routesMap = {}; // Usar map para evitar duplicados

        history.forEach((entry) => {
          const key = new Date(entry.timestamp).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
          if (!groupedByTime[key]) {
            groupedByTime[key] = { 
              time: key,
              timestamp: new Date(entry.timestamp).getTime(),
            };
          }
          groupedByTime[key][entry.routeCode] = Math.round(entry.occupancyPercent * 10) / 10;
          routesMap[entry.routeCode] = entry.routeName;
        });

        // Convertir rutas map a array
        const routesArray = Object.entries(routesMap).map(([code, name]) => ({
          routeCode: code,
          routeName: name,
        }));
        setRoutes(routesArray);

        // Inicializar visibilidad de rutas
        const initialVis = {};
        routesArray.forEach((r) => {
          initialVis[r.routeCode] = true;
        });
        setVisibleRoutes(initialVis);

        // Ordenar por timestamp y limitar a últimos 40 puntos
        const sortedData = Object.values(groupedByTime)
          .sort((a, b) => a.timestamp - b.timestamp)
          .slice(-40);
        
        setChartData(sortedData);
        console.log('[OccupancyChart] Loaded', sortedData.length, 'historical data points from', routesArray.length, 'routes');
      } catch (err) {
        console.error('[OccupancyChart] Error loading historical data:', err);
      }
    };

    loadHistoricalData();
  }, []);

  useEffect(() => {
    const container = document.getElementById('occupancy-data');
    if (!container) return;

    const parseData = () => {
      try {
        const data = JSON.parse(container.textContent || '[]');
        
        // Si datos vacíos, no hacer nada
        if (data.length === 0) return;
        
        // Solo actualizar rutas si hay nuevas
        setRoutes((prevRoutes) => {
          const prevCodes = prevRoutes.map(r => r.routeCode);
          const newRoutes = data.filter(route => !prevCodes.includes(route.routeCode));
          
          if (newRoutes.length > 0) {
            return [...prevRoutes, ...newRoutes];
          }
          return prevRoutes;
        });

        // Solo actualizar visibilidad para rutas nuevas
        setVisibleRoutes((prevVisible) => {
          const updated = { ...prevVisible };
          data.forEach((route) => {
            if (!(route.routeCode in updated)) {
              updated[route.routeCode] = true;
            }
          });
          return updated;
        });

        // Agregar NUEVO punto de datos sin borrar historiales
        setChartData((prevData) => {
          const newPoint = {
            time: new Date().toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit', second: '2-digit' }),
            timestamp: Date.now(),
          };

          // Add occupancy for each route
          data.forEach((route) => {
            newPoint[route.routeCode] = Math.round(route.occupancyPercent * 10) / 10;
          });

          // Verificar si ya existe un punto con el mismo timestamp (dentro de 1 segundo)
          const lastPoint = prevData[prevData.length - 1];
          if (lastPoint && Math.abs(newPoint.timestamp - lastPoint.timestamp) < 1000) {
            // Actualizar el último punto en lugar de agregar uno nuevo
            return [...prevData.slice(0, -1), newPoint].slice(-40);
          }

          // Agregar nuevo punto
          const updated = [...prevData, newPoint];
          return updated.slice(-40);
        });
      } catch (err) {
        console.error('Error parsing occupancy data:', err);
      }
    };

    // Parse immediately
    parseData();

    // Watch for updates
    const observer = new MutationObserver(parseData);
    observer.observe(container, { childList: true, characterData: true, subtree: true });

    return () => observer.disconnect();
  }, []);

  const toggleRoute = (routeCode) => {
    setVisibleRoutes((prev) => ({
      ...prev,
      [routeCode]: !prev[routeCode],
    }));
  };

  if (chartData.length === 0) {
    return <p style={{ fontSize: '0.8rem', color: '#94a3b8' }}>Cargando gráfico...</p>;
  }

  return (
    <div style={{ padding: '0' }}>
      {/* Route toggles */}
      <div style={{
        display: 'flex',
        flexWrap: 'wrap',
        gap: '0.5rem',
        marginBottom: '1rem',
        paddingBottom: '1rem',
        borderBottom: '1px solid #e2e8f0',
      }}>
        {routes.map((route, idx) => (
          <button
            key={route.routeCode}
            onClick={() => toggleRoute(route.routeCode)}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.4rem',
              padding: '0.4rem 0.8rem',
              border: `1px solid ${COLORS[idx % COLORS.length]}`,
              background: visibleRoutes[route.routeCode] ? COLORS[idx % COLORS.length] : '#f8fafc',
              color: visibleRoutes[route.routeCode] ? 'white' : '#334155',
              borderRadius: '6px',
              fontSize: '0.75rem',
              fontWeight: 600,
              cursor: 'pointer',
              transition: 'all 0.2s ease',
            }}
            onMouseEnter={(e) => {
              e.target.style.opacity = '0.8';
            }}
            onMouseLeave={(e) => {
              e.target.style.opacity = '1';
            }}
          >
            <span
              style={{
                width: '8px',
                height: '8px',
                borderRadius: '50%',
                background: COLORS[idx % COLORS.length],
              }}
            />
            {route.routeName}
          </button>
        ))}
      </div>

      {/* Line Chart */}
      <ResponsiveContainer width="100%" height={280}>
        <LineChart data={chartData} margin={{ top: 5, right: 10, left: -20, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
          <XAxis
            dataKey="time"
            tick={{ fontSize: 10 }}
            stroke="#94a3b8"
          />
          <YAxis
            domain={[0, 100]}
            tick={{ fontSize: 10 }}
            stroke="#94a3b8"
            label={{ value: '%', angle: -90, position: 'insideLeftMiddle', offset: 10, fontSize: 10 }}
          />
          <Tooltip
            contentStyle={{
              background: '#1e293b',
              border: '1px solid #334155',
              borderRadius: '8px',
              color: '#f1f5f9',
              fontSize: '0.75rem',
            }}
            formatter={(value) => [`${value}%`, '']}
          />
          <Legend wrapperStyle={{ fontSize: '0.75rem', paddingTop: '1rem' }} />
          {routes.map((route, idx) => (
            visibleRoutes[route.routeCode] && (
              <Line
                key={route.routeCode}
                type="monotone"
                dataKey={route.routeCode}
                stroke={COLORS[idx % COLORS.length]}
                dot={false}
                isAnimationActive={false}
                strokeWidth={2}
                name={route.routeName}
              />
            )
          ))}
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
