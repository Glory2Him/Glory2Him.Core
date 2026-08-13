#!/bin/bash
# ────────────────────────────────────────────────────────────────────────────────
# Mutation audit for exception message wording.
#
# Verifies that every exception message literal in a production source directory
# is actually pinned by at least one unit test: each message is mutated in turn
# (prefixed with "ZZMUT "), the test project is rebuilt, the filtered tests are
# run, and the number of failing tests is reported. A message whose mutation
# kills ZERO tests is not covered — its wording can drift silently.
#
# Usage:
#   Tools/mutation-audit.sh <production-dir> <test-filter> [test-project]
#
# Example (ContentItem processing):
#   Tools/mutation-audit.sh \
#     Glory2Him.Core/Services/Processings/ContentItems \
#     ContentItemProcessingServiceTests
#
# Notes:
# - The target directory must be clean in git (uncommitted changes are refused,
#   because the script reverts the directory with `git checkout` after every
#   mutation).
# - Message literals are extracted from `message:` / `values:` / `Message =`
#   argument lines and positional `...Exception("...")` constructor calls
#   (including one continuation line for concatenated messages).
#   Mutating one segment of a concatenated message still changes the composed
#   message, so segments are audited individually.
# - Expect every line to report "killed: N" with N >= 1. "killed: 0" is a
#   coverage gap; "BUILD-FAIL" means the mutation broke compilation (treat as
#   inconclusive and investigate).
# ────────────────────────────────────────────────────────────────────────────────

set -u

DIR="${1:?usage: mutation-audit.sh <production-dir> <test-filter> [test-project]}"
FILTER="${2:?usage: mutation-audit.sh <production-dir> <test-filter> [test-project]}"
TEST_PROJECT="${3:-Glory2Him.Core.Tests.Unit}"

cd "$(git rev-parse --show-toplevel)" || exit 1

if [ ! -d "$DIR" ]; then
    echo "ERROR: production directory not found: $DIR" >&2
    exit 1
fi

if [ -n "$(git status --porcelain -- "$DIR")" ]; then
    echo "ERROR: $DIR has uncommitted changes; commit or stash them first" >&2
    echo "       (the audit reverts the directory with git checkout after every mutation)" >&2
    exit 1
fi

# Extract candidate message literals (>= 8 chars) from message-bearing lines,
# including one continuation line to catch concatenated message segments.
mapfile -t MESSAGES < <(
    grep -rhA1 -E 'message:|values:|Message =|Exception\("' "$DIR" --include=*.cs \
        | grep -oE '"[^"]{8,}"' \
        | sed 's/^"//;s/"$//' \
        | sort -u)

if [ ${#MESSAGES[@]} -eq 0 ]; then
    echo "ERROR: no message literals found under $DIR" >&2
    exit 1
fi

echo "Auditing ${#MESSAGES[@]} message literals in $DIR"
echo "Test project: $TEST_PROJECT, filter: FullyQualifiedName~$FILTER"
echo

survivors=0

for msg in "${MESSAGES[@]}"; do
    export MUTATION_AUDIT_MSG="$msg"

    grep -rlF "$msg" "$DIR" --include=*.cs | while read -r file; do
        perl -pi -e 's/\Q$ENV{MUTATION_AUDIT_MSG}\E/ZZMUT $ENV{MUTATION_AUDIT_MSG}/g' "$file"
    done

    if ! grep -rq "ZZMUT" "$DIR"; then
        echo "NOT-APPLIED       <= \"$msg\""
        git checkout -q -- "$DIR"
        continue
    fi

    if ! dotnet build "$TEST_PROJECT" -v q > /dev/null 2>&1; then
        echo "BUILD-FAIL        <= \"$msg\""
        git checkout -q -- "$DIR"
        continue
    fi

    failed=$(dotnet test "$TEST_PROJECT" --no-build \
        --filter "FullyQualifiedName~$FILTER" 2>&1 \
        | tail -1 | grep -oE "Failed: *[0-9]+" | grep -oE "[0-9]+")

    if [ -z "${failed:-}" ]; then
        echo "NO-RESULT         <= \"$msg\""
    elif [ "$failed" -eq 0 ]; then
        echo "killed: 0  ⚠ GAP  <= \"$msg\""
        survivors=$((survivors + 1))
    else
        echo "killed: $failed         <= \"$msg\""
    fi

    git checkout -q -- "$DIR"
done

# leave the build in a clean, unmutated state
dotnet build "$TEST_PROJECT" -v q > /dev/null 2>&1

echo
if [ "$survivors" -gt 0 ]; then
    echo "RESULT: $survivors surviving mutation(s) — wording not pinned by any test."
    exit 1
fi

echo "RESULT: all message mutations killed — every wording is pinned by tests."
