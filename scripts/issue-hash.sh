#!/usr/bin/env bash
# Prints a short, stable hash of a GitHub issue body.
#
#   scripts/issue-hash.sh <issue-number>
#
# QA records these hashes when it signs a plan off — or takes a legacy feature in — and the
# build driver recomputes them before every task and halts when one differs, so an approved
# plan cannot change unnoticed.
#
# The body is normalised first, so harmless differences do not count as changes:
#   - Windows line endings become Unix ones;
#   - every ticked checkbox (- [x]) is reset to unticked (- [ ]), because the developer
#     ticks criteria as tests go green;
#   - trailing whitespace on each line, and trailing blank lines, are removed.
# Anything else — a reworded criterion, a new rule — changes the hash.
#
# Runs under Git Bash on Windows and bash on Linux; macOS has shasum where Linux has sha256sum.
set -euo pipefail
n=${1:?usage: issue-hash.sh <issue-number>}

sha256() { if command -v sha256sum >/dev/null 2>&1; then sha256sum; else shasum -a 256; fi; }

gh api "repos/{owner}/{repo}/issues/$n" --jq .body |
  tr -d '\r' |
  sed -E 's/^([[:space:]]*[-*+][[:space:]]+)\[[xX]\]/\1[ ]/' |
  sed -E 's/[[:space:]]+$//' |
  sed -e ':a' -e '/^\n*$/{$d;N;ba' -e '}' |
  sha256 | cut -c1-16
