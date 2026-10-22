#!/usr/bin/env python3
"""Back up only this local development instance. Output contains credentials indirectly: keep private."""
from pathlib import Path
import subprocess,tarfile,datetime,os
root=Path(__file__).resolve().parents[1]
folder=root/'.local/backups'/datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
folder.mkdir(parents=True,mode=0o700)
with (folder/'database.dump').open('wb') as f:
 subprocess.run(['docker','exec','saigonguide-db','pg_dump','-U','saigon','-d','saigonguide','-Fc'],stdout=f,check=True)
with tarfile.open(folder/'media-and-keys.tar.gz','w:gz') as t:
 for name in ('media','keys'):
  p=root/'.local'/name
  if p.exists():t.add(p,arcname=name)
for p in folder.iterdir():p.chmod(0o600)
print('Backup complete in',folder.relative_to(root),'; keep it private, never commit it.')
