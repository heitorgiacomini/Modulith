#!/usr/bin/env bash
set -euo pipefail

schema_root="${SCHEMA_ROOT:-/workspace/.fusion}"
archive_directory="${ARCHIVE_DIRECTORY:-/workspace/Bootstrapper/Gateway}"
schemas=(catalog basket ordering)

if [[ -n "${API_URL:-}" ]]; then
  rm -rf "$schema_root"
  mkdir -p "$schema_root"

  for schema in "${schemas[@]}"; do
    schema_directory="$schema_root/$schema"
    schema_url="${API_URL%/}/graphql/$schema/schema.graphqls"
    mkdir -p "$schema_directory"
    cat > "$schema_directory/schema-settings.json" <<EOF
{
  "name": "$schema",
  "transports": {
    "http": {
      "url": "${API_URL%/}/graphql/$schema"
    }
  }
}
EOF

    for attempt in {1..30}; do
      if curl --fail --silent --show-error "$schema_url" \
        --output "$schema_directory/schema.graphqls" \
        && [[ -s "$schema_directory/schema.graphqls" ]]; then
        break
      fi

      if [[ "$attempt" -eq 30 ]]; then
        echo "Failed to download source schema '$schema' from $schema_url." >&2
        exit 1
      fi

      sleep 1
    done
  done
fi

mkdir -p "$archive_directory"
rm -f "$archive_directory/gateway.far"

dotnet tool restore
dotnet tool run nitro -- fusion compose \
  --source-schema-file "$schema_root/catalog/schema.graphqls" \
  --source-schema-file "$schema_root/basket/schema.graphqls" \
  --source-schema-file "$schema_root/ordering/schema.graphqls" \
  --archive "$archive_directory" \
  --include-satisfiability-paths

test -s "$archive_directory/gateway.far"
