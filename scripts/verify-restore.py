#!/usr/bin/env python3
"""Restore latest backup into a separate disposable verification database; never overwrite the live DB."""
from pathlib import Path
import subprocess,tarfile,tempfile
root=Path(__file__).resolve().parents[1]
backups=sorted((root/'.local/backups').iterdir());assert backups,'Run backup.py first'
b=backups[-1];name='saigonguide_restore_test'
subprocess.run(['docker','exec','saigonguide-db','dropdb','-U','saigon','--if-exists',name],check=True)
subprocess.run(['docker','exec','saigonguide-db','createdb','-U','saigon',name],check=True)
with (b/'database.dump').open('rb') as f:subprocess.run(['docker','exec','-i','saigonguide-db','pg_restore','-U','saigon','-d',name,'--no-owner'],stdin=f,check=True)
result=subprocess.check_output(['docker','exec','saigonguide-db','psql','-U','saigon','-d',name,'-At','-c',"SELECT (SELECT count(*) FROM places WHERE status='published'),(SELECT count(*) FROM audio_assets);"],text=True).strip()
with tempfile.TemporaryDirectory(prefix='sg-restore-',dir='/tmp') as temp:
 with tarfile.open(b/'media-and-keys.tar.gz') as t:t.extractall(temp,filter='data')
 files=list((Path(temp)/'media').glob('*.mp3'))
 assert len(files)>=12,'Expected at least twelve restored MP3 assets'
 print('Restore verified in separate database:',result,'; MP3 files:',len(files))
