#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if ! docker exec saigonguide-db psql -U saigon -d postgres -Atc "SELECT 1 FROM pg_database WHERE datname='saigonguide_e2e_test'" | grep -q 1; then
  docker exec saigonguide-db createdb -U saigon saigonguide_e2e_test
fi
SG_E2E=1 SG_COMMAND=migrate python3 scripts/env.py dotnet run --project apps/api --no-build --no-launch-profile
SG_E2E=1 SG_COMMAND=seed python3 scripts/env.py dotnet run --project apps/api --no-build --no-launch-profile
if [ "$(docker exec saigonguide-db psql -U saigon -d saigonguide_e2e_test -Atc 'SELECT count(*) FROM users WHERE role='"'Admin'"';')" = "0" ]; then
 SG_E2E=1 SG_COMMAND=admin python3 scripts/env.py dotnet run --project apps/api --no-build --no-launch-profile
fi
SG_E2E=1 python3 scripts/env.py dotnet run --project apps/api --no-build --no-launch-profile > .local/e2e-api.log 2>&1 &
server=$!
trap 'kill "$server" 2>/dev/null || true' EXIT
python3 - <<'PY'
import urllib.request,time
for _ in range(30):
 try:
  if urllib.request.urlopen('http://127.0.0.1:5081/health/ready',timeout=2).status==200:break
 except Exception:time.sleep(1)
else:raise SystemExit('E2E instance is not ready')
PY
SG_WEB_URL=http://127.0.0.1:5081 npm --prefix apps/web run e2e
