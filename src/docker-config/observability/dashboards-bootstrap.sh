#!/bin/sh
set -eu

dashboards_url="http://opensearch-dashboards:5601"
admin_auth="admin:${OPENSEARCH_ADMIN_PASSWORD}"

attempt=0
until curl --fail --silent --user "${admin_auth}" \
  "${dashboards_url}/api/status" >/dev/null 2>&1; do
  attempt=$((attempt + 1))
  if [ "${attempt}" -ge 60 ]; then
    echo "OpenSearch Dashboards did not become ready." >&2
    exit 1
  fi
  sleep 2
done

imported_bundle=false
for dashboard_bundle in /dashboards/*.ndjson; do
  [ -f "${dashboard_bundle}" ] || continue
  imported_bundle=true
  response_file="$(mktemp)"
  echo "Importing OpenSearch Dashboards bundle $(basename "${dashboard_bundle}")."
  if ! curl --fail-with-body --silent --show-error --user "${admin_auth}" \
      --header "osd-xsrf: true" \
      --header "securitytenant: global" \
      --form "file=@${dashboard_bundle}" \
      --output "${response_file}" \
      "${dashboards_url}/api/saved_objects/_import?overwrite=true"; then
    cat "${response_file}" >&2
    rm -f "${response_file}"
    exit 1
  fi
  if ! grep -q '"success":true' "${response_file}"; then
    cat "${response_file}" >&2
    rm -f "${response_file}"
    exit 1
  fi
  rm -f "${response_file}"
done

if [ "${imported_bundle}" != true ]; then
  echo "No OpenSearch Dashboards bundles were found." >&2
  exit 1
fi

# Default to the application log view and event-first dashboard so the
# read-only operator lands on the primary investigation workflow.
curl --fail-with-body --silent --show-error --user "${admin_auth}" \
  --request POST \
  --header "Content-Type: application/json" \
  --header "osd-xsrf: true" \
  --header "securitytenant: global" \
  --data '{"changes":{"defaultIndex":"eshop-app-logs","defaultRoute":"/app/dashboards#/view/eshop-event-explorer-dashboard","visualization:enablePluginAugmentation":false}}' \
  "${dashboards_url}/api/opensearch-dashboards/settings" >/dev/null

delete_obsolete_saved_object() {
  object_type="$1"
  object_id="$2"
  status_code="$(curl --silent --output /dev/null --write-out '%{http_code}' \
    --user "${admin_auth}" \
    --request DELETE \
    --header "osd-xsrf: true" \
    --header "securitytenant: global" \
    "${dashboards_url}/api/saved_objects/${object_type}/${object_id}")"

  case "${status_code}" in
    200|404) ;;
    *)
      echo "Could not remove obsolete saved object ${object_type}/${object_id} (HTTP ${status_code})." >&2
      exit 1
      ;;
  esac
}

# Remove objects managed by the previous dashboard bundle. The replacement
# objects above use current OpenTelemetry metric names and index expressions.
delete_obsolete_saved_object dashboard eshop-runtime-dashboard
delete_obsolete_saved_object visualization eshop-request-rate
delete_obsolete_saved_object visualization eshop-error-count
delete_obsolete_saved_object visualization eshop-request-duration
delete_obsolete_saved_object visualization eshop-runtime-heap
delete_obsolete_saved_object visualization eshop-postgres-duration
delete_obsolete_saved_object visualization eshop-rabbitmq-duration
delete_obsolete_saved_object visualization eshop-dependency-duration

echo "OpenSearch observability dashboards and data views imported."
