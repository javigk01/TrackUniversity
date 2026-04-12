import { useEffect, useState } from 'react';

export default function RouteOccupancy() {
  const [occupancyData, setOccupancyData] = useState([]);

  useEffect(() => {
    const container = document.getElementById('occupancy-data');
    if (!container) return;

    const parseData = () => {
      try {
        const data = JSON.parse(container.textContent || '[]');
        setOccupancyData(data);
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

  if (occupancyData.length === 0) {
    return <p style={{ fontSize: '0.8rem', color: '#94a3b8' }}>Cargando datos...</p>;
  }

  return (
    <div style={{ fontSize: '0.8rem' }}>
      {occupancyData.map((route) => (
        <div key={route.routeCode} style={{ marginBottom: '1rem' }}>
          <div style={{
            display: 'flex',
            justifyContent: 'space-between',
            marginBottom: '0.3rem',
            fontSize: '0.75rem',
            fontWeight: 600,
          }}>
            <span>{route.routeName}</span>
            <span style={{ color: '#2563eb' }}>
              {Math.round(route.occupancyPercent)}%
            </span>
          </div>
          <div style={{
            background: '#e2e8f0',
            borderRadius: '4px',
            height: '6px',
            overflow: 'hidden',
          }}>
            <div
              style={{
                background: `linear-gradient(90deg, #2563eb 0%, #3b82f6 100%)`,
                height: '100%',
                width: `${route.occupancyPercent}%`,
                transition: 'width 0.3s ease',
              }}
            />
          </div>
          <div style={{
            fontSize: '0.65rem',
            color: '#64748b',
            marginTop: '0.2rem',
          }}>
            {route.activeBuses} / {route.totalBuses} buses
          </div>
        </div>
      ))}
    </div>
  );
}
