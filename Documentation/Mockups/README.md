# Mockups

Claude Design exports, and any other visual mockup, on their way into a feature
document. `DEVELOPERS.md` §5 describes the full flow.

## Layout

```
Documentation/Mockups/<feature-slug>/
  <screen-name>.html     the Claude Design export, self-contained
  <screen-name>.webp     a flattened image of the same screen
  README.md              what it shows, the issue, and the feature document it produced
```

Keep the HTML when the interaction matters — hover states, spacing, the real DOM.
Keep the image either way: it is what gets embedded in the GitHub issue, because
a multi-megabyte bundled HTML file does not render in an issue body.

## A mockup is never the design

Once the planner has written the feature document, it is
authoritative and the mockup is history. Record it in the folder README the
moment it happens:

```markdown
# Saved searches panel
Source: Claude Design export, 2026-09-11. Issue: #511.
Superseded by `Documentation/DesignFeatures/SavedSearches.md` — its business rules
win wherever the two disagree.
```

A mockup left without that line is a second source of truth waiting to
contradict the design.

## Referencing an image from an issue

Pin the raw URL to the full 40-character commit SHA, never a branch, so the
picture in the issue cannot change under it later:

```markdown
![Panel](https://raw.githubusercontent.com/Glory2Him/Glory2Him.Core/<40-char-sha>/Documentation/Mockups/saved-searches/panel.webp)
```

Issue #398 is the worked precedent.
