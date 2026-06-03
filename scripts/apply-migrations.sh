#!/usr/bin/env bash
# Apply EF Core migrations for AiJobMarketIntelligence (MySQL).
# Run from anywhere; paths are relative to the repo root.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INFRA="$ROOT/api/src/Infrastructure/AiJobMarketIntelligence.Infrastructure/AiJobMarketIntelligence.Infrastructure.csproj"
API="$ROOT/api/src/Api/AiJobMarketIntelligence.Api/AiJobMarketIntelligence.Api.csproj"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Error: dotnet SDK not found." >&2
  exit 1
fi

if ! dotnet ef --version >/dev/null 2>&1; then
  echo "Installing dotnet-ef global tool..."
  dotnet tool install --global dotnet-ef
fi

echo "Applying AiJobContext migrations..."
dotnet ef database update \
  --project "$INFRA" \
  --startup-project "$API" \
  --context AiJobContext

echo "Applying AuthDbContext migrations..."
dotnet ef database update \
  --project "$INFRA" \
  --startup-project "$API" \
  --context AuthDbContext

echo "Done."
