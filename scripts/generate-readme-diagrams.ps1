param([switch]$PreserveExisting)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$sources = Join-Path $PSScriptRoot 'diagram-sources'
function Archive-Asset($path) {
    if (!(Test-Path -LiteralPath $path)) { return }
    $directory = Split-Path $path -Parent
    $stem = [IO.Path]::GetFileNameWithoutExtension($path)
    $extension = [IO.Path]::GetExtension($path)
    $backup = Join-Path $directory "$stem.old$extension"
    $n = 1
    while (Test-Path -LiteralPath $backup) { $backup = Join-Path $directory "$stem.$n.old$extension"; $n++ }
    $hash = (Get-FileHash -LiteralPath $path).Hash
    Move-Item -LiteralPath $path -Destination $backup
    if ((Get-FileHash -LiteralPath $backup).Hash -ne $hash) { throw "Archive hash mismatch: $backup" }
    Write-Host "Preserved and verified: $backup"
}
$names = @('ModularMonolithArchitecture.png','ModularMonolithArchitecture.svg','ModuleBoundaries&Ownership.png','ModuleBoundaries&Ownership.svg','image.png','architecture.svg')
# Validate inputs before archiving any published output.
foreach ($name in @('ModularMonolithArchitecture.png','ModuleBoundaries&Ownership.png','architecture.svg')) {
    if (!(Test-Path -LiteralPath (Join-Path $sources $name))) { throw "Missing diagram master: $name" }
}
$edge = @('C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe','C:/Program Files/Microsoft/Edge/Application/msedge.exe') | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (!$edge) { throw 'Microsoft Edge is required to render the editable architecture SVG.' }
if ($PreserveExisting) {
    foreach ($name in $names) { Archive-Asset (Join-Path $root $name); Archive-Asset (Join-Path $root "docs/images/$name") }
}
$descriptions = @{
    'ModularMonolithArchitecture' = 'Angular client, Fusion and YARP gateway, four modules in one API process, persistence, messaging, Keycloak, startup jobs, and Grafana observability.'
    'ModuleBoundaries&Ownership' = 'Four module columns with domain, application, infrastructure, owned PostgreSQL schemas, contracts, event routes, and Basket Redis cache.'
}
foreach ($stem in $descriptions.Keys) {
    $png = Join-Path $root "$stem.png"
    Copy-Item -LiteralPath (Join-Path $sources "$stem.png") -Destination $png -Force
    $bitmap = [Drawing.Image]::FromFile($png)
    $width = $bitmap.Width; $height = $bitmap.Height; $bitmap.Dispose()
    $data = [Convert]::ToBase64String([IO.File]::ReadAllBytes($png))
    $title = [Security.SecurityElement]::Escape($stem)
    $desc = [Security.SecurityElement]::Escape($descriptions[$stem])
    # SVG companions retain the illustrated raster masters without replacing their visual design.
    $svg = "<svg xmlns='http://www.w3.org/2000/svg' width='$width' height='$height' viewBox='0 0 $width $height' role='img' aria-labelledby='title desc'><title id='title'>$title</title><desc id='desc'>$desc</desc><image width='$width' height='$height' href='data:image/png;base64,$data'/></svg>"
    [IO.File]::WriteAllText((Join-Path $root "$stem.svg"), $svg)
}
$vector = Join-Path $root 'architecture.svg'
Copy-Item -LiteralPath (Join-Path $sources 'architecture.svg') -Destination $vector -Force
$screenshot = Join-Path $root 'image.png'
$profile = Join-Path $env:TEMP 'modulith-svg-render-profile'
$uri = ([Uri]$vector).AbsoluteUri
$renderArguments = @('--headless','--disable-gpu','--hide-scrollbars','--no-sandbox','--force-device-scale-factor=1','--window-size=1800,1080',"--user-data-dir=`"$profile`"","--screenshot=`"$screenshot`"",$uri)
$process = Start-Process -FilePath $edge -ArgumentList $renderArguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $screenshot)) { throw 'SVG rendering failed.' }
foreach ($name in $names) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $root "docs/images/$name") -Force
    if ((Get-FileHash -LiteralPath (Join-Path $root $name)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root "docs/images/$name")).Hash) { throw "Asset copy mismatch: $name" }
}
Write-Host 'Published illustrated masters, SVG companions, and editable two-panel architecture diagram.'
