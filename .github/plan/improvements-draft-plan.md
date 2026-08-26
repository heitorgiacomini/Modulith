 3. why annoying json converter in basket/data
 4. appy rate limit
 5. add item into basket use userID
 7. add a health check endpoint to all services
 6. set logging level to info in all services when dev docker-compose override
 8. Rational Performance Tester

10. integracoes
Gateway de pagamento e antifraude
ERP
Emissão fiscal (NFe)
2. a schema per module and a dedicated database role.
Grant that role privileges only on its schema and set its default search path.
CREATE ROLE orders_role LOGIN PASSWORD 'orders_secret';
CREATE SCHEMA orders AUTHORIZATION orders_role;
GRANT USAGE ON SCHEMA orders TO orders_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA orders TO orders_role;
ALTER ROLE orders_role SET search_path = orders;
"Orders": "Host=localhost;Database=appdb;Username=orders_role;Password=orders_secret",
Transportadoras ou marketplaces