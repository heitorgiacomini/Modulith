#!/bin/sh
set -eu

opensearch_url="https://opensearch:9200"
admin_auth="admin:${OPENSEARCH_ADMIN_PASSWORD}"

request_file() {
  method="$1"
  path="$2"
  file="$3"
  response_file="$(mktemp)"
  echo "Configuring OpenSearch ${path}."
  if ! curl --fail-with-body --silent --show-error --insecure \
      --user "${admin_auth}" \
      --request "${method}" \
      --header "Content-Type: application/json" \
      --data-binary "@${file}" \
      --output "${response_file}" \
      "${opensearch_url}/${path}"; then
    cat "${response_file}" >&2
    rm -f "${response_file}"
    return 1
  fi
  rm -f "${response_file}"
}

json_escape() {
  printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

ingest_password="$(json_escape "${OPENSEARCH_INGEST_PASSWORD}")"
operator_password="$(json_escape "${OPENSEARCH_OPERATOR_PASSWORD}")"

request_file PUT "_plugins/_security/api/roles/eshop_ingest" \
  /bootstrap/security/ingest-role.json
request_file PUT "_plugins/_security/api/roles/eshop_observability_operator" \
  /bootstrap/security/operator-role.json

curl --fail --silent --show-error --insecure --user "${admin_auth}" \
  --request PUT --header "Content-Type: application/json" \
  --data "{\"password\":\"${ingest_password}\",\"backend_roles\":[\"eshop_ingest\"]}" \
  "${opensearch_url}/_plugins/_security/api/internalusers/${OPENSEARCH_INGEST_USERNAME}" \
  >/dev/null
curl --fail --silent --show-error --insecure --user "${admin_auth}" \
  --request PUT --header "Content-Type: application/json" \
  --data "{\"password\":\"${operator_password}\",\"backend_roles\":[\"kibanauser\",\"eshop_observability_operator\"]}" \
  "${opensearch_url}/_plugins/_security/api/internalusers/${OPENSEARCH_OPERATOR_USERNAME}" \
  >/dev/null

curl --fail --silent --show-error --insecure --user "${admin_auth}" \
  --request PUT --header "Content-Type: application/json" \
  --data '{"backend_roles":["eshop_ingest"]}' \
  "${opensearch_url}/_plugins/_security/api/rolesmapping/eshop_ingest" >/dev/null
curl --fail --silent --show-error --insecure --user "${admin_auth}" \
  --request PUT --header "Content-Type: application/json" \
  --data '{"backend_roles":["eshop_observability_operator"]}' \
  "${opensearch_url}/_plugins/_security/api/rolesmapping/eshop_observability_operator" \
  >/dev/null

install_policy_once() {
  policy_id="$1"
  policy_file="$2"
  if curl --fail --silent --insecure --user "${admin_auth}" \
    "${opensearch_url}/_plugins/_ism/policies/${policy_id}" \
    >/dev/null 2>&1; then
    return
  fi
  echo "Installing OpenSearch policy ${policy_id}."
  request_file PUT "_plugins/_ism/policies/${policy_id}" "${policy_file}"
}

install_policy_once eshop-observability-15d \
  /bootstrap/ism/observability-15d.json
install_policy_once eshop-metrics-30d \
  /bootstrap/ism/metrics-30d.json
install_policy_once eshop-audit-60d \
  /bootstrap/ism/audit-60d.json
request_file PUT "_template/eshop-single-node-observability" \
  /bootstrap/templates/single-node-observability.json
request_file PUT "_template/eshop-app-logs" \
  /bootstrap/templates/app-logs-template.json
request_file PUT "_template/eshop-app-metrics" \
  /bootstrap/templates/app-metrics-template.json
request_file PUT "_template/eshop-audit" \
  /bootstrap/templates/audit-template.json
request_file PUT "_template/otel-v1-apm-span-index-standard-template" \
  /bootstrap/templates/otel-v1-apm-span-index-standard-template.json
request_file PUT "_ingest/pipeline/eshop-audit-event-id" \
  /bootstrap/ingest/eshop-audit-event-id.json

ensure_index_once() {
  index_name="$1"
  index_file="$2"
  if curl --fail --silent --insecure --user "${admin_auth}" \
    --head "${opensearch_url}/${index_name}" >/dev/null 2>&1; then
    echo "Preserving existing OpenSearch index ${index_name}."
    return
  fi
  echo "Creating empty OpenSearch compatibility index ${index_name}."
  request_file PUT "${index_name}" "${index_file}"
}

# OpenSearch Dashboards 2.19 requires both legacy Trace Analytics index
# patterns before it enables the native Traces and Spans pages. Collector does
# not write this proprietary service-map shape; keep the index empty and
# unmanaged until Dashboards no longer requires it.
ensure_index_once otel-v1-apm-service-map \
  /bootstrap/indexes/otel-v1-apm-service-map.json

curl --fail --silent --show-error --insecure --user "${admin_auth}" \
  --request PUT --header "Content-Type: application/json" \
  --data '{"persistent":{"cluster.default_number_of_replicas":0,"cluster.routing.allocation.disk.threshold_enabled":true,"cluster.routing.allocation.disk.watermark.low":"70%","cluster.routing.allocation.disk.watermark.high":"80%","cluster.routing.allocation.disk.watermark.flood_stage":"85%"}}' \
  "${opensearch_url}/_cluster/settings" >/dev/null

install_monitor_once() {
  marker="$1"
  monitor_file="$2"
  if curl --fail --silent --insecure --user "${admin_auth}" \
    "${opensearch_url}/eshop-observability-bootstrap/_doc/${marker}" \
    >/dev/null 2>&1; then
    return
  fi

  request_file POST "_plugins/_alerting/monitors" "${monitor_file}"
  curl --fail --silent --show-error --insecure --user "${admin_auth}" \
    --request PUT --header "Content-Type: application/json" \
    --data '{"installed":true}' \
    "${opensearch_url}/eshop-observability-bootstrap/_doc/${marker}?refresh=true" \
    >/dev/null
}

install_monitor_once disk-monitor-v1 \
  /bootstrap/alerting/disk-usage-monitor.json
install_monitor_once trace-error-monitor-v1 \
  /bootstrap/alerting/error-rate-monitor.json

# The cluster default and overlay template handle telemetry indexes. Protected
# plugin system indexes can retain their own replica settings on a single node.
curl --fail --silent --show-error --insecure --user "${admin_auth}" \
  --request PUT --header "Content-Type: application/json" \
  --data '{"index":{"number_of_replicas":0}}' \
  "${opensearch_url}/_all/_settings?expand_wildcards=all&allow_no_indices=true" \
  >/dev/null 2>&1 || true

echo "OpenSearch observability security, retention, and alerting bootstrap complete."
