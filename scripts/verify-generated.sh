#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

verify() {
  local data=$1 output=$2 target=$3
  local generated="$tmp/$(basename "$output")"
  dotnet run --project src/Jef4Net.CodeGen -c Release --no-build -- "$data" "$generated" "$target"
  cmp "$output" "$generated"
}

verify data/jef4j src/Jef4Net/Fujitsu/Internal/Generated/FujitsuTables.g.cs fujitsu
verify data/jef4j src/Jef4Net/Hitachi/Internal/Generated/HitachiTables.g.cs hitachi
verify data/icu/ibm src/Jef4Net/Ibm/Internal/Generated/IbmTables.g.cs ibm
verify data/jbis src/Jef4Net/Unisys/Jbis/Internal/Generated/JbisTables.g.cs jbis
verify data/jef4j src/Jef4Net/Nec/Internal/Generated/NecTables.g.cs nec
verify data/unisys src/Jef4Net/Unisys/Internal/Generated/UnisysTables.g.cs unisys

echo "Verified all generated mapping tables."
