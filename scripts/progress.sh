#!/usr/bin/env bash
# Where the work stands, from GitHub issues: features by stage, and how many wait.
#
#   scripts/progress.sh
#
# Plan just in time: no more than two features should wait, planned or approved,
# before one of them starts building.
set -euo pipefail
cd "$(dirname "$0")/.."

# gh encodes the -f fields into the query string, so a label with a space or a colon
# needs no separate encoding.
list() {
  gh api -X GET "repos/{owner}/{repo}/issues" -f state=open -f labels="$1" -f per_page=100 --paginate \
    --jq '.[] | select(.pull_request == null) | "#\(.number)\t\(.title)"' | sed 's/^/  /'
}

for stage in "status: needs-design" "status: in-plan-review" "status: awaiting-approval" \
             "approved" "status: building" "status: in-feature-qa" "status: blocked"; do
  echo "## $stage"
  list "$stage"
  echo
done

echo "## Recently merged features"
gh api "repos/{owner}/{repo}/pulls?state=closed&base=main&per_page=50" \
  --jq '.[] | select(.merged_at != null and (.title | test("^(MAJOR )?FEATURES:"))) | "  #\(.number)\t\(.title)\t(\(.merged_at[0:10]))"' | head -10
