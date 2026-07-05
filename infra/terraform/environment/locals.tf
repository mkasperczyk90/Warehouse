# The whole stack runs as ONE Fargate task: all containers share the task's network namespace, so they
# reach each other over localhost (no service discovery / Cloud Map needed). This is the cheap, simple
# shape for a throwaway 1-2h environment. Ports are de-conflicted per container (everything defaults to
# 8080 otherwise). Backend services run with ASPNETCORE_ENVIRONMENT=Development on purpose: that is what
# makes them apply EF migrations, create the Wolverine outbox tables, provision RabbitMQ, seed demo data,
# and expose /health (see Program.cs + ServiceDefaults). Harden before any non-ephemeral use.

locals {
  registry = "${data.aws_caller_identity.current.account_id}.dkr.ecr.${var.aws_region}.amazonaws.com"

  # Fargate task sizing auto-scales with whether CDC's 3 extra Debezium Server JVM containers are
  # actually running, so leaving bi_lake_bucket_name unset doesn't silently keep paying for their
  # headroom (var.task_cpu/task_memory default to null in variables.tf so this applies unless an
  # explicit override is set).
  effective_task_cpu    = coalesce(var.task_cpu, var.bi_lake_bucket_name != "" ? 8192 : 4096)
  effective_task_memory = coalesce(var.task_memory, var.bi_lake_bucket_name != "" ? 16384 : 8192)

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

  # Shared env for the .NET backends.
  dotnet_common_env = [
    { name = "ASPNETCORE_ENVIRONMENT", value = "Development" },
  ]

  # CDC-to-Bronze (docs/BI_Plan.md — CDC section): each source database gets its own topic exchange,
  # declared up front by rabbitmq-init (Debezium's RabbitMQ sink never declares the exchange itself —
  # it only auto-creates a queue for the routing key — so publishing would fail against a missing
  # exchange without this). One Debezium Server instance per database publishes onto its exchange with
  # the table's own topic name as routing key; Warehouse.CdcBridge binds one "#" queue per exchange.
  cdc_exchange_warehouse  = "cdc.warehouse"
  cdc_exchange_logistics  = "cdc.logistics"
  cdc_exchange_masterdata = "cdc.masterdata"
  cdc_exchanges           = [local.cdc_exchange_warehouse, local.cdc_exchange_logistics, local.cdc_exchange_masterdata]
  cdc_exchanges_spaced    = join(" ", local.cdc_exchanges)

  # Shared Debezium Server sink config: all three instances publish to their own exchange (above) on
  # the same RabbitMQ broker, as JSON (Debezium's native change-event envelope — op/before/after/source
  # — is left untouched, see docs/BI_Plan.md, so it plugs directly into Databricks' APPLY CHANGES INTO
  # without a custom transform).
  debezium_rabbitmq_sink_env = [
    { name = "DEBEZIUM_SINK_TYPE", value = "rabbitmq" },
    { name = "DEBEZIUM_SINK_RABBITMQ_CONNECTION_HOST", value = "localhost" },
    { name = "DEBEZIUM_SINK_RABBITMQ_CONNECTION_PORT", value = tostring(local.port_rabbitmq) },
    { name = "DEBEZIUM_SINK_RABBITMQ_CONNECTION_USERNAME", value = "guest" },
    { name = "DEBEZIUM_SINK_RABBITMQ_CONNECTION_PASSWORD", value = "guest" },
    # Fixed per the smoke test's finding: the previous name (ROUTING_KEY_SOURCE) never took effect —
    # delivered messages had an empty routing key. Quarkus/SmallRye's config-to-env-var mapping keeps
    # a camelCase property segment (`routingKey`) as one token; it does not insert an underscore
    # between "routing" and "key" the way a naive dots-to-underscores conversion would. Needs a
    # re-run of the smoke test to confirm this corrected name actually takes effect; harmless either
    # way, since Warehouse.CdcBridge binds its queue with routing key "#" and never inspects it.
    { name = "DEBEZIUM_SINK_RABBITMQ_ROUTINGKEY_SOURCE", value = "topic" },
    { name = "DEBEZIUM_FORMAT_VALUE", value = "json" },
    # Without this, Debezium wraps every message as {"schema": {...}, "payload": {...}} (Kafka Connect
    # JSON converter convention) — Warehouse.CdcBridge (and Databricks Auto Loader downstream) only
    # wants the payload itself: {before, after, source, op, ts_ms}.
    { name = "DEBEZIUM_FORMAT_VALUE_SCHEMAS_ENABLE", value = "false" },
    # CONFIRMED REQUIRED by a local docker-compose smoke test (real Postgres + real Debezium Server +
    # real RabbitMQ, not mocked): without an explicit offset file path, Debezium Server crashes on
    # startup with "java.io.IOException: Is a directory" (it resolves the unset filename to the
    # /debezium/data mount point itself). This is a source-side property in Debezium's own config
    # namespace, but the value only needs to be a valid path inside each (isolated) container's own
    # filesystem, so it's identical across all three instances — kept here rather than duplicated
    # three times below.
    { name = "DEBEZIUM_SOURCE_OFFSET_STORAGE_FILE_FILENAME", value = "/debezium/data/offsets.dat" },
    { name = "DEBEZIUM_SOURCE_OFFSET_FLUSH_INTERVAL_MS", value = "1000" },
  ]

  # --- Extra Bronze containers, gated on bi_lake_bucket_name (docs/BI_Plan.md) --------------
  # CDC-to-Bronze (rabbitmq-init + 3x Debezium Server + cdc-bridge) plus identity-bridge (Keycloak
  # Admin API poller — no CDC possible there, see identity-bridge's own comment below). Only
  # deployed when bi_lake_bucket_name is set (spliced into `containers` below) — 4 extra JVM/worker
  # processes is real Fargate cost an ephemeral environment shouldn't pay for BiExport-only use.
  # NOTE: Debezium image tag and the exact DEBEZIUM_* env var names follow its documented
  # Quarkus/MicroProfile config-to-env convention but have only been smoke-tested for a single
  # table/instance (docs/BI_Plan.md's confidence note) — identity-bridge, by contrast, HAS been
  # smoke-tested against a real Keycloak container with the actual realm import and worked first try.
  bi_lake_extra_containers = [
    {
      # One-shot: declare the 3 CDC topic exchanges Debezium's RabbitMQ sink publishes onto. That
      # sink never declares its own exchange (only auto-creates a queue for the routing key), so
      # without this step the first Debezium publish to a not-yet-existing exchange would fail.
      name      = "rabbitmq-init"
      image     = "rabbitmq:3-management-alpine"
      essential = false
      command = ["sh", "-c",
        "for ex in ${local.cdc_exchanges_spaced}; do curl -sf -u guest:guest -X PUT -H content-type:application/json -d '{\"type\":\"topic\",\"durable\":true}' http://localhost:15672/api/exchanges/%2f/$ex; done; echo rabbitmq-init done"
      ]
      dependsOn        = [{ containerName = "rabbitmq", condition = "HEALTHY" }]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      name      = "debezium-warehouse"
      image     = "quay.io/debezium/server:2.7"
      essential = false
      environment = concat(local.debezium_rabbitmq_sink_env, [
        { name = "DEBEZIUM_SINK_RABBITMQ_EXCHANGE", value = local.cdc_exchange_warehouse },
        { name = "DEBEZIUM_SOURCE_CONNECTOR_CLASS", value = "io.debezium.connector.postgresql.PostgresConnector" },
        { name = "DEBEZIUM_SOURCE_DATABASE_HOSTNAME", value = "localhost" },
        { name = "DEBEZIUM_SOURCE_DATABASE_PORT", value = tostring(local.port_postgres) },
        { name = "DEBEZIUM_SOURCE_DATABASE_USER", value = "postgres" },
        { name = "DEBEZIUM_SOURCE_DATABASE_PASSWORD", value = "postgres" },
        { name = "DEBEZIUM_SOURCE_DATABASE_DBNAME", value = "warehouse" },
        { name = "DEBEZIUM_SOURCE_TOPIC_PREFIX", value = "warehouse" },
        { name = "DEBEZIUM_SOURCE_PLUGIN_NAME", value = "pgoutput" },
        { name = "DEBEZIUM_SOURCE_SLOT_NAME", value = "debezium_warehouse" },
        { name = "DEBEZIUM_SOURCE_PUBLICATION_NAME", value = "debezium_warehouse" },
        # Bronze-CDC table scope (docs/BI_Plan.md): current-state/ledger tables only — NOT the
        # inventory.product_snapshots / location_snapshots replicas, which are already fed by
        # integration events from their owning service and would duplicate that data in Bronze.
        # allocations + handling_units/handling_unit_lines promoted from "phase 2" after a direct
        # ask from the Databricks team (StockAllocated/AllocationReleased/HandlingUnitMoved are real
        # domain events with no event contract or consumer — CDC is their only path to Bronze).
        { name = "DEBEZIUM_SOURCE_TABLE_INCLUDE_LIST", value = join(",", [
          "inventory.stock_items", "inventory.stock_movements", "inventory.stock_reservations",
          "inventory.batches", "inventory.stocktakes", "inventory.stocktake_count_lines",
          "inventory.allocations", "inventory.handling_units", "inventory.handling_unit_lines",
          "topology.warehouses", "topology.rooms", "topology.locations", "topology.docks",
        ]) },
      ])
      # Persists DEBEZIUM_SOURCE_OFFSET_STORAGE_FILE_FILENAME on a task-scoped volume (declared in
      # ecs.tf) instead of the container's own writable layer, so this instance's WAL position
      # survives a lone container restart within the task's lifetime (a fresh container gets a fresh,
      # empty writable layer — without this it would restart from scratch instead of resuming).
      # Does not survive the whole task being replaced/redeployed; that's a separate, bigger gap.
      mountPoints = [
        { sourceVolume = "debezium-warehouse-data", containerPath = "/debezium/data", readOnly = false },
      ]
      dependsOn = [
        { containerName = "db-init", condition = "SUCCESS" },
        { containerName = "rabbitmq-init", condition = "SUCCESS" },
      ]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      name      = "debezium-logistics"
      image     = "quay.io/debezium/server:2.7"
      essential = false
      environment = concat(local.debezium_rabbitmq_sink_env, [
        { name = "DEBEZIUM_SINK_RABBITMQ_EXCHANGE", value = local.cdc_exchange_logistics },
        { name = "DEBEZIUM_SOURCE_CONNECTOR_CLASS", value = "io.debezium.connector.postgresql.PostgresConnector" },
        { name = "DEBEZIUM_SOURCE_DATABASE_HOSTNAME", value = "localhost" },
        { name = "DEBEZIUM_SOURCE_DATABASE_PORT", value = tostring(local.port_postgres) },
        { name = "DEBEZIUM_SOURCE_DATABASE_USER", value = "postgres" },
        { name = "DEBEZIUM_SOURCE_DATABASE_PASSWORD", value = "postgres" },
        { name = "DEBEZIUM_SOURCE_DATABASE_DBNAME", value = "logistics" },
        { name = "DEBEZIUM_SOURCE_TOPIC_PREFIX", value = "logistics" },
        { name = "DEBEZIUM_SOURCE_PLUGIN_NAME", value = "pgoutput" },
        { name = "DEBEZIUM_SOURCE_SLOT_NAME", value = "debezium_logistics" },
        { name = "DEBEZIUM_SOURCE_PUBLICATION_NAME", value = "debezium_logistics" },
        # Bronze-CDC table scope — NOT logistics.catalog_product_snapshots (a replica, see above).
        { name = "DEBEZIUM_SOURCE_TABLE_INCLUDE_LIST", value = join(",", [
          "logistics.inbound_deliveries", "logistics.inbound_delivery_lines",
          "logistics.outbound_orders", "logistics.outbound_order_lines",
          "logistics.pick_lists", "logistics.pick_tasks",
          "logistics.shipments", "logistics.packages",
        ]) },
      ])
      # See debezium-warehouse's identical mountPoints comment above.
      mountPoints = [
        { sourceVolume = "debezium-logistics-data", containerPath = "/debezium/data", readOnly = false },
      ]
      dependsOn = [
        { containerName = "db-init", condition = "SUCCESS" },
        { containerName = "rabbitmq-init", condition = "SUCCESS" },
      ]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      name      = "debezium-masterdata"
      image     = "quay.io/debezium/server:2.7"
      essential = false
      environment = concat(local.debezium_rabbitmq_sink_env, [
        { name = "DEBEZIUM_SINK_RABBITMQ_EXCHANGE", value = local.cdc_exchange_masterdata },
        { name = "DEBEZIUM_SOURCE_CONNECTOR_CLASS", value = "io.debezium.connector.postgresql.PostgresConnector" },
        { name = "DEBEZIUM_SOURCE_DATABASE_HOSTNAME", value = "localhost" },
        { name = "DEBEZIUM_SOURCE_DATABASE_PORT", value = tostring(local.port_postgres) },
        { name = "DEBEZIUM_SOURCE_DATABASE_USER", value = "postgres" },
        { name = "DEBEZIUM_SOURCE_DATABASE_PASSWORD", value = "postgres" },
        { name = "DEBEZIUM_SOURCE_DATABASE_DBNAME", value = "masterdata" },
        { name = "DEBEZIUM_SOURCE_TOPIC_PREFIX", value = "masterdata" },
        { name = "DEBEZIUM_SOURCE_PLUGIN_NAME", value = "pgoutput" },
        { name = "DEBEZIUM_SOURCE_SLOT_NAME", value = "debezium_masterdata" },
        { name = "DEBEZIUM_SOURCE_PUBLICATION_NAME", value = "debezium_masterdata" },
        # Bronze-CDC table scope: Partners has no integration events at all today, so this is the
        # ONLY path these tables have into Bronze (docs/BI_Plan.md). product_types promoted from
        # "phase 2" after a direct ask from the Databricks team — ProductStorageChanged (cold-chain/
        # hazard fields) is a real domain event with no event contract or consumer, so CDC is its
        # only path too. product_unit_conversions stays excluded — genuinely low-value static lookup.
        { name = "DEBEZIUM_SOURCE_TABLE_INCLUDE_LIST", value = join(",", [
          "partners.parties", "partners.party_roles", "partners.customer_shipping_addresses",
          "catalog.product_types",
        ]) },
      ])
      # See debezium-warehouse's identical mountPoints comment above.
      mountPoints = [
        { sourceVolume = "debezium-masterdata-data", containerPath = "/debezium/data", readOnly = false },
      ]
      dependsOn = [
        { containerName = "db-init", condition = "SUCCESS" },
        { containerName = "rabbitmq-init", condition = "SUCCESS" },
      ]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      # Warehouse.CdcBridge: raw AMQP consumer of Debezium's CDC JSON, lands it under bronze-cdc/
      # in the same BI lake bucket BiExport writes bronze/ to (docs/BI_Plan.md — CDC section).
      name      = "cdc-bridge"
      image     = "${local.registry}/warehouse-cdc-bridge:${var.image_tag}"
      essential = false
      environment = [
        { name = "CdcBridge__BucketName", value = var.bi_lake_bucket_name },
        { name = "CdcBridge__Region", value = var.aws_region },
        { name = "CdcBridge__RabbitMqHost", value = "localhost" },
        { name = "CdcBridge__RabbitMqPort", value = tostring(local.port_rabbitmq) },
        { name = "CdcBridge__RabbitMqUsername", value = "guest" },
        { name = "CdcBridge__RabbitMqPassword", value = "guest" },
        { name = "CdcBridge__Exchanges__0", value = local.cdc_exchange_warehouse },
        { name = "CdcBridge__Exchanges__1", value = local.cdc_exchange_logistics },
        { name = "CdcBridge__Exchanges__2", value = local.cdc_exchange_masterdata },
      ]
      dependsOn = [
        { containerName = "rabbitmq-init", condition = "SUCCESS" },
      ]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
    {
      # Warehouse.IdentityBridge: polls Keycloak's Admin REST API (no CDC possible — Keycloak has no
      # persistent DB in this stack, dev-mode storage only) and lands a BronzeUser snapshot per user
      # under bronze/identity-service/UserSnapshot/ — same bucket/prefix shape as every other
      # event-driven Bronze writer (docs/BI_Plan.md §5.4). Verified against a real Keycloak container
      # with the actual warehouse-realm.json imported — see docs/BI_Plan.md's Identity source note.
      name      = "identity-bridge"
      image     = "${local.registry}/warehouse-identity-bridge:${var.image_tag}"
      essential = false
      environment = [
        { name = "IdentityBridge__BucketName", value = var.bi_lake_bucket_name },
        { name = "IdentityBridge__Region", value = var.aws_region },
        { name = "IdentityBridge__KeycloakBaseUrl", value = "http://localhost:${local.port_keycloak}" },
        { name = "IdentityBridge__Realm", value = "warehouse" },
        # Dev bootstrap admin (matches KC_BOOTSTRAP_ADMIN_USERNAME/PASSWORD on the keycloak
        # container below) — this ephemeral environment has no dedicated service-account client.
        { name = "IdentityBridge__AdminUsername", value = "admin" },
        { name = "IdentityBridge__AdminPassword", value = "admin" },
      ]
      dependsOn = [
        { containerName = "keycloak", condition = "START" },
      ]
      logConfiguration = { logDriver = "awslogs", options = local.log_options }
    },
  ]

  containers = concat([
    # --- infrastructure ---------------------------------------------------------
    {
      name      = "postgres"
      image     = "postgres:16-alpine"
      essential = true
      # wal_level=logical + replication slots/senders: required for Debezium's CDC-to-Bronze pipeline
      # (docs/BI_Plan.md — CDC section) to read the WAL via logical replication. postgres superuser
      # already has REPLICATION privilege; a dedicated least-privilege replication role is a hardening
      # item for any non-ephemeral use (see the file header note). Harmless to always set, even when
      # CDC is unused.
      command = [
        "postgres",
        "-c", "wal_level=logical",
        "-c", "max_replication_slots=10",
        "-c", "max_wal_senders=10",
      ]
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
      # After creating the databases, also ensure one logical-replication PUBLICATION per database for
      # Debezium's CDC-to-Bronze pipeline (harmless when CDC is unused — an unread publication costs
      # nothing). FOR ALL TABLES (not an explicit table list) is deliberate: it doesn't need the app
      # schemas to exist yet — those land later via each service's own EF migrations at startup — and
      # it means adding a table to Bronze-CDC scope later is just a change to the relevant Debezium
      # connector's table.include.list, no new migration/DDL step here. The actual Bronze-CDC table
      # scoping happens entirely in Debezium (docs/BI_Plan.md — CDC section).
      command = ["sh", "-c",
        "for db in masterdata warehouse logistics; do psql -h localhost -U postgres -tc \"SELECT 1 FROM pg_database WHERE datname='$db'\" | grep -q 1 || psql -h localhost -U postgres -c \"CREATE DATABASE $db\"; done; for db in masterdata warehouse logistics; do psql -h localhost -U postgres -d \"$db\" -tc \"SELECT 1 FROM pg_publication WHERE pubname='debezium_$db'\" | grep -q 1 || psql -h localhost -U postgres -d \"$db\" -c \"CREATE PUBLICATION debezium_$db FOR ALL TABLES\"; done; echo db-init done"
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
        # Bronze export to the BI lake (docs/BI_Plan.md §11) — empty bucket name means the app
        # disables the export consumer. Credentials come from the task role (aws_iam_role.task),
        # not from these vars — the AWS SDK default credential chain picks it up automatically.
        { name = "BiExport__BucketName", value = var.bi_lake_bucket_name },
        { name = "BiExport__Region", value = var.aws_region },
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
        # Bronze export to the BI lake (docs/BI_Plan.md §11) — empty bucket name means the app
        # disables the export consumer. Credentials come from the task role (aws_iam_role.task),
        # not from these vars — the AWS SDK default credential chain picks it up automatically.
        { name = "BiExport__BucketName", value = var.bi_lake_bucket_name },
        { name = "BiExport__Region", value = var.aws_region },
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
        # Bronze export to the BI lake (docs/BI_Plan.md §11) — empty bucket name means the app
        # disables the export consumer. Credentials come from the task role (aws_iam_role.task),
        # not from these vars — the AWS SDK default credential chain picks it up automatically.
        { name = "BiExport__BucketName", value = var.bi_lake_bucket_name },
        { name = "BiExport__Region", value = var.aws_region },
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
    ], var.bi_lake_bucket_name != "" ? local.bi_lake_extra_containers : [])
}
