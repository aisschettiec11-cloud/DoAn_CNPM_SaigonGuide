#!/usr/bin/env python3
"""Run a command with ignored local config, without shell-evaluating secret values."""
from pathlib import Path
import os,sys,subprocess
root=Path(__file__).resolve().parents[1]
env=os.environ.copy()
files=[root/'.local/runtime.env']
if env.get('SG_COMMAND')=='admin':files.append(root/'.local/bootstrap.env')
for file in files:
 if file.exists():
  for line in file.read_text().splitlines():
   if line and not line.startswith('#') and '=' in line:
    key,value=line.split('=',1);env.setdefault(key,value)
env.setdefault('DOTNET_CLI_HOME','/workspace/.tools/dotnet-home')
env.setdefault('NUGET_PACKAGES','/workspace/.tools/nuget')
env.setdefault('XDG_DATA_HOME','/workspace/.tools/xdg-data')
env.setdefault('TMPDIR','/tmp/saigon-dotnet-tmp')
if env.get('SG_E2E')=='1':
 env['ConnectionStrings__Database']=env['ConnectionStrings__Database'].replace('Database=saigonguide;','Database=saigonguide_e2e_test;')
 env['ASPNETCORE_URLS']='http://127.0.0.1:5081'
 env['ASPNETCORE_ENVIRONMENT']='Testing'
 env['Media__Path']=str(root/'.local/e2e-media')
 env['Keys__Path']=str(root/'.local/e2e-keys')
if 'SG_TEST_DB' not in env and 'ConnectionStrings__Database' in env:env['SG_TEST_DB']=env['ConnectionStrings__Database'].replace('Database=saigonguide;','Database=saigonguide_test;')
env.setdefault('DOTNET_ROOT','/workspace/.tools/dotnet')
env['PATH']='/workspace/.tools/dotnet:/workspace/.tools/bin:'+env['PATH']
env.setdefault('Media__Path',str(root/'.local/media'))
env.setdefault('Keys__Path',str(root/'.local/keys'))
env.setdefault('Content__Path',str(root/'content'))
for k in ['DOTNET_CLI_HOME','NUGET_PACKAGES','XDG_DATA_HOME','TMPDIR']:Path(env[k]).mkdir(parents=True,exist_ok=True)
if len(sys.argv)<2:raise SystemExit('Usage: python3 scripts/env.py COMMAND [ARGS...]')
os.chdir(root)
os.execvpe(sys.argv[1],sys.argv[1:],env)
