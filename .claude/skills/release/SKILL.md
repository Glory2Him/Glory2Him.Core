---
name: release
description: Prepare a release of everything merged to main since the last tag — the releaser proposes the SemVer bump, you confirm it, it writes the version, docs, changelog, dependency reports and release notes on a release branch, and QA checks the RELEASES: PR. Run as /release on your own machine; /release hotfix <tag> for a hotfix from a tag.
---

You coordinate one release. The `releaser` agent prepares it and the `qa` agent checks
it. You confirm the version with the user, and you never merge, tag or deploy.

Every GitHub call goes through `gh api` (REST).

## Steps

1. **Check there is something to release.** `git fetch --tags origin` and compare the last
   `v*` tag with `origin/main`. Nothing merged since → say so and stop. An open `RELEASES:`
   PR already → stop and point to it.
2. **Launch the `releaser`** with `Propose the next release from main.` (or, for a
   hotfix, see **Hotfixes** below). It returns the proposed version and its evidence.
3. **Confirm the version with the user.** Show the evidence table in at most ten lines. If
   the releaser raised a breaking change without a new `/api/v{major}/` route, stop: that is
   a planner question before any release.
4. **Launch a new `releaser`** with `Prepare release vX.Y.Z from <commit>.` It cuts the
   branch, writes the version, docs, changelog, reports and notes, and opens the
   `RELEASES: vX.Y.Z` PR.
5. **Wait for CI** with `bash scripts/gh-pr-wait-checks.sh <pr>`.
6. **Launch `qa`** at high effort: `Act as QA, checking release PR #<pr>.` On FAIL, or on a PASS that closes
   `MERGE READY: NO` because a finding is still open, launch a
   new `releaser` with `Address the QA findings on release PR #<pr>.` and check again; stop
   and ask after two failed rounds.
7. **Finish** with the PR link, the version, QA's verdict, and the operations notes'
   external steps. The maintainer reviews and merges it with a merge commit. The release
   workflow then tags the head of the release branch and publishes the GitHub Release
   with the notes and an SBOM. A release deploys nothing: DEV already runs `main`, and
   PROD, when it comes, deploys from a tag.

## Hotfixes

`/release hotfix <tag>` ships one fix on top of a released tag, alone, while `main` holds
unreleased work. It runs in two passes, because the fix is built between them:

1. **No `release/vX.Y.(Z+1)` branch yet:** launch the `releaser` with
   `Cut a hotfix branch from <tag>.` It cuts and pushes the branch and nothing else. Tell
   the user to build the fix with `/tweak --base release/vX.Y.(Z+1) <the fix>`, and to
   run `/release hotfix <tag>` again once it has merged. Stop.
2. **The branch exists and holds at least one merged fix:** run steps 2 to 7 with
   `Propose a hotfix release from <tag>.` in step 2 and
   `Prepare release vX.Y.(Z+1) from <the release branch's head>.` in step 4. If the branch
   holds no merged fix yet, say so and stop.
