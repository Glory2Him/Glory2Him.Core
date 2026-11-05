#!/usr/bin/env bash
# Prints every service class that carries a recorded deviation, one per line, for
#
#   scripts/list-deviations.sh > "$tmp/deviations.txt"
#   python3 scripts/architecture-graph.py --check --deviations "$tmp/deviations.txt"
#
# (On Windows, run the second line with `python`.)
#
# A deviation is recorded in one of two places, each a line of its own:
#
#   - in a user story issue, under its "## Deviations" heading, as
#       - **<ClassName>** — <the rule> — <the reason> — rejected: <the alternatives>
#     The planner writes it; QA's plan review and the maintainer's approved grant it.
#   - in a decision record under Documentation/Decisions/, as
#       **Deviation:** <ClassName> — <the rule>
#     for code that has no user story issue — a service designed before the repository
#     moved its design into issues, whose deviation sat in an architecture document's
#     register, say. The maintainer merges the DESIGN: PR.
#
# It reads issue bodies through the search API, so an issue edited in the last minute may
# be missed, and it trusts the line format rather than checking who approved it: QA's plan
# review is what checks that.
set -euo pipefail

repo=$(gh api 'repos/{owner}/{repo}' --jq .full_name)

{
  gh api -X GET search/issues --paginate -f q="repo:$repo in:body Deviations" --jq '.items[].body' |
    tr -d '\r' |
    awk '
      /^##[[:space:]]/ { inside = ($0 ~ /^##[[:space:]]+Deviations/) }
      inside && match($0, /^[[:space:]]*[-*][[:space:]]+\*\*[A-Za-z0-9_]+\*\*/) {
        name = substr($0, RSTART, RLENGTH)
        gsub(/[-*[:space:]]/, "", name)
        print name
      }'
  if [ -d Documentation/Decisions ]; then
    grep -rhoE '^\*\*Deviation:\*\*[[:space:]]+[A-Za-z0-9_]+' Documentation/Decisions | awk '{print $2}' || true
  fi
} | tr -d '\r' | sort -u
