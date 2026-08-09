[CmdletBinding()]
param(
  [string]$ApiUrl = "http://127.0.0.1:5000",
  [string]$ArchivePath = "Bootstrapper/Gateway/gateway.far"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$archive = Join-Path $repositoryRoot $ArchivePath
$schemaRoot = Join-Path $repositoryRoot ".fusion"
$schemas = @("catalog", "basket", "ordering")

Push-Location $repositoryRoot
try {
  foreach ($schema in $schemas) {
    $schemaDirectory = Join-Path $schemaRoot $schema
    $schemaFile = Join-Path $schemaDirectory "schema.graphqls"
    $schemaUrl = "$ApiUrl/graphql/$schema/schema.graphqls"

    New-Item -ItemType Directory -Path $schemaDirectory -Force | Out-Null
    Invoke-WebRequest -UseBasicParsing -Uri $schemaUrl -OutFile $schemaFile

    if ((Get-Item $schemaFile).Length -eq 0) {
      throw "The '$schema' source schema is empty."
    }
  }

  docker run --rm `
    --volume "${repositoryRoot}:/workspace" `
    --workdir /workspace `
    mcr.microsoft.com/dotnet/sdk:10.0 `
    bash /workspace/scripts/compose-fusion-gateway.sh

  if ($LASTEXITCODE -ne 0) {
    throw "Fusion composition failed."
  }
}
finally {
  Pop-Location
}
