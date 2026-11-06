# Mockups

Yours: UI wireframes and Claude Design HTML exports, on their way into a feature
issue's business rules. `DEVELOPERS.md` §5.2 describes the full flow. The planner
opens an export a feature issue links to; no agent changes this folder, and the
releaser documents a feature from its issue, not from here.

Images alone do not need to live here: drag them into the feature request's
Mockups field, where GitHub hosts them. Commit an export here when the interaction
matters — hover states, spacing, the real DOM — that a picture cannot show. Drag a
flattened image of the same screen into the issue as well: a bundled HTML file does
not render in an issue body.

## Layout

```
Documentation/Mockups/<feature-slug>/
  <screen-name>.html     the Claude Design export, self-contained
  README.md              what it shows, and the feature issue it feeds
```

Bundled exports run to several megabytes. Do not commit five versions of a screen
because a design iterated.

A repository moved onto this workflow from the older template keeps its mockups here
too. Their README lines may still name an old feature document; that document now
sits under `Documentation/Planning/DesignFeatures/`.

## A mockup is never the design

The planner writes every rule a mockup shows into the feature issue as a numbered
business rule. From that moment the business rules are authoritative and the mockup
is history. Record it in the folder README:

```markdown
# Saved searches panel
Source: Claude Design export, 2026-09-11. Feature: #500.
Superseded by #500's business rules — they win wherever the two disagree.
```

A mockup left without that line is a second source of truth waiting to contradict
the plan. "Matches the mockup" is never a rule or a criterion.

## Linking an export from an issue

Pin the URL to the full 40-character commit SHA, never a branch, so what the issue
points at cannot change under it later:

```markdown
[Panel](https://github.com/<owner>/<repo>/blob/<40-char-sha>/Documentation/Mockups/saved-searches/panel.html)
```

## In this repository

The mockup folders here came from the older workflow, so each also holds a flattened
`<screen-name>.webp` beside its export, and its README names the feature document it fed
— now under `Documentation/Planning/DesignFeatures/`. Issue #398 shows one of those
images embedded in an issue by a raw URL pinned to a commit SHA. A new mockup follows the
sections above: its README names the feature issue, and its image is dragged into that
issue.
