#!/usr/bin/env bash
set -euo pipefail

command -v docker >/dev/null || { printf 'Docker é necessário para o teste.\n' >&2; exit 127; }

test_id="taskdev-unified-$$"
image="$test_id:smoke"
api="$test_id-api"
db="$test_id-db"
proxy="$test_id-proxy"
temp_dir="$(mktemp -d)"

cleanup() {
  local status="$?"
  if (( status != 0 )); then
    for container in "$api" "$db" "$proxy"; do
      docker logs --tail 60 "$container" || true
    done
  fi
  docker rm -fv "$api" "$db" "$proxy" >/dev/null 2>&1 || true
  docker network rm "$test_id" >/dev/null 2>&1 || true
  docker image rm "$image" >/dev/null 2>&1 || true
  rm -f -- "$temp_dir"/{Caddyfile,cookies,headers,body,root.crt}
  rmdir -- "$temp_dir"
}
trap cleanup EXIT

docker build -t "$image" .
docker network create "$test_id" >/dev/null
export POSTGRES_PASSWORD="$(openssl rand -hex 24)"
export AGENT_ENCRYPTION_KEY="$(openssl rand -base64 32)"
export DATABASE_URL="postgresql://postgres:$POSTGRES_PASSWORD@$db:5432/taskdev?sslmode=disable"
docker run -d --name "$db" --network "$test_id" \
  -e POSTGRES_PASSWORD -e POSTGRES_DB=taskdev postgres:17-alpine >/dev/null
for attempt in {1..60}; do
  if docker exec "$db" pg_isready -h 127.0.0.1 -U postgres -d taskdev >/dev/null 2>&1; then
    break
  fi
  sleep 1
done
docker exec "$db" pg_isready -h 127.0.0.1 -U postgres -d taskdev

cat > "$temp_dir/Caddyfile" <<EOF
https://localhost {
  tls internal
  reverse_proxy $api:8080
}
EOF
docker run -d --name "$proxy" --network "$test_id" -p 127.0.0.1::443 \
  --mount "type=bind,source=$temp_dir/Caddyfile,target=/etc/caddy/Caddyfile,readonly" caddy:2-alpine >/dev/null
proxy_ip="$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' "$proxy")"
proxy_binding="$(docker port "$proxy" 443/tcp)"
base_url="https://localhost:${proxy_binding##*:}"

start_app() {
  docker run -d --name "$api" --network "$test_id" -p 127.0.0.1::8080 \
    --memory 512m --cpus 0.5 -e DATABASE_URL -e AGENT_ENCRYPTION_KEY \
    -e "ForwardedHeaders__KnownProxies__0=$proxy_ip" \
    -e Security__SecureCookies=false -e Admin__BootstrapEnabled="$1" "$image" >/dev/null
}

request() {
  curl -sS --max-time 15 --cacert "$temp_dir/root.crt" "$@"
}

wait_for_app() {
  request -f --retry 30 --retry-all-errors --retry-delay 2 --retry-max-time 90 \
    "$base_url/api/health" | jq -e '.status == "UP"' >/dev/null
}

expect_status() {
  local expected="$1"
  shift
  local actual
  actual="$(request -o "$temp_dir/body" -w '%{http_code}' "$@")"
  if [[ "$actual" != "$expected" ]]; then
    printf 'Esperado HTTP %s, recebido %s em %s\n' "$expected" "$actual" "${*: -1}" >&2
    return 1
  fi
  if grep -q '<div id="app"></div>' "$temp_dir/body"; then
    printf 'O fallback da SPA respondeu em %s\n' "${*: -1}" >&2
    return 1
  fi
}

start_app true
for attempt in {1..30}; do
  if docker cp "$proxy:/data/caddy/pki/authorities/local/root.crt" "$temp_dir/root.crt" 2>/dev/null; then
    break
  fi
  sleep 1
done
wait_for_app

html="$(request -f "$base_url/")"
grep -Fq '<div id="app"></div>' <<< "$html"
test "$(request -f "$base_url/spaces/caixa")" = "$html"
asset="$(sed -n 's/.*src="\(\/assets\/[^\"]*\.js\)".*/\1/p' <<< "$html")"
test -n "$asset"
request -f -o /dev/null "$base_url$asset"
request -f "$base_url/manifest.webmanifest" | jq -e '.name == "Task.dev"' >/dev/null
expect_status 404 "$base_url/assets/ausente.js"
expect_status 404 "$base_url/api"
expect_status 404 "$base_url/API/ausente"
expect_status 405 "$base_url/api/auth/login"
expect_status 401 "$base_url/api/admin/users"
expect_status 400 --json '{}' "$base_url/api/auth/login"
jq -e '.message' "$temp_dir/body" >/dev/null

direct_binding="$(docker port "$api" 8080/tcp)"
curl -fsS -D "$temp_dir/headers" -o /dev/null -H 'X-Forwarded-Proto: https' \
  "http://$direct_binding/api/auth/csrf"
grep -qi '^set-cookie: XSRF-TOKEN=' "$temp_dir/headers"
if grep -qi '^set-cookie:.*; secure' "$temp_dir/headers"; then
  printf 'O cabeçalho enviado diretamente foi aceito como HTTPS.\n' >&2
  exit 1
fi

admin="$(request -f --json '{"firstName":"Unified","lastName":"Smoke"}' "$base_url/api/admin/bootstrap")"
credentials="$(jq -ce '{username, password: .generatedPassword}' <<< "$admin")"
login="$(request -f -D "$temp_dir/headers" -c "$temp_dir/cookies" --json "$credentials" "$base_url/api/auth/login")"
csrf_token="$(jq -er '.csrfToken' <<< "$login")"
grep -Eiq '^set-cookie: taskdev-session=.*; secure;.*samesite=lax;.*httponly' "$temp_dir/headers"
grep -Eiq '^set-cookie: XSRF-TOKEN=.*; secure;.*samesite=lax' "$temp_dir/headers"
request -f -b "$temp_dir/cookies" "$base_url/api/admin/users" | jq -e 'length > 0' >/dev/null
expect_status 403 -b "$temp_dir/cookies" -X POST "$base_url/api/auth/logout"
test "$(docker exec "$db" psql -X -U postgres -d taskdev -Atc \
  "SELECT count(*) > 0 AND bool_and(encrypted_xml ~ '^[A-Za-z0-9+/]+=*$') FROM taskdev_security.data_protection_keys")" = t

docker rm -f "$api" >/dev/null
start_app false
wait_for_app
request -f -b "$temp_dir/cookies" "$base_url/api/auth/me" | jq -e '.id' >/dev/null
expect_status 404 --json '{}' "$base_url/api/admin/bootstrap"
request -f -b "$temp_dir/cookies" -H "X-CSRF-TOKEN: $csrf_token" -X POST "$base_url/api/auth/logout" >/dev/null
printf 'SPA, API, autorização, CSRF, HTTPS e sessão após recriação do contêiner validados.\n'
