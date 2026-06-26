# Feed these into the GitHub repo (Settings -> Secrets and variables -> Actions) as repository
# *variables* — see infra/README.md.

output "deploy_role_arn" {
  description = "Set as the AWS_ROLE_ARN repository variable for the deploy workflow."
  value       = aws_iam_role.github_actions.arn
}

output "state_bucket" {
  description = "Set as the TF_STATE_BUCKET repository variable for the deploy workflow."
  value       = aws_s3_bucket.tfstate.id
}

output "aws_region" {
  description = "Set as the AWS_REGION repository variable for the deploy workflow."
  value       = var.aws_region
}

output "ecr_repository_urls" {
  description = "ECR repository URLs (images are pushed here by the workflow)."
  value       = { for name, repo in aws_ecr_repository.this : name => repo.repository_url }
}
