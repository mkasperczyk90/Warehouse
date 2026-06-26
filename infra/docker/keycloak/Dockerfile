# Warehouse Keycloak image for AWS (ECS Fargate).
#
# Locally the AppHost runs a raw Keycloak container and bind-mounts the realm folder + the
# Maven-built badge-authenticator SPI jar (see src/Identity + scripts/run-local.sh). ECS cannot
# bind-mount repo files, so this image BAKES both in: a Maven stage builds the SPI jar, then it is
# copied into the Keycloak providers dir alongside the realm import. Tag MUST match the SPI's
# keycloak.version (pom.xml -> 26.0.7).
#
# Build context is src/Identity (so both keycloak-badge-authenticator/ and realms/ are visible):
#   docker build -t warehouse-keycloak -f infra/docker/keycloak/Dockerfile src/Identity

# --- SPI build stage ------------------------------------------------------------
FROM maven:3.9-eclipse-temurin-17 AS jar
WORKDIR /spi
# Resolve deps first (cached unless the pom changes).
COPY keycloak-badge-authenticator/pom.xml ./
RUN mvn -q -B dependency:go-offline
COPY keycloak-badge-authenticator/src ./src
RUN mvn -q -B clean package

# --- runtime stage --------------------------------------------------------------
FROM quay.io/keycloak/keycloak:26.0.7
COPY --from=jar /spi/target/badge-authenticator.jar /opt/keycloak/providers/badge-authenticator.jar
COPY realms/ /opt/keycloak/data/import/

# Dev-mode bootstrap admin (matches the local AppHost). For a throwaway 1-2h environment this is fine;
# harden (KC_HOSTNAME, production mode, real admin secret) before any non-ephemeral use.
ENV KC_BOOTSTRAP_ADMIN_USERNAME=admin \
    KC_BOOTSTRAP_ADMIN_PASSWORD=admin \
    KC_HTTP_PORT=8080

ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
CMD ["start-dev", "--import-realm"]
