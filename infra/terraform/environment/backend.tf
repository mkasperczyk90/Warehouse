# Remote state in the bootstrap-created S3 bucket, so `up` (apply) and `down` (destroy) runs in CI
# share the same state. bucket + region are supplied at init time:
#
#   terraform init \
#     -backend-config="bucket=$TF_STATE_BUCKET" \
#     -backend-config="region=$AWS_REGION"
#
# use_lockfile (Terraform >= 1.10) gives state locking via a lock object in S3 — no DynamoDB table,
# no extra cost.
terraform {
  backend "s3" {
    key          = "warehouse/environment.tfstate"
    encrypt      = true
    use_lockfile = true
  }
}
