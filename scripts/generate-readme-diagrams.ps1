param([switch]$PreserveExisting)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$width = 1600
$height = 1000
function Start-Diagram($title, $subtitle) {
    $script:bitmap = New-Object System.Drawing.Bitmap $width, $height
    $script:graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = 'AntiAlias'
    $graphics.TextRenderingHint = 'AntiAliasGridFit'
    $graphics.Clear([System.Drawing.ColorTranslator]::FromHtml('#f4f7fc'))
    $script:svg = [System.Text.StringBuilder]::new()
    [void]$svg.AppendLine('<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="1000" viewBox="0 0 1600 1000" role="img">')
    [void]$svg.AppendLine('<rect width="1600" height="1000" fill="#f4f7fc"/>')
    Label 65 45 $title 38 '#13243b' $true
    Label 65 103 $subtitle 21 '#52647c'
}
function Label($x, $y, $text, $size = 20, $color = '#13243b', $bold = $false) {
    $style = if ($bold) { [System.Drawing.FontStyle]::Bold } else { [System.Drawing.FontStyle]::Regular }
    $font = [System.Drawing.Font]::new('Segoe UI', $size, $style, [System.Drawing.GraphicsUnit]::Pixel)
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($color))
    $graphics.DrawString($text, $font, $brush, [float]$x, [float]$y)
    $escaped = [System.Security.SecurityElement]::Escape($text)
    $weight = if ($bold) { 700 } else { 400 }
    [void]$svg.AppendLine("<text x='$x' y='$($y + $size)' font-family='Segoe UI,Arial,sans-serif' font-size='$size' font-weight='$weight' fill='$color'>$escaped</text>")
    $font.Dispose(); $brush.Dispose()
}
function Box($x, $y, $w, $h, $title, $lines, $accent = '#2563eb') {
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml($accent), 2)
    $graphics.FillRectangle($brush, $x, $y, $w, $h)
    $graphics.DrawRectangle($pen, $x, $y, $w, $h)
    [void]$svg.AppendLine("<rect x='$x' y='$y' width='$w' height='$h' fill='white' stroke='$accent' stroke-width='2'/>")
    Label ($x+22) ($y+18) $title 25 $accent $true
    $offset = $y+63
    foreach ($line in $lines) { Label ($x+22) $offset $line 19 '#52647c'; $offset += 31 }
    $brush.Dispose(); $pen.Dispose()
}
function Arrow($x1, $y1, $x2, $y2, $color = '#52647c', $dashed = $false) {
    $pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml($color), 3)
    if ($dashed) { $pen.DashStyle = 'Dash' }
    $graphics.DrawLine($pen, $x1, $y1, $x2, $y2)
    $angle = [Math]::Atan2($y2-$y1, $x2-$x1)
    $a = [System.Drawing.PointF]::new($x2-13*[Math]::Cos($angle-0.5), $y2-13*[Math]::Sin($angle-0.5))
    $b = [System.Drawing.PointF]::new($x2-13*[Math]::Cos($angle+0.5), $y2-13*[Math]::Sin($angle+0.5))
    $graphics.DrawLine($pen, [System.Drawing.PointF]::new($x2,$y2), $a)
    $graphics.DrawLine($pen, [System.Drawing.PointF]::new($x2,$y2), $b)
    $dash = if ($dashed) { "stroke-dasharray='9 6'" } else { '' }
    [void]$svg.AppendLine("<path d='M$x1 $y1 L$x2 $y2 M$($a.X) $($a.Y) L$x2 $y2 L$($b.X) $($b.Y)' fill='none' stroke='$color' stroke-width='3' $dash/>")
    $pen.Dispose()
}
function Save-Diagram($png, $source) {
    foreach ($name in @($png, $source)) {
        $path = Join-Path $root $name
        if ($PreserveExisting -and (Test-Path -LiteralPath $path)) {
            $backup = "$path.old"
            if (Test-Path -LiteralPath $backup) { throw "Backup already exists: $backup" }
            Copy-Item -LiteralPath $path -Destination $backup
        }
    }
    [void]$svg.AppendLine('</svg>')
    [System.IO.File]::WriteAllText((Join-Path $root $source), $svg.ToString())
    $bitmap.Save((Join-Path $root $png), [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose()
}

Start-Diagram 'E-Shop | Runtime architecture' 'Docker Compose: browser traffic, one modular API, and persistent infrastructure'
Box 65 190 420 145 'Angular 21 / PrimeNG' @('Browser application :4200', 'GraphQL + REST through gateway')
Box 1115 190 420 145 'Keycloak :9090' @('OIDC / JWT | eshoprealm', 'Selected organization + user identity') '#be185d'
Arrow 485 258 1115 258 '#be185d'
Label 675 216 'Sign in / tokens' 19 '#be185d'
Box 65 420 420 160 'Fusion gateway :5002' @('/graphql -> composed graph', '/api -> REST reverse proxy', 'Forwards caller authorization') '#7c3aed'
Arrow 275 335 275 420
Box 645 420 890 160 'ASP.NET Core / .NET 10 API :5004' @('Catalog | Basket | Ordering | Accounts', 'Three GraphQL source schemas + Carter REST endpoints', 'One process; module-owned EF Core contexts') '#0891b2'
Arrow 485 500 645 500
Box 65 695 420 155 'Gateway configurator' @('Fetches source schemas at startup', 'Writes gateway.far to shared volume', 'One-shot job; exits with code 0') '#7c3aed'
Arrow 275 695 275 580 '#7c3aed' $true
Box 645 695 270 155 'PostgreSQL' @('Shared database', 'Separate schemas', ':5434 on host') '#15803d'
Box 955 695 270 155 'Redis' @('Basket cache', ':6379 on host') '#15803d'
Box 1265 695 270 155 'RabbitMQ' @('Integration events', 'Management :15672') '#c2410c'
Arrow 780 580 780 695 '#15803d'
Arrow 1090 580 1090 695 '#15803d'
Arrow 1400 580 1400 695 '#c2410c'
Label 65 925 'Solid arrows: runtime traffic. Dashed arrow: startup artifact. Telemetry is detailed in the observability diagram.' 20
Save-Diagram 'ModularMonolithArchitecture.png' 'ModularMonolithArchitecture.svg'

Start-Diagram 'E-Shop | Module boundaries and ownership' 'Four business modules in one host; contracts and integration events connect their boundaries'
Box 65 190 710 190 'Catalog' @('Products, pricing, REST + GraphQL', 'Owns catalog schema / CatalogDbContext', 'Exposes Catalog.Contracts for product lookup')
Box 825 190 710 190 'Basket' @('Carts, checkout, REST + GraphQL', 'Owns basket schema / BasketDbContext + Redis', 'Checkout event persisted in transactional outbox') '#15803d'
Box 65 435 710 190 'Ordering' @('Orders, queries, REST + GraphQL', 'Owns ordering schema / OrderingDbContext', 'Consumes basket checkout; enforces authorization') '#c2410c'
Box 825 435 710 190 'Accounts' @('Authenticated /account/me REST endpoints', 'Owns accounts schema / AccountsDbContext', 'Preferences, addresses, saved payment methods') '#be185d'
Box 65 690 1470 220 'Communication contracts' @('Basket -> Catalog.Contracts -> MediatR: GetProductByIdQuery (in process)', 'Catalog -> RabbitMQ -> Basket: ProductPriceChangedIntegrationEvent', 'Basket outbox -> RabbitMQ -> Ordering: BasketCheckoutIntegrationEvent', 'Shared technical building blocks: CQRS, DDD, EF interceptors, messaging, telemetry and auditing') '#7c3aed'
Label 65 945 'Architecture tests check assembly boundaries. Each module owns its data access; organization UUIDs identify tenants.' 20
Save-Diagram 'ModuleBoundaries&Ownership.png' 'ModuleBoundaries&Ownership.svg'

Start-Diagram 'E-Shop | Observability pipeline' 'Alloy collects; Loki, Tempo and Prometheus store; Grafana brings the signals together'
Box 65 190 550 165 'API + Fusion gateway' @('OTLP traces and metrics', 'Opt-in Docker JSON application logs', 'API audit NDJSON on shared volume') '#0891b2'
Box 900 190 635 165 'Grafana Alloy :12345' @('OTLP receivers :4317 / :4318', 'Docker discovery + audit file collection', 'Diagnostics UI; routes each signal') '#7c3aed'
Arrow 615 272 900 272 '#7c3aed'
Box 65 480 440 175 'Loki' @('Application + audit logs', 'LogQL queries', 'Explore through Grafana') '#2563eb'
Box 580 480 440 175 'Tempo' @('Distributed request traces', 'TraceQL queries', 'Span metrics + service graphs') '#c2410c'
Box 1095 480 440 175 'Prometheus' @('Time-series metrics + rules', 'PromQL / internal query UI', 'Scrapes + remote-write receiver') '#15803d'
Arrow 1010 355 285 480 '#2563eb'
Arrow 1215 355 800 480 '#c2410c'
Arrow 1430 355 1315 480 '#15803d'
Arrow 1020 570 1095 570 '#15803d'
Box 350 790 900 125 'Grafana :5601 | Main exploration UI' @('Dashboards, logs, traces, metrics, audit search and correlated links') '#be185d'
Arrow 285 655 500 790 '#be185d'
Arrow 800 655 800 790 '#be185d'
Arrow 1315 655 1100 790 '#be185d'
Label 65 945 'Loki, Tempo and Prometheus have no published host ports in this Compose stack. Grafana queries them internally.' 20
Save-Diagram 'image.png' 'architecture.svg'
