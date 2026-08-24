var Tokens = Java.type('org.keycloak.authorization.util.Tokens');
var Collection = Java.type('java.util.Collection');
var accessToken = Tokens.getAccessToken($evaluation.getAuthorizationProvider().getKeycloakSession());
var organization = accessToken === null ? null : accessToken.getOtherClaims().get('organization');

if (organization !== null && organization.size() === 1) {
  var selectedOrganization = organization.values().iterator().next();
  var realmAccess = selectedOrganization.get('realm_access');
  var roles = realmAccess === null ? null : realmAccess.get('roles');
  if ((roles instanceof Collection && roles.contains('admin')) ||
      (!(roles instanceof Collection) && (' ' + String(roles) + ' ').indexOf(' admin ') >= 0)) {
    $evaluation.grant();
  }
}
