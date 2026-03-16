import { useMemo, useState } from 'react';
import { MapContainer, TileLayer, Polyline, Marker, Popup } from 'react-leaflet';
import L from 'leaflet';

const ORIGIN_INPUT = "St John's Wood Station, London";
const DESTINATION_INPUT = 'St John & St Elizabeth Hospital, London';

// Fix Leaflet marker icons
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
});

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

          <h3>Map View - St John's Wood</h3>
          {route?.drawing?.coordinates && route.drawing.coordinates.length > 0 && (
            <div className="map-container">
              <MapContainer
                center={[51.53435, -0.17962]}
                zoom={15}
                style={{ width: '100%', height: '100%' }}
              >
                <TileLayer
                  url="http://localhost:1002/v1/tiles/{z}/{x}/{y}"
                  attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, &copy; <a href="https://cartodb.com/attributions">CartoDB</a>'
                  maxZoom={19}
                  minZoom={0}
                />

                {route.drawing.coordinates.map((point, index) => (
                  <Marker
                    key={`${point.plusCode}-${index}`}
                    position={[point.latitude, point.longitude]}
                    icon={L.icon({
                      iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
                      shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
                      iconSize: [25, 41],
                      iconAnchor: [12, 41],
                      popupAnchor: [1, -34],
                      shadowSize: [41, 41],
                    })}
                  >
                    <Popup>
                      {index + 1}. {point.label}
                    </Popup>
                  </Marker>
                ))}

                {route.drawing.coordinates.length > 1 && (
                  <Polyline
                    positions={route.drawing.coordinates.map((point) => [
                      point.latitude,
                      point.longitude,
                    ])}
                    color="#1a73e8"
                    weight={4}
                    opacity={0.8}
                  />
                )}
              </MapContainer>
            </div>
          )}
        </section>
      )}
    </main>
  );
}

export default StJohnsWoodRoutePage;
