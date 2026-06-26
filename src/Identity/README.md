# Identity & Access (Keycloak)

The deferred identity decision (`docs/02-bounded-contexts.md`) is made: **self-hosted Keycloak** as the
identity provider, with a **custom badge Direct-Grant authenticator** so the warehouse desk keeps its
badge-scan sign-in (a scanned badge number, no password).

## Pieces

| Where | What |
|---|---|
| `keycloak-badge-authenticator/` | Java SPI: a custom Keycloak `Authenticator` that resolves a user by their `badge` attribute (no password). Built with Maven → a provider jar. |
| `realms/warehouse-realm.json` | Realm import: roles (`manager`/`coordinator`/`inspector`), a confidential client (`warehouse-admin`, direct grant), the 3 desk users with a `badge` attribute, and the **badge direct grant** flow wired to the authenticator. |
| `src/AppHost/AppHost.cs` | Runs Keycloak as a container (`quay.io/keycloak/keycloak:26.0.7`), bind-mounting the realm import and the SPI jar, `start-dev --import-realm`. |
| `src/Gateway` | `Auth/AuthBroker` brokers `POST /api/auth/login` → Keycloak token endpoint (badge, confidential client secret server-side); `AddJwtBearer` validates every other call; `AuthClaims` shapes the desk user from the token. |
| `src/web/admin` | The api seam attaches `Authorization: Bearer`; `AuthContext` stores token + user; MSW returns the same shape with a fake token (dev). |

## Flow

```
desk scans badge → FE POST /api/auth/login {badge}
  → gateway broker → Keycloak /realms/warehouse/protocol/openid-connect/token
        grant_type=password, client_id+secret, badge   (custom authenticator resolves the user)
     ← access_token (JWT: sub, name, email, role, badge, default_warehouse, language)
  ← { accessToken, user }
FE stores token → every call carries Bearer → gateway validates → forwards to services
```

## Run locally (the parts CI/this-repo's unit tests can't cover)

**One-liner** (builds the jar in the right order, then starts everything — needs JDK 17+, Maven and Docker):

```powershell
./scripts/run-local.ps1            # PowerShell;  -SkipJar to reuse a built jar, -JarOnly to stop after the jar
```
```bash
scripts/run-local.sh               # bash;  --skip-jar / --jar-only
```

Or do it by hand:

1. **Build the SPI jar** (needs JDK 17+ and Maven):
   ```bash
   cd src/Identity/keycloak-badge-authenticator
   mvn -q clean package          # → target/badge-authenticator.jar
   ```
   Do this **before** starting the AppHost — the jar is bind-mounted into the container, and Docker fails a
   bind mount of a missing file.
2. **Run the AppHost** (needs Docker): `dotnet run --project src/AppHost/Warehouse.AppHost`. Keycloak imports
   the realm + loads the provider on start.
3. **Smoke-test sign-in** against the gateway:
   ```bash
   curl -X POST http://<gateway>/api/auth/login -H 'Content-Type: application/json' -d '{"badge":"1001"}'
   # → { "accessToken": "eyJ…", "user": { "role": "manager", … } }
   ```
   A protected call without the token should be `401`; with `Authorization: Bearer <accessToken>` it should
   pass through.

## Known follow-ups

- **Issuer validation** is off in the gateway dev config (Keycloak's advertised issuer differs from the
  internal service-discovery host). Pin `TokenValidationParameters.ValidIssuer` once a stable public URL
  fronts Keycloak.
- **`keycloak.version`** in the SPI `pom.xml` must match the container image tag in the AppHost — bump both
  together.
- **Authorization policies** (role-based) and **per-service token validation** (zero-trust) are not wired
  yet — today the gateway gates and services trust it.
