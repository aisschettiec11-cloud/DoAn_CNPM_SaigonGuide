#!/usr/bin/env python3
"""Desktop runner: Python 3.11+, .NET 10, Node 24, Docker, FFmpeg on PATH.
No shell evaluation of credentials. Existing config and database are preserved.
"""
import argparse
import json
import os
from pathlib import Path
import secrets
import shutil
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
os.chdir(ROOT)
PRIVATE = ROOT / '.local'
PRIVATE.mkdir(exist_ok=True)
ENV = os.environ.copy()
# Reuse the verified cloud toolchain when present; desktops use their normal PATH.
CLOUD = Path('/workspace/.tools')
if (CLOUD / 'dotnet/dotnet').exists():
    ENV['PATH'] = str(CLOUD / 'dotnet') + os.pathsep + ENV['PATH']
    ENV.setdefault('DOTNET_ROOT', str(CLOUD / 'dotnet'))
    ENV.setdefault('DOTNET_CLI_HOME', str(CLOUD / 'dotnet-home'))
    ENV.setdefault('NUGET_PACKAGES', str(CLOUD / 'nuget'))
    ENV.setdefault('XDG_DATA_HOME', str(CLOUD / 'xdg-data'))
ENV.setdefault('DOTNET_CLI_TELEMETRY_OPTOUT', '1')
ENV.setdefault('npm_config_cache', str(PRIVATE / 'npm-cache'))

def run(*args, **kwargs):
    # npm is a .cmd launcher on Windows; resolve it without shell=True.
    args = list(args)
    args[0] = shutil.which(args[0], path=ENV['PATH']) or args[0]
    return subprocess.run(args, env=ENV, check=True, **kwargs)

def save(path, text):
    path.write_text(text, encoding='utf-8')
    path.chmod(0o600)

def load(path):
    if path.exists():
        for line in path.read_text(encoding='utf-8').splitlines():
            if line and not line.startswith('#') and '=' in line:
                key, value = line.split('=', 1)
                ENV.setdefault(key, value)

def configure():
    load(PRIVATE / 'runtime.env')
    if 'ConnectionStrings__Database' not in ENV:
        raise SystemExit('Run: python scripts/local.py setup')
    ENV.setdefault('ASPNETCORE_ENVIRONMENT', 'Development')
    ENV.setdefault('ASPNETCORE_URLS', 'http://127.0.0.1:5080')
    for key, value in {'Media__Path': PRIVATE / 'media', 'Keys__Path': PRIVATE / 'keys', 'Content__Path': ROOT / 'content'}.items():
        ENV.setdefault(key, str(value))

def api(command=None):
    previous = ENV.pop('SG_COMMAND', None)
    if command:
        ENV['SG_COMMAND'] = command
    try:
        run('dotnet', 'run', '--project', 'apps/api', '--no-build', '--no-launch-profile')
    finally:
        ENV.pop('SG_COMMAND', None)
        if previous is not None:
            ENV['SG_COMMAND'] = previous

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('command', choices=['setup', 'start', 'dev', 'test'])
command = parser.parse_args().command
try:
    if command == 'setup':
        for tool in ['dotnet', 'node', 'npm', 'docker', 'ffmpeg', 'ffprobe']:
            if not shutil.which(tool, path=ENV['PATH']):
                raise SystemExit(f'Missing {tool}; see docs/INSTALLATION.md')
        run('dotnet', '--version')
        run('node', '--version')
        runtime = PRIVATE / 'runtime.env'
        if not runtime.exists():
            save(runtime, 'ConnectionStrings__Database=Host=127.0.0.1;Port=55432;Database=saigonguide;Username=saigon;Password=' + secrets.token_urlsafe(32) + '\nASPNETCORE_ENVIRONMENT=Development\nASPNETCORE_URLS=http://127.0.0.1:5080\n')
        configure()
        # This local helper supports the generated DB identity only; custom DBs
        # should use the manual procedure documented in INSTALLATION.md.
        connection = ENV['ConnectionStrings__Database']
        if not connection.startswith('Host=127.0.0.1;Port=55432;Database=saigonguide;Username=saigon;Password='):
            raise SystemExit('Custom database configuration detected; use the manual setup procedure.')
        password = connection.split('Password=', 1)[1].rstrip(';')
        result = run('docker', 'ps', '-a', '--format', '{{.Names}}', capture_output=True, text=True)
        if 'saigonguide-db' in result.stdout.splitlines():
            run('docker', 'start', 'saigonguide-db', stdout=subprocess.DEVNULL)
        else:
            db_env = ENV.copy()
            db_env['POSTGRES_PASSWORD'] = password
            executable = shutil.which('docker', path=ENV['PATH'])
            subprocess.run([executable, 'run', '-d', '--name', 'saigonguide-db', '-e', 'POSTGRES_USER=saigon', '-e', 'POSTGRES_DB=saigonguide', '-e', 'POSTGRES_PASSWORD', '-p', '127.0.0.1:55432:5432', '-v', 'saigonguide-db:/var/lib/postgresql/data', 'postgres:17'], env=db_env, check=True, stdout=subprocess.DEVNULL)
        for attempt in range(30):
            ready = subprocess.run(['docker', 'exec', 'saigonguide-db', 'pg_isready', '-U', 'saigon'], env=ENV, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            if ready.returncode == 0:
                break
            time.sleep(1)
        else:
            raise SystemExit('PostgreSQL is not ready; inspect Docker logs without deleting volumes.')
        bootstrap = PRIVATE / 'bootstrap.env'
        if not bootstrap.exists():
            save(bootstrap, 'Bootstrap__AdminEmail=admin@saigon.guide\nBootstrap__AdminPassword=' + secrets.token_urlsafe(24) + '\n')
        ide = {'ConnectionStrings': {'Database': connection}, 'Media': {'Path': ENV['Media__Path']}, 'Keys': {'Path': ENV['Keys__Path']}, 'Content': {'Path': ENV['Content__Path']}}
        save(PRIVATE / 'ide-settings.json', json.dumps(ide, indent=2))
        run('npm', 'ci', '--prefix', 'apps/web', '--no-audit', '--no-fund')
        run('dotnet', 'restore', 'SaigonGuide.slnx', '--locked-mode')
        run('npm', '--prefix', 'apps/web', 'run', 'build')
        shutil.copytree(ROOT / 'apps/web/dist', ROOT / 'apps/api/wwwroot', dirs_exist_ok=True)
        run('dotnet', 'build', 'SaigonGuide.slnx', '--no-restore')
        api('migrate')
        api('seed')
        count = run('docker', 'exec', 'saigonguide-db', 'psql', '-U', 'saigon', '-d', 'saigonguide', '-Atc', "SELECT count(*) FROM users WHERE role='Admin';", capture_output=True, text=True)
        if count.stdout.strip() == '0':
            load(bootstrap)
            api('admin')
        print('Setup complete. Private credentials: .local/bootstrap.env. Start: python scripts/local.py start')
    elif command == 'dev':
        run('npm', '--prefix', 'apps/web', 'run', 'dev')
    else:
        configure()
        if command == 'start':
            api()
        else:
            connection = ENV['ConnectionStrings__Database']
            if 'Database=saigonguide;' not in connection:
                raise SystemExit('Custom database: explicitly configure a separate _test DB and run dotnet test manually.')
            ENV['SG_TEST_DB'] = connection.replace('Database=saigonguide;', 'Database=saigonguide_test;')
            existing = run('docker', 'exec', 'saigonguide-db', 'psql', '-U', 'saigon', '-d', 'postgres', '-Atc', "SELECT 1 FROM pg_database WHERE datname='saigonguide_test'", capture_output=True, text=True)
            if existing.stdout.strip() != '1':
                run('docker', 'exec', 'saigonguide-db', 'createdb', '-U', 'saigon', 'saigonguide_test')
            run('dotnet', 'test', 'SaigonGuide.slnx', '--logger', 'trx')
            run('npm', '--prefix', 'apps/web', 'test')
except subprocess.CalledProcessError as error:
    # Do not echo command arguments, which can contain private configuration.
    raise SystemExit(f'Command failed (exit {error.returncode}); inspect the output above.')
except KeyboardInterrupt:
    raise SystemExit(130)
