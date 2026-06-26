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

resource "aws_ecs_cluster" "main" {
  name = "warehouse"
}

resource "aws_ecs_task_definition" "main" {
  family                   = "warehouse"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = var.task_cpu
  memory                   = var.task_memory
  execution_role_arn       = aws_iam_role.task_execution.arn
  container_definitions    = jsonencode(local.containers)
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

  # Give the 10-container task time to boot before the ALB starts failing it.
  health_check_grace_period_seconds = 180

  depends_on = [
    aws_lb_listener.admin,
    aws_lb_listener.gateway,
    aws_lb_listener.terminal,
  ]
}
