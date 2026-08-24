FROM eclipse-temurin:21-jdk-alpine AS policy-builder
WORKDIR /policy
COPY docker-config/keycloak/policies/ .
RUN jar --create --file /selected-organization-policies.jar -C /policy .

FROM docker.io/keycloak/keycloak:26.7.0 AS keycloak-builder
COPY --from=policy-builder /selected-organization-policies.jar /opt/keycloak/providers/
RUN /opt/keycloak/bin/kc.sh build --features=scripts

FROM docker.io/keycloak/keycloak:26.7.0
ENV KC_FEATURES=scripts
COPY --from=keycloak-builder /opt/keycloak/ /opt/keycloak/
