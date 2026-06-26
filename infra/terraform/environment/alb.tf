# Public ALB in front of the single task. Three listeners map to the three browser-facing containers
# (admin SPA, terminal SPA, gateway API). Target type "ip" because Fargate/awsvpc tasks register by ENI IP.
#
#   http://<alb-dns>/         -> admin SPA      (container :80)
#   http://<alb-dns>:8081/    -> terminal SPA   (container :8082)
#   http://<alb-dns>:8080/    -> gateway API    (container :8081)

resource "aws_security_group" "alb" {
  name        = "warehouse-alb"
  description = "Ingress to the Warehouse ALB"
  vpc_id      = aws_vpc.main.id

  ingress {
    description = "admin SPA"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = [var.allowed_cidr]
  }
  ingress {
    description = "gateway API"
    from_port   = 8080
    to_port     = 8080
    protocol    = "tcp"
    cidr_blocks = [var.allowed_cidr]
  }
  ingress {
    description = "terminal SPA"
    from_port   = 8081
    to_port     = 8081
    protocol    = "tcp"
    cidr_blocks = [var.allowed_cidr]
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_lb" "main" {
  name               = "warehouse"
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = aws_subnet.public[*].id
}

# --- admin (default :80) --------------------------------------------------------
resource "aws_lb_target_group" "admin" {
  name        = "warehouse-admin"
  port        = 80
  protocol    = "HTTP"
  vpc_id      = aws_vpc.main.id
  target_type = "ip"
  health_check {
    path                = "/"
    matcher             = "200"
    interval            = 15
    healthy_threshold   = 2
    unhealthy_threshold = 5
  }
}

resource "aws_lb_listener" "admin" {
  load_balancer_arn = aws_lb.main.arn
  port              = 80
  protocol          = "HTTP"
  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.admin.arn
  }
}

# --- gateway API (:8080 -> container :8081) -------------------------------------
resource "aws_lb_target_group" "gateway" {
  name        = "warehouse-gateway"
  port        = 8081
  protocol    = "HTTP"
  vpc_id      = aws_vpc.main.id
  target_type = "ip"
  health_check {
    path                = "/health" # mapped only in Development; the gateway runs with that env.
    matcher             = "200"
    interval            = 15
    healthy_threshold   = 2
    unhealthy_threshold = 5
  }
}

resource "aws_lb_listener" "gateway" {
  load_balancer_arn = aws_lb.main.arn
  port              = 8080
  protocol          = "HTTP"
  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.gateway.arn
  }
}

# --- terminal (:8081 -> container :8082) ----------------------------------------
resource "aws_lb_target_group" "terminal" {
  name        = "warehouse-terminal"
  port        = 8082
  protocol    = "HTTP"
  vpc_id      = aws_vpc.main.id
  target_type = "ip"
  health_check {
    path                = "/"
    matcher             = "200"
    interval            = 15
    healthy_threshold   = 2
    unhealthy_threshold = 5
  }
}

resource "aws_lb_listener" "terminal" {
  load_balancer_arn = aws_lb.main.arn
  port              = 8081
  protocol          = "HTTP"
  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.terminal.arn
  }
}
