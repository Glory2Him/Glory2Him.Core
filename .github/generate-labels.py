#!/usr/bin/env python3
"""Generate .github/labels.json from the authoritative prefix list in prLinter.yml.

Run from the repository root after adding a prefix to prLinter.yml:

    .github/generate-labels.py .github/workflows/prLinter.yml .github/labels.json

It fails rather than guessing when a prefix has no colour, so a new prefix cannot
silently become a default grey label the first time someone uses it in a PR title.
It also fails on a description longer than the 100 characters GitHub accepts, which
the Labels workflow would otherwise only discover at sync time.
"""
import json
import re
import sys

if len(sys.argv) != 3:
    sys.exit("usage: .github/generate-labels.py .github/workflows/prLinter.yml .github/labels.json")
PR_LINTER = sys.argv[1]
OUT = sys.argv[2]

# Colours and descriptions read from the live Glory2Him.Core labels via the GitHub API.
# Entries marked INVENTED had no label on the repo, or carried GitHub's default grey
# (ededed) because prLinter's addLabels call auto-created them.
BASE = {
    "INFRA":          ("1E90FF", "Project setup, creating the build project / build scripts"),
    "PROVISIONS":     ("2E8B57", "Provisions scripts"),
    "RELEASES":       ("2AE9F8", "The releases category"),
    "FEATURES":       ("0E8A16", "A whole feature merging from its feature branch into main"),  # INVENTED (missing)
    "DATA":           ("DAA520", "The data category"),                      # INVENTED (was grey)
    "BROKERS":        ("4682B4", "The brokers category"),                   # INVENTED (was grey)
    "FOUNDATIONS":    ("A11EE7", "The foundations category"),
    "PROCESSINGS":    ("8A2BE2", "The processings category"),
    "ORCHESTRATIONS": ("20B2AA", "The orchestrations category"),
    "COORDINATIONS":  ("5F9EA0", "The coordinations category"),             # INVENTED (missing)
    "MANAGEMENTS":    ("B60205", "The management category"),
    "AGGREGATIONS":   ("426DD3", "The aggregations category"),
    "CONTROLLERS":    ("DB3ABE", "The controllers category"),
    "CLIENTS":        ("DB3ABE", "The clients category"),
    "EXPOSERS":       ("DB3ABE", "The exposers category"),
    "PROVIDERS":      ("DB3ABE", "The providers category"),
    "BASE":           ("708090", "The base components category"),           # INVENTED (missing)
    "COMPONENTS":     ("00CED1", "The components category"),
    "VIEWS":          ("9370DB", "The views category"),
    "PAGES":          ("C4BB46", "The pages category"),
    "ACCEPTANCE":     ("40E0D0", "The acceptance tests category"),
    "INTEGRATIONS":   ("7B68EE", "The integration tests category"),
    "CODE RUB":       ("9A3BC5", "The code rub category is for small changes (not for bug fixes)"),
    "DOCUMENTATION":  ("0075CA", "The documentation category"),             # INVENTED (missing)
    "CONFIG":         ("8FBC8F", "The configuration category is for any configuration changes i.e. setting up appsettings.json"),
    "STANDARD":       ("006B75", "Changes to The Standard skills or the engineering rules"),  # INVENTED (missing)
    "DESIGN":         ("FF8C00", "Architecture and design decisions"),      # INVENTED (was grey)
    "BUSINESS":       ("3CB371", "The business category is for documentation of business processes / Standard Operating Procedures"),
    "MIGRATIONS":     ("CD853F", "Database schema migrations"),             # INVENTED (missing)
    "PLANNING":       ("BFD4F2", "Planning and backlog shaping"),           # INVENTED (missing)
    "MENTORSHIP":     ("FEF2C0", "Mentorship and knowledge sharing"),       # INVENTED (missing)
    "DISCUSSION":     ("D4C5F9", "Open discussion, no code change expected"),  # INVENTED (missing)
    "IMPORTS":        ("C2E0C6", "Bulk imports of content or data"),        # INVENTED (missing)
    "REVIEWS":        ("BFDADC", "Code or content review work"),            # INVENTED (missing)
    "STATUS":         ("C5DEF5", "Status reporting, no code change"),       # INVENTED (missing)
}

# Labels whose live colour and wording do not follow the base + tier pattern.
OVERRIDES = {
    "MINOR FIX":  ("E99695", "A minor bug fix / change that do not significantly alter the overall functionality of the program"),
    "MEDIUM FIX": ("D93F0B", "A medium fix has a moderate level of impact on the functionality or performance of the program"),
    "MAJOR FIX":  ("B60205", "A major bug fix is a significant repair or correction made to addresses a critical or major issue"),
}

TIERS = {
    "MINOR": "a minor change",
    "MEDIUM": "a medium change",
    "MAJOR": "a major change",
}

# The effort ladder is Low / Medium / High / Extra / Max. It is not a tidy matrix: the
# Opus families carry all five rungs and the rest carry the bottom three, so there is no
# Sonnet 5 - Max and no Fable 5 - Extra. DEVELOPERS.md section 10 is the written record.
#
# "Small" was the old name for the bottom rung on the five-rung models, which left the
# same rung called two different things depending on the model. It is retired.
#
# Only Opus 5.5 is chosen: DEVELOPERS.md section 10 fixes the model and varies the effort.
# Opus 5, Sonnet 5, Fable 5 and Haiku 4.5 are listed to be NORMALISED, not to be chosen.
# Their labels stay because they carry closed issues genuinely built under them. Listing
# them here keeps the sync tidying their colour and description; the sync never deletes,
# so leaving them out would simply abandon them to however they were hand-created.
MODEL_EFFORTS = {
    "Opus 5.5": ["Low", "Medium", "High", "Extra", "Max"],
    "Opus 5": ["Low", "Medium", "High", "Extra", "Max"],
    "Sonnet 5": ["Low", "Medium", "High"],
    "Fable 5": ["Low", "Medium", "High"],
    "Haiku 4.5": ["Low", "Medium", "High"],
}
EFFORT_COLOUR = "fbca04"

# The issue lifecycle. A feature moves needs-design -> in-plan-review ->
# awaiting-approval -> building -> in-feature-qa -> done; a task moves needs-scoping ->
# in-progress -> done. Approval is not a status: it is the approved label below.
# DEVELOPERS.md section 7 is the written record.
STATUS_LABELS = [
    ("status: needs-design", "d4c5f9", "Feature request raised; the planner has not designed it yet"),
    ("status: in-plan-review", "c5def5", "Plan written; QA is reviewing the stories and tasks"),
    ("status: awaiting-approval", "fbca04", "QA signed the plan; waiting on the maintainer to approve it"),
    ("status: building", "1d76db", "Approved; the feature build is running task by task"),
    ("status: in-feature-qa", "5319e7", "Every task merged; QA is verifying the whole feature"),
    ("status: blocked", "b60205", "Stopped on a question or a failure a person must resolve"),
    ("status: needs-scoping", "e4e669", "Criteria written, waiting on QA to sign them off"),
    ("status: in-progress", "1d76db", "A developer is implementing it"),
    ("status: in-qa", "5319e7", "Implemented, under adversarial verification"),
    ("status: done", "6a737d", "Built and verified: a task merged into its feature branch, or a feature PR awaiting the maintainer"),
]

# Issue types (Feature, User Story, Task) are set with the issue type field where the
# organisation has them. These labels are the fallback where it does not, and the
# planner applies both so a query on either works.
TYPE_LABELS = [
    ("type: feature", "a2eeef", "A behaviour the service should have; parent of user stories"),
    ("type: user-story", "bfdadc", "One user's slice of a feature; parent of tasks"),
    ("type: task", "ededed", "One unit of work a developer builds in one PR"),
]

# approved is the human decision: only a person adds it, never an agent — on a
# feature, on the Global Rules issue, or anywhere else. QA's signed body hashes, not a
# second approver, are what protect an approved plan.
# tier: 3 marks a /tweak change that goes straight to main with no design.
# global rules marks the one issue holding the rules every feature inherits; the agents
# find it by this label, and it is approved the same way a plan is.
# legacy marks a task signed ready for development before the repository moved to the
# issue-based workflow, and the Feature and User Story issues made to hold it. Only a
# legacy task is built and verified against the documents it cites under
# Documentation/Planning/, and the planner never adds the label.
# Documentation/Procedures/migrating-an-existing-repository.md is the written record.
WORKFLOW_LABELS = [
    ("approved", "0e8a16", "The maintainer approved the plan; the build may start"),
    ("tier: 3", "c2e0c6", "A small change through /tweak: no design, PR straight to main"),
    ("global rules", "5319e7", "The rules every feature inherits; changed only through QA and the maintainer's approval"),
    ("legacy", "d4c5f9", "Signed off before the issue-based workflow; built against the Planning documents it cites"),
]

# QA's verdicts: ready for development on a task whose plan it signed, ready for review
# on a PR it passes, each only once no finding of any severity is open. The developer
# will not start a task without ready for development, and the build driver will not
# start a feature without approved as well.
# .claude/agents/qa.md makes applying them mandatory, and adding a label the repository
# does not have creates it grey rather than failing, so the sync must run first. There is no
# separate merge-ready label: ready for review already carries QA's MERGE READY: YES.
# Colours are copied from the live Glory2Him.Template labels. The descriptions are this
# file's own, and the label sync carries them to the live labels.
QA_LABELS = [
    ("ready for development", "0e8a16", "QA signed the task off in the plan review; built once the feature is approved"),
    ("ready for review", "0e8a16", "QA passed the PR with no finding of any severity open; ready for a human merge review"),
]

# One per design area, applied by the planner so an area has a live query that never
# goes stale. Note the trap: the all-caps DESIGN label is a PR category label the
# linter auto-applies from a title prefix, and is a different thing entirely.
DESIGN_AREA_COLOUR = "f9d0c4"
DESIGN_AREAS = ["architecture", "domain", "events", "security", "ui"]
EFFORT_DESCRIPTION = "Suggested model and effort for this issue"

with open(PR_LINTER, encoding="utf-8") as file:
    source = file.read()

prefixes = re.findall(r"^\s*'([A-Z][A-Z ]*):',?\s*$", source, re.MULTILINE)

if not prefixes:
    raise SystemExit("No prefixes found in prLinter.yml — has the workflow format changed?")

labels = []
unknown = []

for prefix in prefixes:
    name = prefix.rstrip(":")

    if name in OVERRIDES:
        colour, description = OVERRIDES[name]
    else:
        tier, _, remainder = name.partition(" ")
        if tier in TIERS and remainder in BASE:
            colour, base_description = BASE[remainder]
            description = f"{base_description} — {TIERS[tier]}"
        elif name in BASE:
            colour, description = BASE[name]
        else:
            unknown.append(name)
            continue

    labels.append({"name": name, "color": colour.lower(), "description": description})

for model, efforts in MODEL_EFFORTS.items():
    for effort in efforts:
        labels.append({
            "name": f"{model} - {effort}",
            "color": EFFORT_COLOUR,
            "description": EFFORT_DESCRIPTION,
        })

for name, colour, description in STATUS_LABELS:
    labels.append({"name": name, "color": colour, "description": description})

for name, colour, description in TYPE_LABELS + WORKFLOW_LABELS + QA_LABELS:
    labels.append({"name": name, "color": colour, "description": description})

for area in DESIGN_AREAS:
    labels.append({
        "name": f"design: {area}",
        "color": DESIGN_AREA_COLOUR,
        "description": f"Touches the {area} design",
    })

if unknown:
    raise SystemExit(f"No colour mapping for: {', '.join(unknown)}")

names = [label["name"] for label in labels]
duplicates = {name for name in names if names.count(name) > 1}

if duplicates:
    raise SystemExit(f"Duplicate labels: {', '.join(sorted(duplicates))}")

# GitHub refuses a label description over 100 characters, and the Labels workflow would
# then fail on that one label at sync time. Refuse it here instead, where it is written.
MAX_DESCRIPTION = 100
too_long = [f"{label['name']} ({len(label['description'])})" for label in labels
            if len(label["description"]) > MAX_DESCRIPTION]

if too_long:
    raise SystemExit(f"Descriptions over {MAX_DESCRIPTION} characters: {', '.join(too_long)}")

with open(OUT, "w", encoding="utf-8", newline="\n") as file:
    json.dump(labels, file, indent=2, ensure_ascii=False)
    file.write("\n")

print(f"{len(labels)} labels written to {OUT} ({len(prefixes)} prefixes + {sum(len(e) for e in MODEL_EFFORTS.values())} model-effort + {len(STATUS_LABELS)} status + {len(TYPE_LABELS)} type + {len(WORKFLOW_LABELS)} workflow + {len(QA_LABELS)} QA + {len(DESIGN_AREAS)} design area)")
