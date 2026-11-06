# Design

> **Nothing has been released under this workflow yet.** The releaser replaces this
> file at the first release under this workflow.

This folder is the releaser's. At every release it writes the formal documentation
of the system as released, from three sources only:

- `INTENT.md` — what the system is for;
- the **Global Rules** issue — the rules every feature inherits, and the terms they
  use;
- the **feature issues** released so far — their bodies, their user stories and the
  images and diagrams attached to them.

Never edit anything here by hand: change the source and release. No other agent
reads this folder as a source — the planner, developer and QA work from the issues —
and nothing here is built from `Documentation/Diagrams/`, which is a scratchpad, or
`Documentation/Planning/`, which holds documents from before the issue-based workflow.

## Layout

```
Documentation/Design/
  design.md                                  epic level: what the system is for, a summary
                                             of the global rules, and every feature
                                             released under this workflow, with a link
                                             to its document
  Features/<FeatureName>/<FeatureName>.md    one feature as released: outcome, business
                                             rules, roles, user stories, events, storage
  Features/<FeatureName>/assets/             the screenshots and diagrams attached to the
                                             feature issue, linked or embedded above
```

`<FeatureName>` is the feature issue's title in PascalCase. Diagrams are Mermaid,
written by the releaser, or the images attached to the issue.

A repository moved onto this workflow from the older template kept its old design
documents under `Documentation/Planning/Design/`, `Documentation/Planning/DesignFeatures/`
and, for any other loose design document, `Documentation/Planning/<name>`.
They are not this folder's sources, with one exception: a `legacy` feature's page is
written from its issues and from the old documents those issues link, read at the
commit each link is pinned to — never from the working tree.
