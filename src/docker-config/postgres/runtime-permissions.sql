-- Run after every migration using the schema owner, not any application role.
DO $$
DECLARE
  module_name text;
  runtime_role text;
BEGIN
  FOREACH module_name IN ARRAY ARRAY['catalog', 'basket', 'ordering', 'accounts'] LOOP
    runtime_role := module_name || '_app';
    EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I', module_name, runtime_role);
    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO %I', module_name, runtime_role);
    EXECUTE format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %I TO %I', module_name, runtime_role);
    EXECUTE format('REVOKE UPDATE, DELETE, TRUNCATE ON %I."AuditRecords" FROM %I', module_name, runtime_role);
    EXECUTE format('REVOKE DELETE, TRUNCATE ON %I."AuditDeliveries" FROM %I', module_name, runtime_role);
  END LOOP;
  REVOKE UPDATE, DELETE, TRUNCATE ON ordering."CheckoutReceipts" FROM ordering_app;
END $$;
