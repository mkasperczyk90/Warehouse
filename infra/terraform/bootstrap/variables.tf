variable "aws_region" {
  description = "AWS region for the bootstrap resources (use the same region as the environment stack)."
  type        = string
  default     = "eu-central-1"
}

variable "github_repo" {
  description = "GitHub repo allowed to assume the deploy role, as owner/name."
  type        = string
  default     = "mkasperczyk90/Warehouse"
}

variable "state_bucket_name" {
  description = "Override the S3 bucket name for the environment state. Defaults to warehouse-tfstate-<account-id>."
  type        = string
  default     = null
}
