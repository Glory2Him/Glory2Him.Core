#!/usr/bin/env bash
# Waits for every check on a PR's head commit to finish, through the REST API.
#
#   scripts/gh-pr-wait-checks.sh <pr-number> [timeout-seconds]
#
# Exits 0 when all checks completed successfully (or were skipped/neutral), 1 when any
# failed, was cancelled or timed out, and 2 when the wait itself timed out. Waits for the
# required Build check to appear before judging, so a run that has not been queued yet is
# not mistaken for "no checks".
#
# Every filter runs in gh's own --jq, so this needs no separate jq, and its output has Unix
# line endings under Git Bash on Windows as well as on Linux and macOS.
set -uo pipefail
pr=${1:?usage: gh-pr-wait-checks.sh <pr-number> [timeout-seconds]}
limit=${2:-3600}
start=$(date +%s)

while :; do
  sha=$(gh api "repos/{owner}/{repo}/pulls/$pr" --jq .head.sha) || exit 1
  # One line: total, pending, how many are named Build, then the failed names.
  line=$(gh api "repos/{owner}/{repo}/commits/$sha/check-runs?per_page=100" \
    --jq '[.check_runs[] | {name, status, conclusion}] as $r
          | "\($r | length) \([$r[] | select(.status != "completed")] | length) \([$r[] | select(.name == "Build")] | length) \([$r[] | select(.status == "completed" and (.conclusion | IN("success", "skipped", "neutral") | not)) | .name] | join(", "))"') || exit 1
  read -r total pending has_build failed <<<"$line"

  if [ -n "$failed" ]; then echo "PR #$pr: failed: $failed"; exit 1; fi
  if [ "$total" -gt 0 ] && [ "$pending" -eq 0 ] && [ "$has_build" -gt 0 ]; then
    echo "PR #$pr: $total checks passed at ${sha:0:7}"; exit 0
  fi
  if [ $(( $(date +%s) - start )) -gt "$limit" ]; then
    echo "PR #$pr: still waiting after ${limit}s ($pending pending of $total)"; exit 2
  fi
  sleep 20
done
