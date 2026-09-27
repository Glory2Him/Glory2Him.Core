# 1. Contribute

The contribution page: where a signed-in reader shares a story, a testimony, a quote or a verse
for review. The form is `ContentItemPanel`'s add face; the page owns everything around it — the
settings it is shaped by, the write, the thanks, the redirect and the validation read-back. Every
invitation to contribute on the site leads here.

- **Route:** `/posts/contribute` (`src/routes/publicPostRoutes.tsx` line 38 at 70dc72e7); the
  old address `/post/contribute` redirects here (line 65)
- **Source:** `src/pages/contribute.tsx`
- **Section:** user
- **Access:** no guard today — every reader reaches it; a holder of the global `ReadOnly` must not (rule 2.2)
- **Layout:** single column
- **Components:** [ContentItemPanel.md](../Components/ContentItemPanel.md) on its add face,
  [ContentItemPanel.Add.md](../Components/ContentItemPanel.Add.md), or, where the reader has no
  content type left to contribute, its restricted face,
  [ContentItemPanel.Restricted.md](../Components/ContentItemPanel.Restricted.md). Core-UI primitive: `Spinner`
  (`src/components/coreUI/spinner.tsx`), while the settings load.

Paths in this document are under `Websites/Glory2Him.WebApp.React/` unless they start with
`Documentation/`. Line numbers are at 70dc72e7.

## 2. Business Rules

**2.1 [Must]** `/posts/contribute` is the contribution page, and `/post/contribute`, the address it used to answer on, redirects to it, so links already in the wild still land. *(code: publicPostRoutes.tsx — the `posts/contribute` route and the `post/contribute` redirect)*

**2.2 [Must]** A reader holding the global `ReadOnly` never reaches the page: refusing them is the page's, not the add face's. *(user, 2026-09-27; `UI/Components/ContentItemPanel.Add.md §6`)* ≠ item 1

**2.3 [Must]** The form is `ContentItemPanel` handed a settings collection and no item, so it renders its add face. *(code: contribute.tsx — the `ContentItemPanel` element; test: contribute.test.tsx — "should render the panel in its add surface")*

**2.4 [Must]** The settings the page hands the form are the content type defaults open to general contribution, from `GET api/ContentItemSettings` (`UI/Components/ContentItemPanel.md §7`). The face orders them, and removes the types the reader is blocked from, itself (`UI/Components/ContentItemPanel.Add.md rules 2.3–2.7`). *(code: contribute.tsx — `useGetAvailableForContribution`)*

**2.5 [Must]** The page owns the write. On `onAdded` it posts the contribution, `POST api/ContentItems`, carrying only the members the API accepts from a caller (`UI/Components/ContentItemPanel.Add.md §7`). *(code: contribute.tsx — `addContentItemAsync`; test: contribute.test.tsx — "should post only the members the API accepts from a caller")*

**2.6 [Must]** A successful submission is thanked with the design's own words — *Thank you for your submission. It will be reviewed before publishing.* — and lands the contributor on their own posts, `/myposts`. The page never reads the response body, so a duplicate cannot be told from a new contribution (§DOM3.4.2 rule 6). *(code: contribute.tsx — `contributeThanksText`, `addContentItemAsync`; test: contribute.test.tsx — "should thank the contributor and land on their posts once it is submitted", "should not read the response body, so a duplicate cannot be told from a new one")*

**2.7 [Should]** A failed submission keeps the reader on the form: the page hands the API's field messages back as `validationIssues`, raises a notification carrying the API's reason, and clears the previous read-back before the next submission (`UI/Components/ContentItemPanel.md rules 2.33 and 2.34`). *(test: contribute.test.tsx — "should mark the form up from the API messages and say why, staying put", "should still notify when the failure names no field at all", "should clear a previous readback before it submits again")*

**2.8 [Should]** The form is frozen while the write is in flight. *(test: contribute.test.tsx — "should freeze the panel while the write is in flight")*

**2.9 [Must]** Cancel returns the reader to where they came from. Every link to this page passes its own address as the origin, and the origin survives the sign-in step; with no origin — a bookmark, a typed address — Cancel goes to the home page, `/`. *(user, 2026-09-27)* ≠ item 3

**2.10 [Must]** A signed-out reader is offered sign-in in place of the form, through the one reusable sign-in action the page supplies, and is returned to this page afterwards — but a reader whose sign-in state is still being read is not offered it (§UI20.6.6 rule 2; `UI/Components/ContentItemPanel.Add.md rule 3.2.1`). *(§UI20.6.6 rule 2)* ≠ item 2

**2.11 [Should]** While the settings load, a spinner stands in the form's place; if they cannot be read, the page says so rather than showing an empty form. *(code: contribute.tsx — the `isLoading` and `isError` branches)*

**2.12 [Could]** The page heads the form with *Share what He has done* and a short invitation that says submissions are reviewed before publishing. *(code: contribute.tsx — the heading block)*

**2.13 [Must]** A signed-in reader left with no content type to contribute — none on offer, or none their per-type read-only roles leave — is shown `ContentItemPanel`'s restricted face, `ContentItemRestrictedPanel`, in the form's place (`UI/Components/ContentItemPanel.md rule 2.43`). *(user, 2026-09-27)* ≠ `UI/Components/ContentItemPanel.Restricted.md §10 item 1`

**2.14 [Must]** A signed-in holder of `ContentItem-ReadOnly` reaches the page: the route asks for no role, and the page refuses no one at its door today. The role blocks every content type, so its holder is left with no tile, and the restricted face stands in the form's place (`UI/Components/ContentItemPanel.Add.md rule 3.2.4`). *(user, 2026-09-27; code: publicPostRoutes.tsx — the `posts/contribute` route; code: contribute.tsx — `Contribute`)* ≠ `UI/Components/ContentItemPanel.Restricted.md §10 item 1`

## 3. Layout

One centred column, nine twelfths wide at the `xl` breakpoint and full width below it, inside
`Root`'s header and footer (`src/components/root.tsx`). There is no shell sidebar, so nothing
stacks on a narrow screen.

```text
+------------------------------------------------------------+
| header (Root)                                              |
+------------------------------------------------------------+
|        heading, invitation                                 |
|        card: ContentItemPanel (add face)                   |
+------------------------------------------------------------+
| footer (Root)                                              |
+------------------------------------------------------------+
```

| Region | Width | Components, in order |
| --- | --- | --- |
| Main | `col-xl-9`, centred | The page's heading block; then, in a bordered card, `ContentItemPanel` — or `Spinner`, or the error alert, in its place |

## 4. Components and their hooks

### 4.1 ContentItemPanel — the add face

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `ariaLabel` | `Share what He has done` | Names the form's section. |
| `contentItemSettingCollection` | The defaults open to general contribution | Rule 2.4. |
| `validationIssues` | The API's field messages from the last failed submission, cleared before the next | Rule 2.7. |
| `isSubmitting` | Whether the write is in flight | Rule 2.8. |
| Everything else | Left at the panel's defaults: `isLoading` unset, since the page shows its own spinner until the settings arrive; no `submittedByDisplayName`, so an owned basis prefills the signed-in reader's own name (`UI/Components/ContentItemPanel.Add.md rule 2.16`); `approvalStatusDefault` unset, so *Submit as* opens on Submitted | Rule 2.11. The face's other properties are out of the page's reach (`UI/Components/ContentItemPanel.md §10 item 6`). |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onAdded` | *Submit for review* is pressed on a valid form | Posts the contribution; on success thanks the contributor and navigates to `/myposts`; on failure reads the messages back and notifies (rules 2.5–2.7) | ✅ Yes (`contribute.tsx`, lines 49-75 and 108) |
| `onCancelled` ≠ item 3 | *Cancel* is pressed | Returns the reader to the origin the page is told, or to `/` when it is told none (rule 2.9) | ❌ No — it navigates to `/` whatever the origin (`contribute.tsx`, line 109); item 3 |
| The login link — `loginHref` ≠ item 2 | A signed-out reader presses *Login to contribute* | Sends them to sign in through the one reusable sign-in action, returning them here (rule 2.10) | ❌ No — the panel does not forward `loginHref`, so the page cannot supply it, and the face composes its own sign-in route; item 2 |

### 4.2 ContentItemRestrictedPanel, through the panel

Shown in the add face's place when the reader has no content type left to contribute (rule 2.13).
It is not built yet (`UI/Components/ContentItemPanel.Restricted.md §10 item 1`).

**Properties the page sets:** none. Its wording keeps its default, *Sorry, we are not taking any
contributions at the moment.* (`UI/Components/ContentItemPanel.Restricted.md rule 2.3`).

**Hooks:** none. The face offers no action
(`UI/Components/ContentItemPanel.Restricted.md rule 2.4`).

## 5. Security and access

Every reader reaches the page today (rule 2.2 says who must not). What each sees is the add face's
decision (`UI/Components/ContentItemPanel.Add.md §3.4`, `UI/Components/ContentItemPanel.Add.md §5`),
or `ContentItemPanel`'s where it shows the restricted face instead
(`UI/Components/ContentItemPanel.md rule 2.43`); the page asks no role of its own. There is no
item yet, so no reader owns anything here.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The page — its heading and the form's frame | ✅ Yes | ✅ Yes | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| The persona also holds the global `ReadOnly` — **the page** ≠ item 1 | ➖ n/a | ❌ No | ➖ n/a | ❌ No | ❌ No | ❌ No |
| The persona also holds `ContentItem-ReadOnly` — **the page**, with the restricted face in the form's place ≠ `UI/Components/ContentItemPanel.Restricted.md §10 item 1` | ➖ n/a | ✅ Yes | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| The login link, in the form's place ≠ item 2 | ✅ Yes | ❌ No | ➖ n/a | ❌ No | ❌ No | ❌ No |
| The form, no read-only role covering every type on offer | ❌ No | ✅ Yes | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| No content type left to the reader — **the restricted face**, in the form's place ≠ `UI/Components/ContentItemPanel.Restricted.md §10 item 1` | ❌ No | ✅ Yes | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |

**The read-only roles.** The page is to refuse the global `ReadOnly` at its door (rule 2.2). Every
narrower block is the face's: a type the reader is blocked from by `ContentItem-{ContentType}-ReadOnly`
is removed from the picker (`UI/Components/ContentItemPanel.Add.md §5`), and a reader left with no
tile is shown the restricted face in the form's place (rule 2.13;
`UI/Components/ContentItemPanel.Restricted.md §5`). A holder of `ContentItem-ReadOnly` is let in,
is left with no tile, and is shown the restricted face (rule 2.14).

**The server decides.** The contribution is decided again under §SEC14.7 posture A rule 1 and
§APR9.7.1 rule 1 (`UI/Components/ContentItemPanel.Add.md §5`).

## 6. Open Questions and Gaps

1. (needs issue) **Page gap — `/posts/contribute`: a holder of the global read-only role reaches
   it.** Copied from `UI/Components/ContentItemPanel.Add.md §10 item 6`. The user ruled on
   2026-09-27 that a reader holding the global `ReadOnly` never reaches the contribution page, and
   that refusing them is the page's (rule 2.2). Today the route is public and asks for no role, and
   the page renders `ContentItemPanel` without reading the viewer's roles, so such a reader
   reaches the page and meets the face's fallback
   (`UI/Components/ContentItemPanel.Add.md rule 3.2.4`). Evidence:
   `src/routes/publicPostRoutes.tsx` — the `posts/contribute` route (line 38);
   `src/pages/contribute.tsx` — `Contribute`.
2. (needs issue) **The page cannot supply the sign-in action.** Rule 2.10 has the page supply the
   route the add face's *Login to contribute* link follows, through the one reusable sign-in
   action (§UI20.6.6 rule 2; `UI/Pages/Home.md §6 item 3`). `ContentItemPanel` does not forward
   `loginHref` to the add face (`UI/Components/ContentItemPanel.md §10 item 6`), so the page
   cannot pass it, and the face composes `/Account/Login?returnUrl=<path>` itself
   (`UI/Components/ContentItemPanel.Add.md §10 item 4`). The page's half is to pass the reusable
   action once it can. The face also reads `isAuthenticated` alone (`contentItemFormPanel.tsx` —
   `useAuth`, line 358), which is false while the reader's sign-in state is still being read, so a
   signed-in reader arriving on a full page load can be shown the login link until it is; that is
   the face's own gap, `UI/Components/ContentItemPanel.Add.md §10 item 12`.
3. (needs issue) **Page gap — Cancel leads to `/`, not back to where the reader came from.** Rule
   2.9 (user ruling 2026-09-27). Cancel navigates to `/` whatever the origin (`contribute.tsx` —
   `onCancelled`, line 109; test: `contribute.test.tsx` — "should leave the page when the
   contribution is abandoned"). Three pages send a reader here carrying where they came from as
   `from` in router state — `/`, `/posts/{id}` and `/myposts/{id}` (`UI/Components/SharingPanel.md
   §4.3`) — and this page reads none of it; a test on `/posts/{id}` says the reader is carried "to
   the contribution surface and back here" (`postDetail.test.tsx`). The page's half is to return
   the reader to the origin it is told, and to `/` when it is told none (rule 2.9). The links that
   tell it none today are their pages' gaps: `/posts` (`UI/Pages/Posts.md §6 item 12`) and
   `/myposts` (`UI/Pages/MyPosts.md §6 item 11`).
4. **Note — `ContentItem-ReadOnly` at the contribution page's door, ruled.** This item asked
   whether the page should refuse a holder of `ContentItem-ReadOnly` at its door, as it is to
   refuse the global `ReadOnly`, or let them in to be shown the restricted face. The user ruled on
   2026-09-27: such a holder is let in and shown the restricted face; only the global `ReadOnly` is
   kept off the page (rules 2.2 and 2.14). It was the page's half of
   `UI/Components/ContentItemPanel.Add.md §10 item 7`.
5. **Note — where Cancel leads when the page is not told where the reader came from, ruled.** This
   item asked whether the two invitations that link here with no router state — `/posts` and
   `/myposts` — should carry their origin as the other invitations do, and where Cancel leads a
   reader whose origin the page cannot know: one who arrives by sign-in, by a bookmark or by a
   typed address. The user ruled on 2026-09-27: every link to this page passes its own address as
   the origin, `/posts` and `/myposts` included; the origin survives the sign-in step; and with no
   origin — a bookmark, a typed address — Cancel goes to the home page. Rule 2.9 says so here,
   `UI/Pages/Posts.md rule 2.13` and `UI/Pages/MyPosts.md rule 2.20` for the two links; the gaps
   are item 3 and the items it names.
