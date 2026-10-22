#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
python3 scripts/env.py dotnet run --project apps/api --no-build --no-launch-profile
