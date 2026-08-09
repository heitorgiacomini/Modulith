$ErrorActionPreference = 'Continue'

docker compose ps -a

foreach ($schema in 'catalog', 'basket', 'ordering') {
  foreach ($suffix in 'schema.graphqls', '?sdl') {
    $uri = "http://127.0.0.1:5000/graphql/$schema/$suffix".Replace('/?sdl', '?sdl')
    try {
      $response = Invoke-WebRequest -UseBasicParsing -Uri $uri -TimeoutSec 10
      Write-Host "$uri -> $($response.StatusCode), $($response.Content.Length) bytes"
    }
    catch {
      Write-Host "$uri -> ERROR: $($_.Exception.Message)"
    }
  }
}
