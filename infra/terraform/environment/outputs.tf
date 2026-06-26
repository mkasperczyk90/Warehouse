output "admin_url" {
  description = "Admin panel (MSW-mocked SPA)."
  value       = "http://${aws_lb.main.dns_name}/"
}

output "terminal_url" {
  description = "Operator terminal (MSW-mocked SPA)."
  value       = "http://${aws_lb.main.dns_name}:8081/"
}

output "gateway_url" {
  description = "API gateway (YARP). e.g. POST /api/auth/login, GET /api/search."
  value       = "http://${aws_lb.main.dns_name}:8080/"
}

output "alb_dns_name" {
  value = aws_lb.main.dns_name
}
