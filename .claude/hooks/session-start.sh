#!/usr/bin/env bash
# Prepares a Claude Code cloud session: .NET 10 SDK, Backlog.md CLI, restored packages.
# Does nothing on a local machine.
set -euo pipefail
[ "${CLAUDE_CODE_REMOTE:-}" = "true" ] || exit 0

if ! command -v dotnet >/dev/null 2>&1; then
  export DEBIAN_FRONTEND=noninteractive
  apt-get update -qq >/dev/null
  apt-get install -y -qq dotnet-sdk-10.0 >/dev/null
fi

command -v backlog >/dev/null 2>&1 || npm install -g --silent backlog.md@1

cd "${CLAUDE_PROJECT_DIR:-$(pwd)}"
dotnet restore JiraTime.slnx --verbosity quiet
