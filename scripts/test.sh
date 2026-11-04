#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${INGRESDATE_CONNECTION:?Set INGRESDATE_CONNECTION to a restored disposable fixture database}"
dotnet restore IngresDateKit.sln
dotnet build IngresDateKit.sln --no-restore
dotnet test tests/IngresDates.Tests --no-build --logger "trx;LogFileName=unit.trx"
dotnet test tests/IngresDates.PortableTests --no-build --logger "trx;LogFileName=portable.trx"
dotnet test tests/IngresDates.IngresTests --no-build --logger "trx;LogFileName=ingres.trx"
