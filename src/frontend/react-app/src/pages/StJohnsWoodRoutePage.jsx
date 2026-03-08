import { useMemo, useState } from 'react';

const ORIGIN_INPUT = "St John's Wood Station, London";
const DESTINATION_INPUT = 'St John & St Elizabeth Hospital, London';

function formatDistance(distanceMeters) {
  if (distanceMeters < 1000) {
    return `${Math.round(distanceMeters)} m`;
  }

  return `${(distanceMeters / 1000).toFixed(2)} km`;
}

function buildProjectedPoints(points, width, height, padding) {
  if (!points?.length) {
    return [];
  }

  const longitudes = points.map((point) => point.longitude);
  const latitudes = points.map((point) => point.latitude);

  const minLon = Math.min(...longitudes);
  const maxLon = Math.max(...longitudes);
  const minLat = Math.min(...latitudes);
  const maxLat = Math.max(...latitudes);

  const usableWidth = width - padding * 2;
  const usableHeight = height - padding * 2;
  const lonRange = maxLon - minLon || 0.00001;
  const latRange = maxLat - minLat || 0.00001;

  return points.map((point) => {
    const x = padding + ((point.longitude - minLon) / lonRange) * usableWidth;
    const y = height - padding - ((point.latitude - minLat) / latRange) * usableHeight;

    return { ...point, x, y };
  });
}

function StJohnsWoodRoutePage() {
  const [route, setRoute] = useState(null);
  const [loading, setLoading] = useState(false);
  const [seeding, setSeeding] = useState(false);
  const [error, setError] = useState('');
  const [seedMessage, setSeedMessage] = useState('');

  const apiBaseUrl = useMemo(() => {
    const envUrl = import.meta.env.VITE_GOOGLE_MAPS_API_BASE_URL;
    return envUrl?.trim() || 'http://localhost:1002';
  }, []);
  const projectedPath = useMemo(
    () => buildProjectedPoints(route?.drawing?.coordinates || [], 680, 340, 24),
    [route]
  );

  async function loadRoute() {
    setLoading(true);
    setError('');
    setSeedMessage('');

    try {
      const response = await fetch(`${apiBaseUrl}/v1/routes`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          originInput: ORIGIN_INPUT,
          destinationInput: DESTINATION_INPUT
        })
      });

      if (!response.ok) {
        setRoute(null);
        setError('Could not load route. Ensure geolocations exist in the API.');
        return;
      }

      const payload = await response.json();
      setRoute(payload);
    }
    catch {
      setRoute(null);
      setError('Could not connect to the API.');
    }
    finally {
      setLoading(false);
    }
  }

  async function seedRouteData() {
    setSeeding(true);
    setError('');
    setSeedMessage('');

    const places = [
      { input: ORIGIN_INPUT, latitude: 51.53408, longitude: -0.17485 },
      { input: DESTINATION_INPUT, latitude: 51.53467, longitude: -0.18438 }
    ];

    try {
      for (const place of places) {
        const response = await fetch(`${apiBaseUrl}/v1/geolocations`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            location: {
              longitude: place.longitude,
              latitude: place.latitude
            },
            input: place.input,
            language: 'en',
            regionBias: 'gb',
            source: 'react-seed'
          })
        });

        if (!response.ok && response.status !== 202) {
          setError('Failed to seed geolocations.');
          return;
        }
      }

      setSeedMessage('Seeded geolocations. You can now load the route.');
    }
    catch {
      setError('Could not connect to the API.');
    }
    finally {
      setSeeding(false);
    }
  }

  return (
    <main className="page">
      <section className="card">
        <h1>St John&apos;s Wood Route</h1>
        <p className="muted">Origin: {ORIGIN_INPUT}</p>
        <p className="muted">Destination: {DESTINATION_INPUT}</p>

        <button type="button" onClick={loadRoute} disabled={loading}>
          {loading ? 'Loading route...' : 'Load Route'}
        </button>
        <button type="button" onClick={seedRouteData} disabled={seeding}>
          {seeding ? 'Seeding...' : 'Seed St John\'s Wood Data'}
        </button>

        {error && <p className="error">{error}</p>}
        {seedMessage && <p className="success">{seedMessage}</p>}
      </section>

      {route && (
        <section className="card">
          <h2>Route</h2>
          <p>Total distance: {formatDistance(route.totalDistanceMeters)}</p>

          <h3>Instructions</h3>
          <ol>
            {route.segments.map((segment) => (
              <li key={segment.order}>
                {segment.instruction} ({formatDistance(segment.distanceMeters)})
              </li>
            ))}
          </ol>

          <h3>Draw (Minimal)</h3>
          <p>{route.drawing.instruction}</p>
          <p>Type: {route.drawing.type}</p>

          <h3>Coordinates</h3>
          <ul>
            {route.drawing.coordinates.map((point, index) => (
              <li key={`${point.plusCode}-${index}`}>
                {index + 1}. {point.label}: {point.latitude.toFixed(5)}, {point.longitude.toFixed(5)}
              </li>
            ))}
          </ul>

          <h3>Route Preview</h3>
          <div className="route-canvas">
            <svg viewBox="0 0 680 340" role="img" aria-label="Route preview">
              {projectedPath.length > 1 && (
                <polyline
                  points={projectedPath.map((point) => `${point.x},${point.y}`).join(' ')}
                  fill="none"
                  stroke="#1a73e8"
                  strokeWidth="4"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              )}

              {projectedPath.map((point, index) => (
                <g key={`${point.plusCode}-${index}`}>
                  <circle cx={point.x} cy={point.y} r="6" fill={index === 0 ? '#0b6b2f' : '#b11c1c'} />
                  <text x={point.x + 10} y={point.y - 10} fontSize="12" fill="#172033">
                    {index + 1}. {point.label}
                  </text>
                </g>
              ))}
            </svg>
          </div>
        </section>
      )}
    </main>
  );
}

export default StJohnsWoodRoutePage;
