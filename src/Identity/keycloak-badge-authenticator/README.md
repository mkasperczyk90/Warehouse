# Keycloak Badge Authenticator (SPI)

A custom Keycloak **Direct Grant** authenticator that signs a desk user in by **scanned badge number**
(no password). The badge arrives as a form parameter on the token request; the authenticator resolves the
user by their `badge` attribute. This is the missing piece that makes the warehouse's badge-scan login work
with Keycloak as the identity provider (the gateway brokers the token request — see `Warehouse.Gateway`).

## Why a custom SPI

Keycloak's built-in flows assume username + password (or an OIDC redirect). Badge-only sign-in is not a
native credential, so it needs a custom `Authenticator` bound to the realm's direct-grant flow. This is the
canonical, secure way to add it (Keycloak stays the single source of truth for identity and roles).

## Build

> Requires **JDK 17+** and **Maven**. Not built by the .NET solution — it produces a Keycloak provider jar.

```bash
cd src/Identity/keycloak-badge-authenticator
mvn -q clean package
# → target/badge-authenticator.jar
```

The `keycloak.version` in `pom.xml` **must match the running Keycloak image** (see the AppHost
`AddKeycloak` / `WithImageTag`). Bump both together when upgrading Keycloak.

## Deploy

The AppHost bind-mounts `target/badge-authenticator.jar` into the Keycloak container's
`/opt/keycloak/providers/` directory; Keycloak loads providers from there on start (a fresh dev container
runs `kc.sh start-dev`, which builds providers automatically). Build the jar **before** running the AppHost.

## How it is wired

- `BadgeAuthenticator` — reads the `badge` form param (falls back to `username`), looks up the user by the
  `badge` attribute, and authenticates them. No password is checked.
- `BadgeAuthenticatorFactory` — registers it as provider id **`badge-authenticator`**
  (`META-INF/services/org.keycloak.authentication.AuthenticatorFactory`).
- The realm import (`../realms/warehouse-realm.json`) defines a **`badge direct grant`** flow whose single
  REQUIRED execution is `badge-authenticator`, and sets it as the realm's `directGrantFlow`.

## Token request shape (what the gateway broker sends)

```
POST {keycloak}/realms/warehouse/protocol/openid-connect/token
grant_type=password
client_id=warehouse-admin
client_secret=…            # confidential client, kept in the gateway
badge=1001                 # the scanned badge
```

Returns the standard OIDC token response (`access_token`, `refresh_token`, …). The user's `role`, `name`,
`email`, `badge` and default warehouse are carried as claims (mapped from realm roles + user attributes).
