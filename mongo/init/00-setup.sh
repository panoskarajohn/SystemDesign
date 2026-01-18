#!/usr/bin/env bash
set -euo pipefail

MONGOS_HOST="${MONGOS_HOST:-127.0.0.1}"
MONGOS_PORT="27017"

SH1_HOST="shard1"
SH2_HOST="shard2"
SH3_HOST="shard3"
SH_PORT="27017"

SH1_RS="rs-shard-1"
SH2_RS="rs-shard-2"
SH3_RS="rs-shard-3"

MONGO_ROOT_USER="${MONGO_INITDB_ROOT_USERNAME:-root}"
MONGO_ROOT_PASSWORD="${MONGO_INITDB_ROOT_PASSWORD:-rootpassword}"
MONGO_AUTH_URI="mongodb://${MONGO_ROOT_USER}:${MONGO_ROOT_PASSWORD}@${MONGOS_HOST}:${MONGOS_PORT}/admin?authSource=admin"

wait_for_mongos () {
  echo "Waiting for mongos (${MONGOS_HOST}:${MONGOS_PORT})..."
  until \
    mongosh --host "$MONGOS_HOST" --port "$MONGOS_PORT" --quiet --eval "db.adminCommand({ping:1}).ok" 2>/dev/null | grep -q 1 \
    || mongosh "$MONGO_AUTH_URI" --quiet --eval "db.adminCommand({ping:1}).ok" 2>/dev/null | grep -q 1; do
    sleep 1
  done
}

ensure_root_user () {
  local exists
  exists="$(mongosh "$MONGO_AUTH_URI" --quiet --eval "db.getSiblingDB('admin').getUser('${MONGO_ROOT_USER}') ? '1' : '0'" 2>/dev/null || true)"
  if echo "$exists" | grep -q 1; then
    echo "Root user already present."
    return 0
  fi
  exists="$(mongosh --host "$MONGOS_HOST" --port "$MONGOS_PORT" --quiet --eval "db.getSiblingDB('admin').getUser('${MONGO_ROOT_USER}') ? '1' : '0'" 2>/dev/null || true)"
  if echo "$exists" | grep -q 1; then
    echo "Root user already present."
    return 0
  fi
  echo "Creating root user..."
  mongosh --host "$MONGOS_HOST" --port "$MONGOS_PORT" --quiet --eval "db.getSiblingDB('admin').createUser({user:'${MONGO_ROOT_USER}', pwd:'${MONGO_ROOT_PASSWORD}', roles:[{role:'root', db:'admin'}]})"
}

wait_for_mongos_auth () {
  echo "Waiting for mongos auth to be ready (${MONGOS_HOST}:${MONGOS_PORT})..."
  until mongosh "$MONGO_AUTH_URI" --quiet --eval "db.adminCommand({ping:1}).ok" 2>/dev/null | grep -q 1; do
    sleep 1
  done
}

echo "== Waiting for mongos =="
wait_for_mongos

echo "== Ensure root user =="
ensure_root_user

echo "== Waiting for mongos with auth =="
wait_for_mongos_auth

echo "== Add shards to cluster (idempotent) =="
mongosh "$MONGO_AUTH_URI" --quiet --eval "
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
  if [[ "$base" == "00-"* ]] || [[ "$base" == "rs-config.js" ]] || [[ "$base" == "rs-shard-"* ]]; then
    continue
  fi
  echo "-> $base"
  mongosh "$MONGO_AUTH_URI" --quiet "$f"
done

echo "Bootstrap complete."
