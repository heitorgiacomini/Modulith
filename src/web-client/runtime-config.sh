#!/bin/sh
set -eu
: "${KEYCLOAK_PUBLIC_URL:?KEYCLOAK_PUBLIC_URL is required}"
jq -n --arg url "$KEYCLOAK_PUBLIC_URL" \
  '{apiUrl:"/api",graphqlUrl:"/graphql",keycloak:{url:$url,realm:"eshoprealm",clientId:"myclient"}}' > /tmp/config.json
