#!/usr/bin/env bash
#
# One-shot setup of the 5-column "Status" board on the GitHub Projects v2 project named
# "Warehouse". Replaces the Status field's single-select options with:
#   Todo -> In Progress -> In Review -> Tests Passed -> Done
#
# Requires: gh CLI authenticated with the `project` scope (no external `jq` needed — this
# uses gh's built-in --jq engine).
#   gh auth login            # choose "GitHub.com", and grant the `project` scope, OR:
#   gh auth refresh -s project,read:project
#
# WARNING: this REPLACES the existing Status options. Items sitting on a removed/renamed
# option lose their Status value, so re-triage afterwards (this script does NOT migrate cards).
#
# Usage: scripts/setup-project-board.sh [owner] [project-title]
set -euo pipefail

OWNER="${1:-mkasperczyk90}"
TITLE="${2:-Warehouse}"
STATUS_FIELD="Status"

echo "Resolving project '$TITLE' for user '$OWNER'..."
project_id=$(gh api graphql -f login="$OWNER" -f query='
  query($login:String!){
    user(login:$login){ projectsV2(first:50){ nodes{ id title } } }
  }' --jq ".data.user.projectsV2.nodes[] | select(.title==\"$TITLE\") | .id" | head -n1)
if [ -z "$project_id" ] || [ "$project_id" = "null" ]; then
  echo "ERROR: project '$TITLE' not found for user '$OWNER'." >&2
  echo "If it is an org project, edit this script to use organization(login:...) instead of user(login:...)." >&2
  exit 1
fi
echo "  project id: $project_id"

echo "Resolving '$STATUS_FIELD' field..."
field_id=$(gh api graphql -f project="$project_id" -f query='
  query($project:ID!){
    node(id:$project){ ... on ProjectV2 {
      fields(first:50){ nodes{ ... on ProjectV2SingleSelectField { id name } } }
    } }
  }' --jq ".data.node.fields.nodes[] | select(.name==\"$STATUS_FIELD\") | .id")
if [ -z "$field_id" ] || [ "$field_id" = "null" ]; then
  echo "ERROR: single-select field '$STATUS_FIELD' not found on the project." >&2
  exit 1
fi
echo "  field id: $field_id"

echo "Setting 5 columns: Todo, In Progress, In Review, Tests Passed, Done ..."
gh api graphql -f field="$field_id" -f query='
  mutation($field:ID!){
    updateProjectV2Field(input:{
      fieldId:$field
      singleSelectOptions:[
        { name:"Todo",         color:GRAY,   description:"Planned / not started" },
        { name:"In Progress",  color:BLUE,   description:"Being worked on; CI failure sends issues back here" },
        { name:"In Review",    color:YELLOW, description:"A PR links this issue (Closes #N) and is ready for review" },
        { name:"Tests Passed", color:GREEN,  description:"All required CI gates green on the PR; ready to merge" },
        { name:"Done",         color:PURPLE, description:"PR merged / issue closed" }
      ]
    }){ projectV2Field{ ... on ProjectV2SingleSelectField { id options{ name } } } }
  }' --jq '.data.updateProjectV2Field.projectV2Field.options[].name | "  - " + .'

echo "Done. Now enable the built-in Project workflows in the UI:"
echo "  Item added to project        -> Todo"
echo "  Item reopened                -> In Progress"
echo "  Pull request merged / closed -> Done"
