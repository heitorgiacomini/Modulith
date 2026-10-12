#!/usr/bin/env bash
# For an isolated CI PostgreSQL service only; uses dedicated scratch databases.
set -euo pipefail
: "${POSTGRES_TEST_CONTAINER:?Set the isolated CI PostgreSQL container ID}"
docker exec "$POSTGRES_TEST_CONTAINER" createdb -U postgres eshop_backup_source
export ConnectionStrings__Database='Host=localhost;Database=eshop_backup_source;Username=postgres;Password=ci-postgres'
export ASPNETCORE_ENVIRONMENT=Development
export Database__Seed=true
dotnet run --project src/Bootstrapper/Api/Api.csproj --configuration Release --no-build --no-launch-profile -- --migrate
docker exec "$POSTGRES_TEST_CONTAINER" pg_dump -U postgres -Fc --no-owner --no-acl -f /tmp/eshop.dump eshop_backup_source
docker exec "$POSTGRES_TEST_CONTAINER" createdb -U postgres eshop_backup_restore
docker exec "$POSTGRES_TEST_CONTAINER" pg_restore -U postgres --exit-on-error --no-owner --no-acl -d eshop_backup_restore /tmp/eshop.dump
for table in 'catalog."Products"' 'catalog."AuditRecords"' 'catalog."AuditDeliveries"'; do
  source_count=$(docker exec "$POSTGRES_TEST_CONTAINER" psql -U postgres -d eshop_backup_source -Atc "SELECT count(*) FROM $table")
  restored_count=$(docker exec "$POSTGRES_TEST_CONTAINER" psql -U postgres -d eshop_backup_restore -Atc "SELECT count(*) FROM $table")
  test "$source_count" -gt 0
  test "$source_count" = "$restored_count"
done
echo 'Database restore preserves seeded products and their transactional audit records.'
