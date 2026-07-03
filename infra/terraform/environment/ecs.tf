resource "aws_cloudwatch_log_group" "this" {
  name              = "/ecs/warehouse"
  retention_in_days = var.log_retention_days
}

# Execution role: lets Fargate pull from ECR and write task logs to CloudWatch.
data "aws_iam_policy_document" "ecs_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    effect  = "Allow"
    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "task_execution" {
  name               = "warehouse-task-execution"
  assume_role_policy = data.aws_iam_policy_document.ecs_assume.json
}

resource "aws_iam_role_policy_attachment" "task_execution" {
  role       = aws_iam_role.task_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# Task role: what the CONTAINERS themselves can call, via the AWS SDK default credential chain —
# distinct from the execution role above (which is Fargate's own ECR-pull/logs identity). Every
# container in this task shares one task role (single-task-namespace design, see locals.tf), so
# this is scoped to exactly what app code needs: writing Bronze exports to the BI lake's `bronze/`
# prefix (event-driven, docs/BI_Plan.md §11) and `bronze-cdc/` prefix (Debezium CDC, via
# Warehouse.CdcBridge — docs/BI_Plan.md, CDC section). Empty when `bi_lake_bucket_name` is unset —
# the role still exists (containers get an identity) but with no permissions.
resource "aws_iam_role" "task" {
  name               = "warehouse-task"
  assume_role_policy = data.aws_iam_policy_document.ecs_assume.json
}

data "aws_iam_policy_document" "task" {
  count = var.bi_lake_bucket_name != "" ? 1 : 0
  statement {
    sid    = "BiExportWrite"
    effect = "Allow"
    actions = [
      "s3:PutObject",
      "s3:AbortMultipartUpload",
    ]
    resources = [
      "arn:aws:s3:::${var.bi_lake_bucket_name}/bronze/*",
      "arn:aws:s3:::${var.bi_lake_bucket_name}/bronze-cdc/*",
    ]
  }
}

resource "aws_iam_role_policy" "task" {
  count  = var.bi_lake_bucket_name != "" ? 1 : 0
  name   = "warehouse-task-bi-export"
  role   = aws_iam_role.task.id
  policy = data.aws_iam_policy_document.task[0].json
}

resource "aws_ecs_cluster" "main" {
  name = "warehouse"
}

resource "aws_ecs_task_definition" "main" {
  family                   = "warehouse"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = local.effective_task_cpu
  memory                   = local.effective_task_memory
  execution_role_arn       = aws_iam_role.task_execution.arn
  task_role_arn            = aws_iam_role.task.arn
  container_definitions    = jsonencode(local.containers)

  # Task-scoped (Fargate ephemeral storage) volumes, one per Debezium instance, so a lone container
  # restart doesn't lose its WAL offset (see each debezium-* container's mountPoints in locals.tf).
  # Declared unconditionally: harmless when CDC is off (never mounted by any container in that case,
  # since the 3 debezium-* containers themselves are gated on bi_lake_bucket_name in locals.tf).
  volume {
    name = "debezium-warehouse-data"
  }
  volume {
    name = "debezium-logistics-data"
  }
  volume {
    name = "debezium-masterdata-data"
  }
}

resource "aws_security_group" "ecs_tasks" {
  name        = "warehouse-tasks"
  description = "ALB -> task. Inter-container traffic is localhost and bypasses this SG."
  vpc_id      = aws_vpc.main.id

  ingress {
    description     = "admin from ALB"
    from_port       = local.port_admin
    to_port         = local.port_admin
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }
  ingress {
    description     = "gateway from ALB"
    from_port       = local.port_gateway
    to_port         = local.port_gateway
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }
  ingress {
    description     = "terminal from ALB"
    from_port       = local.port_terminal
    to_port         = local.port_terminal
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_ecs_service" "main" {
  name            = "warehouse"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.main.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = aws_subnet.public[*].id
    security_groups  = [aws_security_group.ecs_tasks.id]
    assign_public_ip = true # required in a public subnet to pull images / reach the internet (no NAT).
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.admin.arn
    container_name   = "admin"
    container_port   = local.port_admin
  }
  load_balancer {
    target_group_arn = aws_lb_target_group.gateway.arn
    container_name   = "gateway"
    container_port   = local.port_gateway
  }
  load_balancer {
    target_group_arn = aws_lb_target_group.terminal.arn
    container_name   = "terminal"
    container_port   = local.port_terminal
  }

  # Give the multi-container task (now including 3 JVM-based Debezium Server instances for
  # CDC-to-Bronze) time to boot before the ALB starts failing it.
  health_check_grace_period_seconds = 180

  depends_on = [
    aws_lb_listener.admin,
    aws_lb_listener.gateway,
    aws_lb_listener.terminal,
  ]
}
