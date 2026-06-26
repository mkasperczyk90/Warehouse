variable "aws_region" {
  description = "AWS region (must match the bootstrap stack / where ECR repos live)."
  type        = string
  default     = "eu-central-1"
}

variable "image_tag" {
  description = "Container image tag to deploy (the workflow passes the git SHA)."
  type        = string
  default     = "latest"
}

variable "vpc_cidr" {
  description = "CIDR for the throwaway VPC."
  type        = string
  default     = "10.42.0.0/16"
}

variable "allowed_cidr" {
  description = "Who may reach the ALB (admin/terminal/gateway). Lock to your IP/CIDR for safety; 0.0.0.0/0 is open to the internet."
  type        = string
  default     = "0.0.0.0/0"
}

variable "task_cpu" {
  description = "Fargate task vCPU units for the whole stack (1024 = 1 vCPU). 4096 keeps the 10-container boot snappy; drop to 2048 to save ~40%."
  type        = number
  default     = 4096
}

variable "task_memory" {
  description = "Fargate task memory (MiB). Must be a valid pairing with task_cpu (4096 vCPU -> 8192..30720)."
  type        = number
  default     = 8192
}

variable "log_retention_days" {
  description = "CloudWatch log retention for the task logs."
  type        = number
  default     = 1
}
