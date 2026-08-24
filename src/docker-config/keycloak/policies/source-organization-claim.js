var Tokens = Java.type('org.keycloak.authorization.util.Tokens');
var sourceToken = Tokens.getAccessToken(keycloakSession);
exports = sourceToken === null ? null : sourceToken.getOtherClaims().get('organization');
