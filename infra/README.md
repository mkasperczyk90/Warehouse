# AWS deployment (on-demand, ephemeral)

Spin the whole Warehouse stack up on AWS **on demand** (~1–2 h), then tear it down to ~$0. Everything
runs as a **single ECS Fargate task** with all containers sharing `localhost` — the cheapest, fastest
shape for a throwaway environment. No RDS, no Amazon MQ, no NAT Gateway, no Kubernetes control plane.

```
ALB ──:80──▶ admin SPA
    ──:8080─▶ gateway (YARP API)
    ──:8081─▶ terminal SPA
                 │  (single Fargate task, awsvpc — containers talk over localhost)
                 ├─ admin · terminal           (nginx, MSW-mocked)
                 ├─ gateway                     (.NET, validates Keycloak JWTs)
                 ├─ masterdata/warehousing/logistics-api  (.NET, ASPNETCORE_ENVIRONMENT=Development:
                 │                               auto-migrate + seed + outbox + RabbitMQ provisioning)
                 ├─ keycloak                    (realm + badge SPI baked in)
                 ├─ postgres + db-init          (one DB per service)
                 └─ rabbitmq
```

## Layout

| Path | What |
|------|------|
| `terraform/bootstrap/` | Run **once**. S3 state bucket, GitHub OIDC + deploy IAM role, 7 ECR repos. |
| `terraform/environment/` | The ephemeral stack (VPC, ALB, ECS task/service). `apply` on up, `destroy` on down. |
| `docker/keycloak/Dockerfile` | Keycloak image with the realm + Maven-built badge SPI baked in. |
| `../.github/workflows/deploy-aws.yml` | Manual (`workflow_dispatch`) **up / down**. |

## One-time setup

1. **Bootstrap** (local, with admin-ish AWS creds):
   ```bash
   cd infra/terraform/bootstrap
   terraform init
   terraform apply           # optionally -var="aws_region=eu-central-1"
   terraform output
   ```
   Keep this stack's local `terraform.tfstate`.

2. **Add the outputs as GitHub repo _variables_** (Settings → Secrets and variables → Actions → *Variables*):
   | Variable | From bootstrap output |
   |----------|-----------------------|
   | `AWS_ROLE_ARN` | `deploy_role_arn` |
   | `TF_STATE_BUCKET` | `state_bucket` |
   | `AWS_REGION` | `aws_region` |

## Daily use

- **Actions → “Deploy to AWS (manual)” → Run workflow → `action = up`.** Builds + pushes images, then
  `terraform apply`. The run summary prints the admin / terminal / gateway URLs. First boot ~3–5 min.
  - Optional: set `allowed_cidr` to your IP (e.g. `1.2.3.4/32`) so the ALB isn’t open to the world.
- **When done → run again with `action = down`.** `terraform destroy` removes the VPC/ALB/ECS → ~$0.

Run it locally instead if you prefer:
```bash
cd infra/terraform/environment
terraform init -backend-config="bucket=<TF_STATE_BUCKET>" -backend-config="region=<AWS_REGION>"
terraform apply -var="image_tag=<tag-you-pushed>"
# ... later ...
terraform destroy -var="image_tag=<tag-you-pushed>"
```

## Cost (≈, eu-central-1, on-demand)

| Item | Rate | Per 2 h session |
|------|------|-----------------|
| Fargate task 4 vCPU / 8 GB | ~$0.197/h | ~$0.39 |
| ALB | ~$0.027/h + minimal LCU | ~$0.06 |
| Logs / data transfer | minimal | a few cents |
| **Total per session** | | **~$0.45–0.55** |
| ECR storage while idle | ~$0.10/GB-month | cents/month |

Drop `task_cpu`/`task_memory` (e.g. 2048 / 8192) to cut compute ~40% at the cost of slower boot.
**No charges accrue while the environment is `down`** apart from a few cents of ECR storage.

## Caveats (it’s a throwaway demo env)

- **Data is ephemeral** — Postgres runs as a Fargate container; everything is lost on `down`. Swap the
  `postgres`/`db-init` containers for RDS if you ever need persistence.
- **Dev settings on purpose** — backends run with `ASPNETCORE_ENVIRONMENT=Development` (auto-migrate,
  seed, health endpoints) and Keycloak uses the dev bootstrap admin. Harden before any non-ephemeral use.
- **SPAs call the real Gateway here** — the deploy builds admin/terminal with MSW off
  (`VITE_USE_MOCKS=false` / `EXPO_PUBLIC_USE_MOCKS=false`) and their nginx reverse-proxies `/api` to the
  gateway in the same task, so requests are same-origin (no CORS). The standalone/local images still
  default to MSW-mocked. The admin Profile screen (`/api/profile`) is served by the gateway from the
  token's claims with an in-memory prefs overlay (lost on restart — fine for a throwaway env).
- The deploy IAM policy in bootstrap is broad-but-scoped for convenience; tighten for production.
