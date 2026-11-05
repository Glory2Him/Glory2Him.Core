# Handover template

Post as a comment on the feature issue, beginning `Handover:`, and copy into the feature
PR's body under `## Handover`. Fill every section from what the build produced — the task
issues, their PR descriptions, their `Build cost:` comments and the feature QA rounds.
Write nothing generic: a line that would be true of any feature is noise. A section with
nothing in it says "None"; an empty **External steps** is itself what the reviewer needs
to know.

---

Handover: Feature #<n> — <outcome>

Built <date>. Feature PR #<pr>. Tasks: #<n>, #<n>, #<n>.

## What changed

One line per user story: what a person can now do, and the task PRs behind it. Name the
migration file for any task that has one.

## Before you merge

Only things a person has to judge. For each, name the PR or issue to open.

- **Presentation decisions no criterion covered**, from each PR's **Decisions not in the
  task**. QA did not verify these against a spec.
- **Findings left open** — normally `None`: QA passes nothing while a finding is open, so
  anything here was a question the maintainer settled by leaving it, with their reason.
- **Global rules changed** on the Global Rules issue while this feature was planned or
  built — each `Change <n>:` it relied on.
- **Criteria QA could verify only in part**, and why.
- **Tasks built on top of one that later took a BLOCKING fix.**

## External steps

Everything that happens outside this repository, **in the order it happens**. Merging the
PR into `main` starts the DEV deployment from `build.yml`; nothing else here happens on
its own.

- **Migrations**, in filename order, each marked before or after the app deploys. DEV is
  where a migration first meets Azure SQL.
- **Azure configuration** — app settings, Key Vault secrets, Service Bus entities, storage
  containers — for DEV, which every merge to `main` deploys, and the same again for PROD
  once it exists.
- **What to check on DEV** after the deployment: connection strings, token handling and
  retries first meet Azure there.
- **One-off queries** to run against hosted data, with the query.
- **Anything with a window** — a change that breaks clients on the current release.

## If it goes wrong

The one or two steps above that cannot simply be reverted, and what to do instead.
Migrations are append-only: rolling one back means a new migration.

## Not covered

What the feature deliberately left out, from its Out of scope section.

## Cost

Spec, build and total, from the `Spec cost:` and `Build cost:` comments.
