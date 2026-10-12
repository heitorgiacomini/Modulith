#!/bin/sh
set -eu
psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set=ON_ERROR_STOP=1 \
  --set=migration_password="$MIGRATION_PASSWORD" --set=catalog_password="$CATALOG_PASSWORD" \
  --set=basket_password="$BASKET_PASSWORD" --set=ordering_password="$ORDERING_PASSWORD" \
  --set=accounts_password="$ACCOUNTS_PASSWORD" --set=keycloak_password="$KEYCLOAK_DB_PASSWORD" <<'SQL'
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
CREATE ROLE eshop_migrator LOGIN PASSWORD :'migration_password';
CREATE ROLE catalog_app LOGIN PASSWORD :'catalog_password';
CREATE ROLE basket_app LOGIN PASSWORD :'basket_password';
CREATE ROLE ordering_app LOGIN PASSWORD :'ordering_password';
CREATE ROLE accounts_app LOGIN PASSWORD :'accounts_password';
CREATE ROLE keycloak_app LOGIN PASSWORD :'keycloak_password';
GRANT USAGE, CREATE ON SCHEMA public TO eshop_migrator;
CREATE SCHEMA catalog AUTHORIZATION eshop_migrator;
CREATE SCHEMA basket AUTHORIZATION eshop_migrator;
CREATE SCHEMA ordering AUTHORIZATION eshop_migrator;
CREATE SCHEMA accounts AUTHORIZATION eshop_migrator;
CREATE SCHEMA keycloak AUTHORIZATION keycloak_app;
ALTER ROLE catalog_app SET search_path = catalog;
ALTER ROLE basket_app SET search_path = basket;
ALTER ROLE ordering_app SET search_path = ordering;
ALTER ROLE accounts_app SET search_path = accounts;
ALTER ROLE keycloak_app SET search_path = keycloak;
SQL
