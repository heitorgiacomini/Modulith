#!/usr/bin/env bash
set -euo pipefail

dotnet tool restore
dotnet tool run nitro -- fusion compose \
  --source-schema-file /workspace/.fusion/catalog \
  --source-schema-file /workspace/.fusion/basket \
  --source-schema-file /workspace/.fusion/ordering \
  --archive /workspace/Bootstrapper/Gateway \
  --include-satisfiability-paths
