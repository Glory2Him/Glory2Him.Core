# The compliance report

One self-contained HTML file. The only script is Mermaid, for the layer graphs; the
styling is inline CSS, so the page still reads if the CDN is blocked — the graphs then
show as their Mermaid text, which is still legible.

## Scaffold

```html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Standard compliance — {{repository}}</title>
  <script type="module">
    import mermaid from "https://cdn.jsdelivr.net/npm/mermaid@11.4.1/dist/mermaid.esm.min.mjs";
    mermaid.initialize({ startOnLoad: true, theme: "neutral" });
  </script>
  <style>
    :root { --ink: #1f2933; --muted: #616e7c; --line: #d9e2ec; --paper: #ffffff;
            --breach: #c81e1e; --likely: #b7791f; --drift: #616e7c; --ok: #2f855a; }
    body { margin: 0; background: #f5f7fa; color: var(--ink);
           font: 15px/1.5 system-ui, -apple-system, "Segoe UI", sans-serif; }
    main { max-width: 1040px; margin: 0 auto; padding: 32px 16px 64px; }
    article { background: var(--paper); border: 1px solid var(--line); border-radius: 8px;
              padding: 20px; margin: 20px 0; }
    .badges span { display: inline-block; font-size: 12px; font-weight: 600; padding: 2px 8px;
                   border-radius: 999px; margin-right: 6px; border: 1px solid currentColor; }
    .clear { color: var(--breach); } .likely { color: var(--likely); } .drift { color: var(--drift); }
    .files { font: 13px/1.4 ui-monospace, Consolas, monospace; color: var(--muted); }
    .pair { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    @media (max-width: 720px) { .pair { grid-template-columns: 1fr; } }
    .pair > div { border: 1px solid var(--line); border-radius: 6px; padding: 8px; overflow-x: auto; }
    .callout { background: #fffbea; border-left: 4px solid var(--likely); padding: 8px 12px; }
    table { border-collapse: collapse; width: 100%; } td, th { border-bottom: 1px solid var(--line);
            padding: 6px 8px; text-align: left; vertical-align: top; }
  </style>
</head>
<body>
<main>
  <header>…</header>
  <section id="findings">…</section>
  <section id="deviations">…</section>
  <section id="script">…</section>
  <section id="top">…</section>
</main>
</body>
</html>
```

## Header

The repository, the date, the commit surveyed (`git rev-parse --short HEAD`), the scope
and how it was chosen — named by the user, or the hot spots from the history. A count of
findings by strength and by tier. A collapsed `<details>` holding the whole layer graph
the script drew. No introduction beyond that.

## A finding

One `<article>` each, most severe first: clear breaches before likely ones, security and
identity first among equals.

- **Title** — names the remedy, in the Terms' words: *"Split enrolment under a
  coordination service"*, not *"Fix EnrolmentOrchestrationService"*.
- **Badges** — the strength (`Clear breach`, `Likely breach`, `Drift`), the tier, and the
  rule by name: `Florance`, `same kind`, `utility brokers`, `query-shaping`, `§ARC3`,
  `the-standard-brokers`, `identity`.
- **Files** — monospaced, `file:line` where it helps.
- **Before and after** — two Mermaid graphs side by side. Before: the services involved,
  cut from the script's graph, the breaching edges in red. After: the shape the remedy
  gives, the new service or moved edge marked. Same layer order, top to bottom, in both,
  so the eye compares like with like:

  ```html
  <div class="pair">
    <div><pre class="mermaid">
  graph TD
    EnrolmentOrchestrationService --> StudentService
    EnrolmentOrchestrationService --> GroupService
    EnrolmentOrchestrationService --> LibraryService
    EnrolmentOrchestrationService --> CardService
    classDef breach stroke:#c81e1e,stroke-width:2px;
    class EnrolmentOrchestrationService breach
    </pre></div>
    <div><pre class="mermaid">
  graph TD
    EnrolmentCoordinationService --> StudentEnrolmentOrchestrationService
    EnrolmentCoordinationService --> LibraryEnrolmentOrchestrationService
    StudentEnrolmentOrchestrationService --> StudentService
    StudentEnrolmentOrchestrationService --> GroupService
    LibraryEnrolmentOrchestrationService --> LibraryService
    LibraryEnrolmentOrchestrationService --> CardService
    </pre></div>
  </div>
  ```

- **The breach** — one sentence, and the rule it breaks.
- **The remedy** — one sentence, and the other remedies if there is a real choice.
- **Tests** — which layers gain tests, which tests move with their code. Never "fewer
  tests".
- **Callout**, if any — *"contradicts decision 0004, worth reopening because …"*.

If a card needs a paragraph to be understood, redraw the graph instead.

## Deviations

A table of every recorded deviation in scope: the service, the rule, and where it is
recorded — the user story issue, or the decision record that carries a `**Deviation:**`
line for a service designed before the move to issues. These are approved, not findings.

## The script

Violations the script reported that the code does not bear out, each with the reason it
was fooled. These are improvements to `scripts/architecture-graph.py`, not findings.

## Top recommendation

One card: the finding to take first and why, in a sentence — usually the clearest breach
in the hottest code at the lowest tier that removes it.
