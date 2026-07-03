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
  description = <<-EOT
    Fargate task vCPU units for the whole stack (1024 = 1 vCPU). Left null (the default), this
    auto-sizes to 8192 when bi_lake_bucket_name is set (the 3 extra Debezium Server JVM containers
    added for CDC-to-Bronze need that headroom) or 4096 when it's not — see locals.tf's
    effective_task_cpu. Set explicitly to override either default.
  EOT
  type        = number
  default     = null
}

variable "task_memory" {
  description = <<-EOT
    Fargate task memory (MiB). Must be a valid pairing with the effective task_cpu (4096 vCPU ->
    8192..30720; 8192 vCPU -> 16384..61440). Left null (the default), auto-sizes alongside
    task_cpu — see locals.tf's effective_task_memory. Set explicitly to override.
  EOT
  type        = number
  default     = null
}

variable "log_retention_days" {
  description = "CloudWatch log retention for the task logs."
  type        = number
  default     = 1
}

variable "bi_lake_bucket_name" {
  description = <<-EOT
    Name of the durable BI Bronze landing bucket created by ../bootstrap (its `bi_lake_bucket`
    output). Empty string disables BI export: the task role gets no S3 permission and
    warehousing-api isn't given a bucket to write to (see docs/BI_Plan.md §11).
  EOT
  type        = string
  default     = ""
}
