#!/usr/bin/env bash
set -euo pipefail

CFG_HOST="configsvr"
CFG_PORT="27019"
CFG_RS="rs-config"

MONGOS_HOST="mongos"
MONGOS_PORT="27017"

SH1_HOST="shard1"
SH2_HOST="shard2"
SH3_HOST="shard3"

# >>> THIS WAS MISSING <<<
SH_PORT="27017"

# Must match each shard's --replSet value
SH1_RS="rs-shard-1"
SH2_RS="rs-shard-2"
SH3_RS="rs-shard-3"

wait_for_ping () {
  local host="$1"; local port="$2"; local label="$3"
  echo "Waiting for $label (${host}:${port})..."
  until mongosh --host "$host" --port "$port" --quiet --eval "db.adminCommand('ping').ok" 2>/dev/null | grep -q 1; do
    sleep 1
  done
}

wait_for_primary () {
  local host="$1"; local port="$2"; local label="$3"
  echo "Waiting for $label PRIMARY..."
  for i in {1..90}; do
    if mongosh --host "$host" --port "$port" --quiet --eval "db.hello().isWritablePrimary" 2>/dev/null | grep -q true; then
      echo "$label is PRIMARY."
      return 0
    fi
    sleep 1
  done
  echo "ERROR: $label did not become PRIMARY in time"
  mongosh --host "$host" --port "$port" --eval "rs.status()" || true
  exit 1
}

echo "== Config server bootstrap =="
wait_for_ping "$CFG_HOST" "$CFG_PORT" "configsvr"

echo "Ensuring config RS ($CFG_RS) is initiated (idempotent)..."
mongosh --host "$CFG_HOST" --port "$CFG_PORT" --quiet --eval "
try { rs.status().ok } catch (e) {
  rs.initiate({
    _id: '$CFG_RS',
    configsvr: true,
    members: [{ _id: 0, host: '${CFG_HOST}:${CFG_PORT}' }]
  })
}
"

wait_for_primary "$CFG_HOST" "$CFG_PORT" "config RS ($CFG_RS)"

echo "== Mongos readiness =="
wait_for_ping "$MONGOS_HOST" "$MONGOS_PORT" "mongos"

init_shard_rs () {
  local host="$1"; local port="$2"; local rsname="$3"
  echo "== Shard bootstrap: $rsname on $host:$port =="

  wait_for_ping "$host" "$port" "shard node ($host)"

  echo "Ensuring shard RS ($rsname) is initiated (idempotent)..."
  mongosh --host "$host" --port "$port" --quiet --eval "
try { rs.status().ok } catch (e) {
  rs.initiate({
    _id: '$rsname',
    members: [{ _id: 0, host: '${host}:${port}' }]
  })
}
"
  wait_for_primary "$host" "$port" "shard RS ($rsname)"
}

init_shard_rs "$SH1_HOST" "$SH_PORT" "$SH1_RS"
init_shard_rs "$SH2_HOST" "$SH_PORT" "$SH2_RS"
init_shard_rs "$SH3_HOST" "$SH_PORT" "$SH3_RS"

echo "== Add shards to cluster (idempotent) =="
mongosh --host "$MONGOS_HOST" --port "$MONGOS_PORT" --quiet --eval "
const existing = (db.adminCommand({listShards:1}).shards || []).map(s => s._id);

function addIfMissing(rsName, host, port) {
  if (!existing.includes(rsName)) {
    const conn = rsName + '/' + host + ':' + port;
    print('Adding shard ' + rsName + ' -> ' + conn);
    db.adminCommand({ addShard: conn, name: rsName });
  } else {
    print('Shard already present: ' + rsName);
  }
}

addIfMissing('$SH1_RS', '$SH1_HOST', '$SH_PORT');
addIfMissing('$SH2_RS', '$SH2_HOST', '$SH_PORT');
addIfMissing('$SH3_RS', '$SH3_HOST', '$SH_PORT');
"

echo "== Run user JS scripts against mongos =="
shopt -s nullglob
for f in /scripts/*.js; do
  base="$(basename "$f")"
  if [[ "$base" == "00-"* ]]; then
    continue
  fi
  echo "-> $base"
  mongosh --host "$MONGOS_HOST" --port "$MONGOS_PORT" --quiet "$f"
done

echo "Bootstrap complete."
