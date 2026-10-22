#!/usr/bin/env bash
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/.tools/dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/.tools/nuget}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
if [ -x /workspace/.tools/dotnet/dotnet ]; then export PATH="/workspace/.tools/dotnet:$PATH"; fi
export TMPDIR="${TMPDIR:-/tmp/saigon-dotnet-tmp}"
export XDG_DATA_HOME="${XDG_DATA_HOME:-/workspace/.tools/xdg-data}"
mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES" "$TMPDIR" "$XDG_DATA_HOME"

export DOTNET_ROOT="${DOTNET_ROOT:-/workspace/.tools/dotnet}"
