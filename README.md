# SystemDesign

This repo includes two Docker Compose setups:
- **Base (single MongoDB)** for local development.
- **Distributed (MongoDB sharded cluster)** for testing sharding/auth.

## Base stack
Starts the API plus a single MongoDB container defined in `compose.yaml`.

```bash
docker compose up -d --build
```

Stop and remove volumes:

```bash
docker compose down -v
```

## Distributed stack
Starts the sharded MongoDB cluster plus the API using the distributed settings.

```bash
docker compose -f mongo/compose.distributed.yaml -f compose.distributed.yaml up -d --build
```

Stop and remove volumes:

```bash
docker compose -f mongo/compose.distributed.yaml -f compose.distributed.yaml down -v
```

## Notes
- The distributed API uses `ASPNETCORE_ENVIRONMENT=Distributed` and reads
  `src/proximity/Proximity.Api/appsettings.Distributed.json`.
- The sharded cluster exposes `mongos` on `localhost:27017`.
