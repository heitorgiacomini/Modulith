const keycloakUrl = process.env.KEYCLOAK_URL ?? 'http://keycloak:9090';
const adminUser = process.env.KEYCLOAK_ADMIN ?? 'admin';
const adminPassword = process.env.KEYCLOAK_ADMIN_PASSWORD ?? 'admin';
const realm = 'eshoprealm';

const tenants = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    name: 'Acme',
    alias: 'acme',
    groups: { Customers: { role: 'customer', shopper: true }, Admins: { role: 'admin', shopper: true } }
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    name: 'Contoso',
    alias: 'contoso',
    groups: { Customers: { role: 'customer', shopper: true }, Admins: { role: 'admin', shopper: false } }
  }
];

let accessToken;

async function waitForKeycloak() {
  for (let attempt = 1; attempt <= 60; attempt++) {
    try {
      const body = new URLSearchParams({
        grant_type: 'password',
        client_id: 'admin-cli',
        username: adminUser,
        password: adminPassword
      });
      const response = await fetch(`${keycloakUrl}/realms/master/protocol/openid-connect/token`, {
        method: 'POST',
        headers: { 'content-type': 'application/x-www-form-urlencoded' },
        body
      });
      if (response.ok) {
        accessToken = (await response.json()).access_token;
        return;
      }
    } catch {
      // Keycloak is still starting or upgrading its database.
    }

    await new Promise(resolve => setTimeout(resolve, 2000));
  }

  throw new Error('Keycloak did not become ready in time.');
}

async function admin(path, options = {}) {
  const response = await fetch(`${keycloakUrl}/admin/realms/${realm}${path}`, {
    ...options,
    headers: {
      authorization: `Bearer ${accessToken}`,
      ...(options.body === undefined ? {} : { 'content-type': 'application/json' }),
      ...options.headers
    },
    body: options.body === undefined || typeof options.body === 'string'
      ? options.body
      : JSON.stringify(options.body)
  });
  const accepted = options.accepted ?? [200, 201, 204];
  if (!accepted.includes(response.status)) {
    throw new Error(`${options.method ?? 'GET'} ${path} failed (${response.status}): ${await response.text()}`);
  }

  if (response.status === 204 || response.headers.get('content-length') === '0') {
    return null;
  }

  const text = await response.text();
  return text ? JSON.parse(text) : null;
}

async function configureOrganizationScope() {
  let clientScopes = await admin('/client-scopes');
  let organizationScope = clientScopes.find(scope => scope.name === 'organization');
  if (!organizationScope) {
    await admin('/client-scopes', {
      method: 'POST',
      body: {
        name: 'organization',
        description: 'Organization membership, groups, and organization-specific role mappings',
        protocol: 'openid-connect',
        attributes: {
          'include.in.token.scope': 'true',
          'consent.screen.text': '${organizationScopeConsentText}',
          'display.on.consent.screen': 'true'
        }
      }
    });
    clientScopes = await admin('/client-scopes');
    organizationScope = clientScopes.find(scope => scope.name === 'organization');
  }
  if (!organizationScope) {
    throw new Error('Could not create the organization client scope.');
  }

  const mapperPath = `/client-scopes/${organizationScope.id}/protocol-mappers/models`;
  const mappers = await admin(mapperPath);
  const membershipMapper = mappers.find(mapper =>
    mapper.protocolMapper === 'oidc-organization-membership-mapper');
  const membershipMapperRepresentation = {
    ...(membershipMapper ?? {}),
    name: membershipMapper?.name ?? 'organization',
    protocol: 'openid-connect',
    protocolMapper: 'oidc-organization-membership-mapper',
    consentRequired: false,
    config: {
    ...membershipMapper?.config,
    'id.token.claim': 'true',
    'access.token.claim': 'true',
    'userinfo.token.claim': 'true',
    'introspection.token.claim': 'true',
    'claim.name': 'organization',
    'jsonType.label': 'String',
    multivalued: 'true',
    addOrganizationId: 'true',
    addOrganizationAttributes: 'false'
    }
  };
  await admin(membershipMapper ? `${mapperPath}/${membershipMapper.id}` : mapperPath, {
    method: membershipMapper ? 'PUT' : 'POST',
    body: membershipMapperRepresentation
  });

  const groupMapper = mappers.find(mapper =>
    mapper.protocolMapper === 'oidc-organization-group-membership-mapper');
  const groupMapperRepresentation = {
    ...(groupMapper ?? {}),
    name: groupMapper?.name ?? 'organization groups',
    protocol: 'openid-connect',
    protocolMapper: 'oidc-organization-group-membership-mapper',
    consentRequired: false,
    config: {
      ...groupMapper?.config,
      'id.token.claim': 'true',
      'access.token.claim': 'true',
      'userinfo.token.claim': 'true',
      'introspection.token.claim': 'true',
      'lightweight.claim': 'false',
      addGroupRoleMappings: 'true'
    }
  };
  await admin(groupMapper ? `${mapperPath}/${groupMapper.id}` : mapperPath, {
    method: groupMapper ? 'PUT' : 'POST',
    body: groupMapperRepresentation
  });

  const clients = await admin('/clients?max=200');
  for (const clientId of ['myclient', 'ordering-api']) {
    const client = clients.find(value => value.clientId === clientId);
    if (!client) {
      throw new Error(`Required client ${clientId} was not found.`);
    }

    const optionalScopes = await admin(`/clients/${client.id}/optional-client-scopes`);
    if (!optionalScopes.some(scope => scope.id === organizationScope.id)) {
      await admin(`/clients/${client.id}/optional-client-scopes/${organizationScope.id}`, { method: 'PUT' });
    }

    if (clientId === 'myclient') {
      const clientMapperPath = `/clients/${client.id}/protocol-mappers/models`;
      const clientMappers = await admin(clientMapperPath);
      const subjectMapper = clientMappers.find(mapper => mapper.protocolMapper === 'oidc-sub-mapper');
      await admin(subjectMapper ? `${clientMapperPath}/${subjectMapper.id}` : clientMapperPath, {
        method: subjectMapper ? 'PUT' : 'POST',
        body: {
          ...(subjectMapper ?? {}),
          name: subjectMapper?.name ?? 'Subject (sub)',
          protocol: 'openid-connect',
          protocolMapper: 'oidc-sub-mapper',
          consentRequired: false,
          config: {
            ...subjectMapper?.config,
            'access.token.claim': 'true',
            'lightweight.claim': 'true',
            'introspection.token.claim': 'true'
          }
        }
      });

      const userNameMapper = clientMappers.find(mapper =>
        mapper.protocolMapper === 'oidc-usermodel-property-mapper' &&
        mapper.config?.['claim.name'] === 'preferred_username');
      await admin(userNameMapper ? `${clientMapperPath}/${userNameMapper.id}` : clientMapperPath, {
        method: userNameMapper ? 'PUT' : 'POST',
        body: {
          ...(userNameMapper ?? {}),
          name: userNameMapper?.name ?? 'Preferred username',
          protocol: 'openid-connect',
          protocolMapper: 'oidc-usermodel-property-mapper',
          consentRequired: false,
          config: {
            ...userNameMapper?.config,
            'user.attribute': 'username',
            'claim.name': 'preferred_username',
            'jsonType.label': 'String',
            'id.token.claim': 'true',
            'access.token.claim': 'true',
            'lightweight.claim': 'true',
            'userinfo.token.claim': 'true',
            'introspection.token.claim': 'true'
          }
        }
      });
    }

    if (clientId === 'ordering-api') {
      const defaultScopes = await admin(`/clients/${client.id}/default-client-scopes`);
      if (defaultScopes.some(scope => scope.id === organizationScope.id)) {
        await admin(`/clients/${client.id}/default-client-scopes/${organizationScope.id}`, { method: 'DELETE' });
      }

      const clientMapperPath = `/clients/${client.id}/protocol-mappers/models`;
      const clientMappers = await admin(clientMapperPath);
      const sourceOrganizationMapper = clientMappers.find(mapper =>
        mapper.protocolMapper === 'script-source-organization-claim.js');
      const sourceOrganizationMapperRepresentation = {
        ...(sourceOrganizationMapper ?? {}),
        name: sourceOrganizationMapper?.name ?? 'Source Organization Claim',
        protocol: 'openid-connect',
        protocolMapper: 'script-source-organization-claim.js',
        consentRequired: false,
        config: {
          ...sourceOrganizationMapper?.config,
          'claim.name': 'organization',
          'jsonType.label': 'JSON',
          'access.token.claim': 'true',
          'id.token.claim': 'false',
          'userinfo.token.claim': 'false',
          'introspection.token.claim': 'true'
        }
      };
      await admin(
        sourceOrganizationMapper ? `${clientMapperPath}/${sourceOrganizationMapper.id}` : clientMapperPath,
        {
          method: sourceOrganizationMapper ? 'PUT' : 'POST',
          body: sourceOrganizationMapperRepresentation
        });

      const subjectMapper = clientMappers.find(mapper => mapper.protocolMapper === 'oidc-sub-mapper');
      const subjectMapperRepresentation = {
        ...(subjectMapper ?? {}),
        name: subjectMapper?.name ?? 'Subject (sub)',
        protocol: 'openid-connect',
        protocolMapper: 'oidc-sub-mapper',
        consentRequired: false,
        config: {
          ...subjectMapper?.config,
          'access.token.claim': 'true',
          'lightweight.claim': 'true',
          'introspection.token.claim': 'true'
        }
      };
      await admin(subjectMapper ? `${clientMapperPath}/${subjectMapper.id}` : clientMapperPath, {
        method: subjectMapper ? 'PUT' : 'POST',
        body: subjectMapperRepresentation
      });
    }
  }
}

async function configureOrderingAuthorization() {
  const clients = await admin('/clients?clientId=ordering-api');
  if (clients.length !== 1) {
    throw new Error('Expected exactly one ordering-api client.');
  }

  const resourceServerPath = `/clients/${clients[0].id}/authz/resource-server`;
  const policyPath = `${resourceServerPath}/policy`;
  async function ensureScriptPolicy(name, type, description) {
    let policy = await admin(
      `${policyPath}/search?name=${encodeURIComponent(name)}`,
      { accepted: [200, 204] });
    if (!policy) {
      await admin(`${policyPath}/${type}`, {
        method: 'POST',
        body: { name, description, logic: 'POSITIVE', decisionStrategy: 'UNANIMOUS' }
      });
      policy = await admin(`${policyPath}/search?name=${encodeURIComponent(name)}`);
    }
    if (policy.type !== type) {
      throw new Error(`Policy ${name} exists with unexpected type ${policy.type}.`);
    }

    return policy;
  }

  const customerPolicy = await ensureScriptPolicy(
    'Keycloak Organization Customer',
    'script-selected-organization-customer.js',
    'Requires customer in the single selected Keycloak organization claim.');
  const adminPolicy = await ensureScriptPolicy(
    'Keycloak Organization Admin',
    'script-selected-organization-admin.js',
    'Requires admin in the single selected Keycloak organization claim.');

  for (const [permissionName, organizationPolicy] of [
    ['Customer Own Orders', customerPolicy],
    ['Admin All Orders', adminPolicy]
  ]) {
    const permissionPath = `${resourceServerPath}/permission/scope`;
    const permission = await admin(
      `${permissionPath}/search?name=${encodeURIComponent(permissionName)}`);
    const resources = await admin(`${permissionPath}/${permission.id}/resources`);
    const scopes = await admin(`${permissionPath}/${permission.id}/scopes`);
    await admin(`${permissionPath}/${permission.id}`, {
      method: 'PUT',
      body: {
        id: permission.id,
        name: permission.name,
        type: 'scope',
        logic: 'POSITIVE',
        decisionStrategy: 'UNANIMOUS',
        resources: resources.map(resource => resource._id),
        scopes: scopes.map(scope => scope.id),
        policies: [organizationPolicy.id]
      },
      accepted: [201, 204]
    });
  }

  const obsoletePolicyNames = new Set([
    'Customer',
    'OrderingAdmin',
    'Trusted Angular Client',
    'Selected Organization Scope',
    'Selected Organization Customer',
    'Selected Organization Admin',
    'Acme Organization Customer',
    'Contoso Organization Customer',
    'Acme Organization Admin',
    'Contoso Organization Admin'
  ]);
  const policies = await admin(`${policyPath}?max=100`);
  for (const policy of policies.filter(value => obsoletePolicyNames.has(value.name))) {
    await admin(`${policyPath}/${policy.id}`, { method: 'DELETE' });
  }
}

async function ensureOrganization(definition, shopper, roles) {
  const organizations = await admin('/organizations?max=100');
  const organization = organizations.find(value => value.alias === definition.alias);
  if (!organization) {
    throw new Error(
      `Required development organization ${definition.alias} is missing. Reset the Keycloak schema and import eshoprealm-realm.json.`);
  }
  if (organization.id !== definition.id) {
    throw new Error(
      `Organization ${definition.alias} has ID ${organization.id}; expected ${definition.id}. Reset the Keycloak schema and re-import the realm.`);
  }
  const organizationId = organization.id;

  await admin(`/organizations/${organizationId}`, {
    method: 'PUT',
    body: { ...organization, name: definition.name, alias: definition.alias, enabled: true }
  });
  await admin(`/organizations/${organizationId}/members`, {
    method: 'POST',
    body: JSON.stringify(shopper.id),
    accepted: [201, 409]
  });

  for (const [groupName, groupDefinition] of Object.entries(definition.groups)) {
    let groups = await admin(`/organizations/${organizationId}/groups?max=100`);
    let group = groups.find(value => value.name === groupName);
    if (!group) {
      await admin(`/organizations/${organizationId}/groups`, {
        method: 'POST',
        body: { name: groupName }
      });
      groups = await admin(`/organizations/${organizationId}/groups?max=100`);
      group = groups.find(value => value.name === groupName);
    }
    if (!group) {
      throw new Error(`Could not create ${definition.alias}/${groupName}.`);
    }

    const desiredRole = roles[groupDefinition.role];
    const mappedRoles = await admin(
      `/organizations/${organizationId}/groups/${group.id}/role-mappings/realm`);
    for (const roleName of ['customer', 'admin']) {
      const mapped = mappedRoles.find(value => value.name === roleName);
      if (mapped && roleName !== desiredRole.name) {
        await admin(`/organizations/${organizationId}/groups/${group.id}/role-mappings/realm`, {
          method: 'DELETE',
          body: [mapped]
        });
      }
    }
    if (!mappedRoles.some(value => value.name === desiredRole.name)) {
      await admin(`/organizations/${organizationId}/groups/${group.id}/role-mappings/realm`, {
        method: 'POST',
        body: [desiredRole]
      });
    }

    const members = await admin(`/organizations/${organizationId}/groups/${group.id}/members?max=100`);
    const shopperIsMember = members.some(member => member.id === shopper.id);
    if (groupDefinition.shopper && !shopperIsMember) {
      await admin(`/organizations/${organizationId}/groups/${group.id}/members/${shopper.id}`, { method: 'PUT' });
    } else if (!groupDefinition.shopper && shopperIsMember) {
      await admin(`/organizations/${organizationId}/groups/${group.id}/members/${shopper.id}`, { method: 'DELETE' });
    }
  }
}

await waitForKeycloak();
if (process.env.CLEAR_REALM_CACHE === 'true') {
  await admin('/clear-realm-cache', { method: 'POST' });
  await admin('/clear-user-cache', { method: 'POST' });
  await admin('/clear-keys-cache', { method: 'POST' });
  await waitForKeycloak();
}
await admin('', { method: 'PUT', body: { organizationsEnabled: true } });
await configureOrganizationScope();
await configureOrderingAuthorization();

const shoppers = await admin('/users?username=shopper&exact=true');
if (shoppers.length !== 1) {
  throw new Error('Expected exactly one seeded shopper user.');
}
const shopper = shoppers[0];
const roles = {
  customer: await admin('/roles/customer'),
  admin: await admin('/roles/admin')
};

const directRoles = await admin(`/users/${shopper.id}/role-mappings/realm`);
const directCustomer = directRoles.find(role => role.name === 'customer');
if (directCustomer) {
  await admin(`/users/${shopper.id}/role-mappings/realm`, { method: 'DELETE', body: [directCustomer] });
}

for (const tenant of tenants) {
  await ensureOrganization(tenant, shopper, roles);
}

console.log('Keycloak organizations, groups, memberships, roles, and token mappers are configured.');
