const required = ['KEYCLOAK_URL', 'KEYCLOAK_ADMIN', 'KEYCLOAK_ADMIN_PASSWORD', 'APP_PUBLIC_URL', 'ORDERING_CLIENT_SECRET'];
for (const name of required) {
  if (!process.env[name]) throw new Error(`${name} is required`);
}
const origin = new URL(process.env.APP_PUBLIC_URL).origin;
if (!origin.startsWith('https://')) throw new Error('APP_PUBLIC_URL must use HTTPS');
process.env.PRODUCTION_SETUP = 'true';
// Reuse organization claim and permission configuration without creating demo users or tenants.
await import('./configure-organizations.mjs');

const tokenResponse = await fetch(`${process.env.KEYCLOAK_URL}/realms/master/protocol/openid-connect/token`, {
  method: 'POST',
  body: new URLSearchParams({ grant_type: 'password', client_id: 'admin-cli', username: process.env.KEYCLOAK_ADMIN, password: process.env.KEYCLOAK_ADMIN_PASSWORD })
});
if (!tokenResponse.ok) throw new Error('Keycloak administrator authentication failed');
const { access_token: token } = await tokenResponse.json();
const base = `${process.env.KEYCLOAK_URL}/admin/realms/eshoprealm`;
const headers = { authorization: `Bearer ${token}`, 'content-type': 'application/json' };
const response = await fetch(`${base}/clients?max=200`, { headers });
if (!response.ok) throw new Error('Could not read clients');
const clients = await response.json();
for (const clientId of ['myclient', 'ordering-api']) {
  const client = clients.find(value => value.clientId === clientId);
  if (!client) throw new Error(`Missing ${clientId}`);
  const detailResponse = await fetch(`${base}/clients/${client.id}`, { headers });
  if (!detailResponse.ok) throw new Error(`Could not read ${clientId}`);
  const details = await detailResponse.json();
  if (clientId === 'myclient') {
    details.redirectUris = [`${origin}/*`];
    details.webOrigins = [origin];
    details.attributes = { ...details.attributes, 'post.logout.redirect.uris': `${origin}/*` };
  } else {
    details.secret = process.env.ORDERING_CLIENT_SECRET;
  }
  const update = await fetch(`${base}/clients/${client.id}`, { method: 'PUT', headers, body: JSON.stringify(details) });
  if (!update.ok) throw new Error(`Could not configure ${clientId}`);
}
console.log('Production clients and organization permissions are configured. No demo identities were created.');
