#!/usr/bin/env bash
# Says whether a task PR's green CI already tested the feature-branch tip it merged to.
#
#   scripts/ci-covers-merge.sh <repo> <tested-head> <feature-before> <feature-after>
#
# A pull_request run tests the PR head merged into its base. When the base
# before the merge is an ancestor of the head, that test merge is the head's
# own tree, and so is a merge commit or a rebase merge onto it. If the feature tip after the merge
# has that same tree, CI has already run on exactly this code.
#
# Exits 0 when it has, and non-zero otherwise, including on any git error, so
# a caller that waits for CI on non-zero always fails safe.
set -u

REPO=$1 TESTED=$2 BEFORE=$3 AFTER=$4

git -C "$REPO" merge-base --is-ancestor "$BEFORE" "$TESTED" || exit 1
tested_tree=$(git -C "$REPO" rev-parse --verify --quiet "$TESTED^{tree}") || exit 1
after_tree=$(git -C "$REPO" rev-parse --verify --quiet "$AFTER^{tree}") || exit 1
[ "$tested_tree" = "$after_tree" ]
