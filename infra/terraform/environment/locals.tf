# The whole stack runs as ONE Fargate task: all containers share the task's network namespace, so they
# reach each other over localhost (no service discovery / Cloud Map needed). This is the cheap, simple
# shape for a throwaway 1-2h environment. Ports are de-conflicted per container (everything defaults to
# 8080 otherwise). Backend services run with ASPNETCORE_ENVIRONMENT=Development on purpose: that is what
# makes them apply EF migrations, create the Wolverine outbox tables, provision RabbitMQ, seed demo data,
# and expose /health (see Program.cs + ServiceDefaults). Harden before any non-ephemeral use.

locals {
  registry = "${data.aws_caller_identity.current.account_id}.dkr.ecr.${var.aws_region}.amazonaws.com"

  # localhost ports inside the shared task namespace.
  port_postgres    = 5432
  port_rabbitmq    = 5672
  port_keycloak    = 8080
  port_masterdata  = 5001
  port_warehousing = 5002
  port_logistics   = 5003
  port_gateway     = 8081
  port_admin       = 80
  port_terminal    = 8082

  pg_conn     = "Host=localhost;Port=${local.port_postgres};Username=postgres;Password=postgres;Database="
  rabbit_conn = "amqp://guest:guest@localhost:${local.port_rabbitmq}"

  log_options = {
    "awslogs-group"         = aws_cloudwatch_log_group.this.name
    "awslogs-region"        = var.aws_region
    "awslogs-stream-prefix" = "warehouse"
  }

  # Shared env for the four .NET backends.
  dotnet_common_env = [
    { name = "ASPNETCORE_ENVIRONMENT", value = "Development" },
  ]

  containers = [
    # --- infrastructure ---------------------------------------------------------
    {
      name      = "postgres"
      image     = "postgres:16-alpine"
      essential = true
      environment = [
        { name = "POSTGRES_USER", value = "postgres" },
        { name = "POSTGRES_PASSWORD", value = "postgres" },
      ]
      portMappings = [{ containerPort = local.port_postgres }]
      healthCheck = {
        command     = ["CMD-SHELL", "pg_isready -U postgres || exit 1"]
        interval    = 10
        timeout     = 5
        retries     = 10
        startPeriod = 20
      }
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      # One-shot: create the three per-service databases, then exit. Backends wait for this to SUCCEED.
      name      = "db-init"
      image     = "postgres:16-alpine"
      essential = false
      environment = [
        { name = "PGPASSWORD", value = "postgres" },
      ]
      command = ["sh", "-c",
        "for db in masterdata warehouse logistics; do psql -h localhost -U postgres -tc \"SELECT 1 FROM pg_database WHERE datname='$db'\" | grep -q 1 || psql -h localhost -U postgres -c \"CREATE DATABASE $db\"; done; echo db-init done"
      ]
      dependsOn        = [{ containerName = "postgres", condition = "HEALTHY" }]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      name      = "rabbitmq"
      image     = "rabbitmq:3-management-alpine"
      essential = true
      portMappings = [
        { containerPort = local.port_rabbitmq },
        { containerPort = 15672 },
      ]
      healthCheck = {
        command     = ["CMD-SHELL", "rabbitmq-diagnostics -q ping || exit 1"]
        interval    = 15
        timeout     = 10
        retries     = 10
        startPeriod = 40
      }
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      name             = "keycloak"
      image            = "${local.registry}/warehouse-keycloak:${var.image_tag}"
      essential        = true
      environment      = [{ name = "KC_HTTP_PORT", value = tostring(local.port_keycloak) }]
      portMappings     = [{ containerPort = local.port_keycloak }]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },

    # --- backend services -------------------------------------------------------
    {
      name      = "masterdata-api"
      image     = "${local.registry}/warehouse-masterdata-api:${var.image_tag}"
      essential = true
      environment = concat(local.dotnet_common_env, [
        { name = "ASPNETCORE_HTTP_PORTS", value = tostring(local.port_masterdata) },
        { name = "ConnectionStrings__masterdata", value = "${local.pg_conn}masterdata" },
        { name = "ConnectionStrings__rabbitmq", value = local.rabbit_conn },
      ])
      portMappings = [{ containerPort = local.port_masterdata }]
      dependsOn = [
        { containerName = "db-init", condition = "SUCCESS" },
        { containerName = "rabbitmq", condition = "HEALTHY" },
      ]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      name      = "warehousing-api"
      image     = "${local.registry}/warehouse-warehousing-api:${var.image_tag}"
      essential = true
      environment = concat(local.dotnet_common_env, [
        { name = "ASPNETCORE_HTTP_PORTS", value = tostring(local.port_warehousing) },
        { name = "ConnectionStrings__warehouse", value = "${local.pg_conn}warehouse" },
        { name = "ConnectionStrings__rabbitmq", value = local.rabbit_conn },
      ])
      portMappings = [{ containerPort = local.port_warehousing }]
      dependsOn = [
        { containerName = "db-init", condition = "SUCCESS" },
        { containerName = "rabbitmq", condition = "HEALTHY" },
      ]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      name      = "logistics-api"
      image     = "${local.registry}/warehouse-logistics-api:${var.image_tag}"
      essential = true
      environment = concat(local.dotnet_common_env, [
        { name = "ASPNETCORE_HTTP_PORTS", value = tostring(local.port_logistics) },
        { name = "ConnectionStrings__logistics", value = "${local.pg_conn}logistics" },
        { name = "ConnectionStrings__rabbitmq", value = local.rabbit_conn },
      ])
      portMappings = [{ containerPort = local.port_logistics }]
      dependsOn = [
        { containerName = "db-init", condition = "SUCCESS" },
        { containerName = "rabbitmq", condition = "HEALTHY" },
      ]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },

    # --- gateway ----------------------------------------------------------------
    {
      name      = "gateway"
      image     = "${local.registry}/warehouse-gateway:${var.image_tag}"
      essential = true
      environment = concat(local.dotnet_common_env, [
        { name = "ASPNETCORE_HTTP_PORTS", value = tostring(local.port_gateway) },
        # YARP/BFF/auth resolve these logical hosts via service discovery env vars -> localhost ports.
        { name = "services__masterdata-api__http__0", value = "http://localhost:${local.port_masterdata}" },
        { name = "services__warehousing-api__http__0", value = "http://localhost:${local.port_warehousing}" },
        { name = "services__logistics-api__http__0", value = "http://localhost:${local.port_logistics}" },
        { name = "services__keycloak__http__0", value = "http://localhost:${local.port_keycloak}" },
        { name = "Keycloak__Authority", value = "http://localhost:${local.port_keycloak}/realms/warehouse" },
        { name = "Keycloak__Realm", value = "warehouse" },
        { name = "Keycloak__ClientId", value = "warehouse-admin" },
        { name = "Keycloak__ClientSecret", value = "warehouse-admin-secret-dev" },
      ])
      portMappings = [{ containerPort = local.port_gateway }]
      dependsOn = [
        { containerName = "masterdata-api", condition = "START" },
        { containerName = "warehousing-api", condition = "START" },
        { containerName = "logistics-api", condition = "START" },
        { containerName = "keycloak", condition = "START" },
      ]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },

    # --- front-ends (MSW-mocked static SPAs served by nginx) --------------------
    {
      name      = "admin"
      image     = "${local.registry}/warehouse-admin:${var.image_tag}"
      essential = true
      # nginx envsubst: serve on :80 and reverse-proxy /api to the gateway in this same task.
      environment = [
        { name = "LISTEN_PORT", value = tostring(local.port_admin) },
        { name = "GATEWAY_UPSTREAM", value = "http://localhost:${local.port_gateway}" },
      ]
      portMappings     = [{ containerPort = local.port_admin }]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      name      = "terminal"
      image     = "${local.registry}/warehouse-terminal:${var.image_tag}"
      essential = true
      # nginx envsubst: admin already owns :80 in the shared namespace, so the terminal listens on 8082
      # (no baked-in sed needed) and proxies /api to the gateway.
      environment = [
        { name = "LISTEN_PORT", value = tostring(local.port_terminal) },
        { name = "GATEWAY_UPSTREAM", value = "http://localhost:${local.port_gateway}" },
      ]
      portMappings     = [{ containerPort = local.port_terminal }]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
  ]
}
