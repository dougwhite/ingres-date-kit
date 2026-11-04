$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
if (-not $env:INGRESDATE_CONNECTION) {
    throw 'Set INGRESDATE_CONNECTION to a restored disposable fixture database.'
}
function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet exited with code $LASTEXITCODE" }
}
Invoke-DotNet restore IngresDateKit.sln
Invoke-DotNet build IngresDateKit.sln --no-restore
Invoke-DotNet test tests/IngresDates.Tests --no-build --logger 'trx;LogFileName=unit.trx'
Invoke-DotNet test tests/IngresDates.PortableTests --no-build --logger 'trx;LogFileName=portable.trx'
Invoke-DotNet test tests/IngresDates.IngresTests --no-build --logger 'trx;LogFileName=ingres.trx'
