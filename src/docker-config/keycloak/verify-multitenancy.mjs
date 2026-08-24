const keycloakUrl = process.env.KEYCLOAK_URL ?? 'http://keycloak:9090';
const apiUrl = process.env.API_URL ?? 'http://api:8080';
const realm = 'eshoprealm';
const acmeId = '11111111-1111-1111-1111-111111111111';
const contosoId = '22222222-2222-2222-2222-222222222222';

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function decodeJwt(token) {
  const payload = token.split('.')[1];
  assert(payload, 'The response was not a JWT.');
  return JSON.parse(Buffer.from(payload, 'base64url').toString('utf8'));
}

function selectedOrganization(token) {
  const claim = decodeJwt(token).organization;
  assert(claim && typeof claim === 'object' && !Array.isArray(claim), 'Missing organization claim.');
  const entries = Object.entries(claim);
  assert(entries.length === 1, `Expected exactly one organization, received ${entries.length}.`);
  const [alias, organization] = entries[0];
  const roles = organization?.realm_access?.roles ?? [];
  return { alias, id: organization?.id, roles };
}

async function request(path, options = {}, accepted = [200]) {
  const response = await fetch(`${apiUrl}${path}`, options);
  const text = await response.text();
  if (!accepted.includes(response.status)) {
    throw new Error(`${options.method ?? 'GET'} ${path} failed (${response.status}): ${text}`);
  }
  return {
    status: response.status,
    body: text ? JSON.parse(text) : null,
    contentType: response.headers.get('content-type')
  };
}

async function tokenFor(organizationAlias) {
  const body = new URLSearchParams({
    grant_type: 'password',
    client_id: 'myclient',
    username: 'shopper',
    password: 'shopper',
    scope: `openid organization:${organizationAlias}`
  });
  const response = await fetch(`${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`, {
    method: 'POST',
    headers: { 'content-type': 'application/x-www-form-urlencoded' },
    body
  });
  if (!response.ok) {
    throw new Error(`Could not obtain ${organizationAlias} token (${response.status}): ${await response.text()}`);
  }
  return (await response.json()).access_token;
}

async function tokenWithoutOrganization() {
  const body = new URLSearchParams({
    grant_type: 'password',
    client_id: 'myclient',
    username: 'shopper',
    password: 'shopper',
    scope: 'openid'
  });
  const response = await fetch(`${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`, {
    method: 'POST',
    headers: { 'content-type': 'application/x-www-form-urlencoded' },
    body
  });
  assert(response.ok, `Could not obtain tenant-neutral token (${response.status}).`);
  return (await response.json()).access_token;
}

async function rptFor(sourceToken, scope) {
  const body = new URLSearchParams({
    grant_type: 'urn:ietf:params:oauth:grant-type:uma-ticket',
    audience: 'ordering-api',
    permission: `Orders#${scope}`
  });
  const response = await fetch(`${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`, {
    method: 'POST',
    headers: {
      authorization: `Bearer ${sourceToken}`,
      'content-type': 'application/x-www-form-urlencoded'
    },
    body
  });
  if (!response.ok) {
    throw new Error(`Could not obtain ${scope} RPT (${response.status}): ${await response.text()}`);
  }
  return (await response.json()).access_token;
}

async function assertRptDenied(sourceToken, scope) {
  const body = new URLSearchParams({
    grant_type: 'urn:ietf:params:oauth:grant-type:uma-ticket',
    audience: 'ordering-api',
    permission: `Orders#${scope}`
  });
  const response = await fetch(`${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`, {
    method: 'POST',
    headers: {
      authorization: `Bearer ${sourceToken}`,
      'content-type': 'application/x-www-form-urlencoded'
    },
    body
  });
  assert(response.status === 403,
    `Expected ${scope} RPT to be denied with 403, received ${response.status}: ${await response.text()}`);
}

async function graphQl(path, token, query) {
  const result = await request(path, {
    method: 'POST',
    headers: {
      authorization: `Bearer ${token}`,
      'content-type': 'application/json'
    },
    body: JSON.stringify({ query })
  });
  assert(!result.body.errors, `${path} returned GraphQL errors: ${JSON.stringify(result.body.errors)}`);
  assert(result.body.data, `${path} returned no GraphQL data.`);
  return result.body.data;
}

const acmeToken = await tokenFor('acme');
const contosoToken = await tokenFor('contoso');
const acmeOrganization = selectedOrganization(acmeToken);
const contosoOrganization = selectedOrganization(contosoToken);
const acmePayload = decodeJwt(acmeToken);
const contosoPayload = decodeJwt(contosoToken);
const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
assert(guidPattern.test(acmePayload.sub) && acmePayload.sub === contosoPayload.sub,
  'Organization-selected access tokens must retain the same GUID user sub.');
assert(acmePayload.preferred_username === 'shopper' && contosoPayload.preferred_username === 'shopper',
  'Organization-selected access tokens must include preferred_username for Basket compatibility.');
assert(acmeOrganization.id === acmeId, 'Acme token has the wrong organization ID.');
assert(acmeOrganization.roles.includes('customer') && acmeOrganization.roles.includes('admin'),
  'Acme token must carry customer and admin organization roles.');
assert(contosoOrganization.id === contosoId, 'Contoso token has the wrong organization ID.');
assert(contosoOrganization.roles.includes('customer') && !contosoOrganization.roles.includes('admin'),
  'Contoso token must carry customer but not admin organization roles.');

const productQuery = 'query { products(take: 20) { totalCount items { id name } } }';
const acmeCatalog = await graphQl('/graphql/catalog', acmeToken, productQuery);
const contosoCatalog = await graphQl('/graphql/catalog', contosoToken, productQuery);
const acmeProductIds = acmeCatalog.products.items.map(product => product.id.toLowerCase());
const contosoProductIds = contosoCatalog.products.items.map(product => product.id.toLowerCase());
assert(acmeProductIds.length > 0, 'Acme Catalog GraphQL result is empty.');
assert(contosoProductIds.length > 0, 'Contoso Catalog GraphQL result is empty.');
assert(acmeProductIds.every(productId => !contosoProductIds.includes(productId)),
  'Catalog GraphQL returned overlapping product IDs across tenants.');
const acmeProductId = acmeProductIds[0];
const contosoProductId = contosoProductIds[0];

await graphQl('/graphql/basket', acmeToken,
  'query { baskets(take: 10) { totalCount items { id userName } } }');
await graphQl('/graphql/basket', contosoToken,
  'query { baskets(take: 10) { totalCount items { id userName } } }');

const acmeRpt = await rptFor(acmeToken, 'orders:read-own');
const contosoRpt = await rptFor(contosoToken, 'orders:read-own');
const acmeAllToken = await tokenFor('acme');
const contosoAllToken = await tokenFor('contoso');
const acmeAllRpt = await rptFor(acmeAllToken, 'orders:read-all');
const acmeRptOrganization = selectedOrganization(acmeRpt);
const contosoRptOrganization = selectedOrganization(contosoRpt);
assert(acmeRptOrganization.id === acmeId && acmeRptOrganization.roles.includes('admin'),
  'Acme RPT did not retain its selected organization and roles.');
assert(contosoRptOrganization.id === contosoId && !contosoRptOrganization.roles.includes('admin'),
  'Contoso RPT did not retain its selected organization and roles.');
assert(decodeJwt(acmeRpt).sub === decodeJwt(acmeToken).sub &&
  decodeJwt(contosoRpt).sub === decodeJwt(contosoToken).sub &&
  decodeJwt(acmeAllRpt).sub === decodeJwt(acmeAllToken).sub,
  'An Ordering RPT did not retain the source user sub.');
await graphQl('/graphql/ordering', acmeRpt,
  'query { orders(take: 10) { totalCount items { id customerId orderName } } }');
await graphQl('/graphql/ordering', contosoRpt,
  'query { orders(take: 10) { totalCount items { id customerId orderName } } }');
await graphQl('/graphql/ordering', acmeAllRpt,
  'query { orders(take: 10) { totalCount items { id customerId orderName } } }');
await assertRptDenied(contosoAllToken, 'orders:read-all');

const bearer = token => ({ authorization: `Bearer ${token}` });
await request(`/products/${acmeProductId}`, { headers: bearer(acmeToken) });
await request(`/products/${contosoProductId}`, { headers: bearer(contosoToken) });
await request(`/products/${contosoProductId}`, { headers: bearer(acmeToken) }, [404]);
await request(`/products/${acmeProductId}`, { headers: bearer(contosoToken) }, [404]);

await request(`/products/${contosoProductId}`, {
  method: 'DELETE',
  headers: bearer(acmeToken)
}, [404]);
await request(`/products/${contosoProductId}`, {
  method: 'DELETE',
  headers: bearer(contosoToken)
}, [403]);
await request(`/products/${contosoProductId}`, { headers: bearer(contosoToken) });

await request(`/products/${contosoProductId}`, {
  headers: {
    ...bearer(acmeToken),
    'x-tenant-id': contosoId
  }
}, [404]);

const tenantNeutralToken = await tokenWithoutOrganization();
assert(decodeJwt(tenantNeutralToken).organization === undefined,
  'The tenant-neutral token unexpectedly contains an organization claim.');
const missingTenant = await request('/products?pageIndex=0&pageSize=10', {
  headers: {
    ...bearer(tenantNeutralToken),
    'x-tenant-id': acmeId
  }
}, [403]);
assert(missingTenant.contentType?.includes('application/problem+json'),
  'Missing-tenant response was not Problem Details.');

console.log(JSON.stringify({
  organizations: {
    acme: { id: acmeOrganization.id, roles: acmeOrganization.roles },
    contoso: { id: contosoOrganization.id, roles: contosoOrganization.roles }
  },
  catalog: {
    acmeProducts: acmeCatalog.products.totalCount,
    contosoProducts: contosoCatalog.products.totalCount
  },
  verified: [
    'single selected organization and source sub in access tokens and RPTs',
    'Acme receives Ordering own/all scopes and Contoso receives only own scopes',
    'Catalog, Basket, and Ordering GraphQL tenant access',
    'cross-tenant REST reads return 404',
    'cross-tenant and unauthorized deletes are rejected',
    'X-Tenant-Id is ignored',
    'missing organization returns 403 Problem Details'
  ]
}, null, 2));
