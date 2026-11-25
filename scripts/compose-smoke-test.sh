#!/usr/bin/env bash
set -euo pipefail

export AGENT_ENCRYPTION_KEY="$(openssl rand -base64 32)"
export POSTGRES_PASSWORD="$(openssl rand -hex 24)"
export Admin__BootstrapEnabled=true
export Security__SecureCookies=false
export TASKDEV_PORT="${TASKDEV_SMOKE_PORT:-18080}"

base_url="http://127.0.0.1:$TASKDEV_PORT"
temp_dir="$(mktemp -d)"
project_name="taskdev-smoke-$$"
compose=(docker compose --env-file /dev/null -p "$project_name" -f compose.yaml -f compose.postgres.yaml)

cleanup() {
  local status="$?"
  if (( status != 0 )); then
    "${compose[@]}" logs --tail 100 || true
  fi
  "${compose[@]}" down --volumes --remove-orphans || true
  rm -f -- "$temp_dir/cookies"
  rmdir -- "$temp_dir"
}
trap cleanup EXIT

"${compose[@]}" up --build -d
curl -fSs --retry 30 --retry-all-errors --retry-delay 2 --retry-max-time 90 \
  --max-time 10 "$base_url/api/auth/csrf" | jq -e '.csrfToken | length > 0' > /dev/null
curl -fSs "$base_url/" | grep '<div id="app"></div>' > /dev/null

admin="$(curl -fSs --json '{"firstName":"Smoke","lastName":"Test"}' "$base_url/api/admin/bootstrap")"
credentials="$(jq -ce '{username, password: .generatedPassword}' <<< "$admin")"
login="$(curl -fSs -c "$temp_dir/cookies" --json "$credentials" "$base_url/api/auth/login")"
csrf_token="$(jq -er '.csrfToken' <<< "$login")"

request() {
  curl -fSs -b "$temp_dir/cookies" -H "X-CSRF-TOKEN: $csrf_token" "$@"
}

space="$(request --json '{"name":"CI","visibility":"PRIVATE"}' "$base_url/api/spaces/")"
space_id="$(jq -er '.id' <<< "$space")"
task="$(request --json "$(jq -cn --arg spaceId "$space_id" \
  '{spaceId: $spaceId, title: "Tarefa de teste", subtasks: ["Primeira", "Segunda"]}')" \
  "$base_url/api/tasks/")"
task_id="$(jq -er '.id' <<< "$task")"
jq -e '[.subtasks[].title] == ["Primeira", "Segunda"]' <<< "$task" > /dev/null
request "$base_url/api/tasks/$task_id" | jq -e '[.subtasks[].title] == ["Primeira", "Segunda"]' > /dev/null
printf 'Interface, migrações, login e criação da tarefa com subtarefas validados.\n'
