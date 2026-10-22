#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p .local /workspace/.tools/dotnet
python3 - <<'PY'
from pathlib import Path
import json,subprocess,hashlib,tarfile
root=Path('/workspace/.tools/dotnet');version=json.loads(Path('global.json').read_text())['sdk']['version']
if not (root/'sdk'/version).is_dir():
 metadata='/tmp/saigon-dotnet-releases.json';subprocess.run(['curl','-fsSL','--retry','2','https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json','-o',metadata],check=True)
 release=json.load(open(metadata));sdks=[s for r in release['releases'] for s in r.get('sdks',[r.get('sdk',{})])];sdk=next(s for s in sdks if s.get('version')==version);artifact=next(a for a in sdk['files'] if a['rid']=='linux-x64' and a['name'].endswith('.tar.gz'))
 archive='/tmp/saigon-dotnet-sdk.tar.gz';subprocess.run(['curl','-fsSL','--retry','2',artifact['url'],'-o',archive],check=True)
 with open(archive,'rb') as f:actual=hashlib.file_digest(f,'sha512').hexdigest()
 if actual.lower()!=artifact['hash'].lower():raise SystemExit('Official SDK checksum mismatch; refusing installation')
 with tarfile.open(archive) as t:t.extractall(root,filter='data')
print('.NET SDK available with verification retained.')
PY
source scripts/toolchain.sh
python3 - <<'PY'
from pathlib import Path
import secrets,subprocess
p=Path('.local/runtime.env')
if not p.exists():p.write_text('ConnectionStrings__Database=Host=127.0.0.1;Port=55432;Database=saigonguide;Username=saigon;Password='+secrets.token_urlsafe(32)+'\nASPNETCORE_ENVIRONMENT=Development\nASPNETCORE_URLS=http://127.0.0.1:5080\n')
p.chmod(0o600)
password=next(line.split('Password=',1)[1] for line in p.read_text().splitlines() if line.startswith('ConnectionStrings__Database='))
exists=subprocess.run(['docker','inspect','saigonguide-db'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0
if exists:subprocess.run(['docker','start','saigonguide-db'],stdout=subprocess.DEVNULL,check=True)
else:subprocess.run(['docker','run','-d','--name','saigonguide-db','-e','POSTGRES_USER=saigon','-e','POSTGRES_DB=saigonguide','-e','POSTGRES_PASSWORD='+password,'-p','127.0.0.1:55432:5432','-v','saigonguide-db:/var/lib/postgresql/data','postgres:17'],stdout=subprocess.DEVNULL,check=True)
b=Path('.local/bootstrap.env')
if not b.exists():b.write_text('Bootstrap__AdminEmail=admin@saigon.guide\nBootstrap__AdminPassword='+secrets.token_urlsafe(24)+'\n')
b.chmod(0o600)
PY
for attempt in $(seq 1 30); do
  if docker exec saigonguide-db pg_isready -U saigon -d saigonguide >/dev/null; then break; fi
  sleep 1
done
command -v ffmpeg >/dev/null
command -v ffprobe >/dev/null
npm --prefix apps/web --cache /tmp/saigon-npm-cache ci --no-audit --no-fund
dotnet restore SaigonGuide.slnx --locked-mode
npm --prefix apps/web run build
dotnet build SaigonGuide.slnx --no-restore
python3 - <<'PY'
from pathlib import Path
import shutil
p=Path('apps/api/wwwroot')
if p.exists():shutil.rmtree(p)
shutil.copytree('apps/web/dist',p)
PY
SG_COMMAND=migrate python3 scripts/env.py dotnet run --project apps/api --no-build --no-launch-profile
SG_COMMAND=seed python3 scripts/env.py dotnet run --project apps/api --no-build --no-launch-profile
if [ "$(docker exec saigonguide-db psql -U saigon -d saigonguide -Atc 'SELECT count(*) FROM users WHERE role='"'Admin'"';')" = "0" ]; then
  SG_COMMAND=admin python3 scripts/env.py dotnet run --project apps/api --no-build --no-launch-profile
fi
printf '%s\n' 'Setup complete. Start with bash scripts/start.sh. Private local credentials: .local/bootstrap.env (never commit).'
