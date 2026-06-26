# Warehouse AWS bootstrap — run ONCE, by hand, with admin-ish credentials.
#
# Creates the durable, near-$0 pieces the ephemeral environment + the GitHub workflow depend on:
#   - an S3 bucket that holds the *environment* Terraform state (so `up` and `down` runs share state),
#   - a GitHub OIDC provider + an IAM role the deploy workflow assumes (no long-lived AWS keys),
#   - one ECR repository per image the deploy builds and pushes.
#
# This stack keeps its OWN state locally (chicken-and-egg: it creates the bucket the other stack uses).
# Keep the generated terraform.tfstate around, or import if you lose it. Cost while idle: a few cents
# (ECR storage) — the expensive, ephemeral compute lives in ../environment and is destroyed each cycle.
#
#   cd infra/terraform/bootstrap
#   terraform init && terraform apply
#   terraform output            # feed these into the GitHub repo (see infra/README.md)

terraform {
  required_version = ">= 1.10"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
  default_tags {
    tags = {
      Project   = "warehouse"
      ManagedBy = "terraform"
      Stack     = "bootstrap"
    }
  }
}

data "aws_caller_identity" "current" {}

locals {
  state_bucket = coalesce(var.state_bucket_name, "warehouse-tfstate-${data.aws_caller_identity.current.account_id}")
  # Images the deploy workflow builds + pushes (one ECR repo each).
  ecr_repos = [
    "warehouse-gateway",
    "warehouse-masterdata-api",
    "warehouse-warehousing-api",
    "warehouse-logistics-api",
    "warehouse-admin",
    "warehouse-terminal",
    "warehouse-keycloak",
  ]
}

# --- Terraform state bucket for the ephemeral environment stack ------------------
resource "aws_s3_bucket" "tfstate" {
  bucket = local.state_bucket
}

resource "aws_s3_bucket_versioning" "tfstate" {
  bucket = aws_s3_bucket.tfstate.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "tfstate" {
  bucket = aws_s3_bucket.tfstate.id
  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}

resource "aws_s3_bucket_public_access_block" "tfstate" {
  bucket                  = aws_s3_bucket.tfstate.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# --- ECR repositories -----------------------------------------------------------
resource "aws_ecr_repository" "this" {
  for_each             = toset(local.ecr_repos)
  name                 = each.value
  image_tag_mutability = "MUTABLE"
  force_delete         = true # let `terraform destroy` of bootstrap remove repos even with images.

  image_scanning_configuration {
    scan_on_push = true
  }
}

# Keep storage near-zero: drop untagged layers quickly.
resource "aws_ecr_lifecycle_policy" "this" {
  for_each   = aws_ecr_repository.this
  repository = each.value.name
  policy = jsonencode({
    rules = [{
      rulePriority = 1
      description  = "Expire untagged images after 1 day"
      selection = {
        tagStatus   = "untagged"
        countType   = "sinceImagePushed"
        countUnit   = "days"
        countNumber = 1
      }
      action = { type = "expire" }
    }]
  })
}

# --- GitHub Actions OIDC -> IAM role -------------------------------------------
# Lets the workflow get short-lived AWS credentials by presenting a GitHub-signed token; no static keys.
resource "aws_iam_openid_connect_provider" "github" {
  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]
  # AWS no longer verifies these for GitHub's IdP, but the provider still requires the field.
  thumbprint_list = [
    "6938fd4d98bab03faadb97b34396831e3780aea1",
    "1c58a3a8518e8759bf075b76b750d4f2df264fcd",
  ]
}

data "aws_iam_policy_document" "github_assume" {
  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]
    effect  = "Allow"
    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.github.arn]
    }
    condition {
      test     = "StringEquals"
      variable = "token.actions.githubusercontent.com:aud"
      values   = ["sts.amazonaws.com"]
    }
    condition {
      test     = "StringLike"
      variable = "token.actions.githubusercontent.com:sub"
      values   = ["repo:${var.github_repo}:*"]
    }
  }
}

resource "aws_iam_role" "github_actions" {
  name               = "warehouse-github-deploy"
  assume_role_policy = data.aws_iam_policy_document.github_assume.json
}

# Pragmatic, demo-scoped policy: enough to push images and to apply/destroy the ephemeral stack
# (VPC/ECS/ALB/logs + the task-execution role it manages). Tighten before any production use; if a
# `terraform apply` ever fails on a missing permission, this is the place to add it.
data "aws_iam_policy_document" "github_deploy" {
  statement {
    sid    = "EcrPushPull"
    effect = "Allow"
    actions = [
      "ecr:GetAuthorizationToken",
      "ecr:BatchCheckLayerAvailability",
      "ecr:BatchGetImage",
      "ecr:GetDownloadUrlForLayer",
      "ecr:PutImage",
      "ecr:InitiateLayerUpload",
      "ecr:UploadLayerPart",
      "ecr:CompleteLayerUpload",
      "ecr:DescribeRepositories",
      "ecr:ListImages",
    ]
    resources = ["*"]
  }
  statement {
    sid    = "ProvisionEphemeralStack"
    effect = "Allow"
    actions = [
      "ec2:*",
      "ecs:*",
      "elasticloadbalancing:*",
      "logs:*",
      "application-autoscaling:*",
      "servicediscovery:*",
    ]
    resources = ["*"]
  }
  statement {
    sid    = "ManageTaskRoles"
    effect = "Allow"
    actions = [
      "iam:CreateRole",
      "iam:DeleteRole",
      "iam:GetRole",
      "iam:PassRole",
      "iam:TagRole",
      "iam:UntagRole",
      "iam:ListRolePolicies",
      "iam:ListAttachedRolePolicies",
      "iam:AttachRolePolicy",
      "iam:DetachRolePolicy",
      "iam:PutRolePolicy",
      "iam:GetRolePolicy",
      "iam:DeleteRolePolicy",
      "iam:CreateServiceLinkedRole",
    ]
    resources = ["*"]
  }
  statement {
    sid       = "TerraformState"
    effect    = "Allow"
    actions   = ["s3:ListBucket", "s3:GetObject", "s3:PutObject", "s3:DeleteObject"]
    resources = [aws_s3_bucket.tfstate.arn, "${aws_s3_bucket.tfstate.arn}/*"]
  }
}

resource "aws_iam_role_policy" "github_deploy" {
  name   = "warehouse-deploy"
  role   = aws_iam_role.github_actions.id
  policy = data.aws_iam_policy_document.github_deploy.json
}
