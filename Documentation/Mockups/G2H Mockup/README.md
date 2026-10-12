# Admin Post Detail and Admin Association Detail

This README covers two screens of this folder and no other.

- `Admin Post Detail.dc.html` — the admin post page, `/Admin/Posts/{contentItemId}`: the post, with
  its tag and Bible reference chips on its card, each showing its association's status and leading
  to that association's page; its review comments; its reviews and *Review Outcome*; and its
  content settings.
- `Admin Association Detail.dc.html` — the admin page for one tag or Bible reference on a post,
  `/Admin/Posts/{contentItemId}/Associations/{associationId}`: the post, the association (who
  suggested it, and when), its confidence score and reason, and the association's own review
  comments, reviews and *Review Outcome*, with links back to the post and to the queue.

Source: Claude Design exports. `Admin Post Detail.dc.html` came in with the 2026-09-13 sync
(aa6fa441) and was revised in 8868c321, which added `Admin Association Detail.dc.html`. Issue:
#698, whose mockup step these two screens fulfil.

Superseded by `Documentation/DesignFeatures/PostModeration.md`,
`Documentation/Design/UI/Pages/ContentItemModerationDetailPage.md` and
`Documentation/Design/UI/Pages/AssociationModerationDetailPage.md`, written under #705. Their
rules win wherever these screens and the design disagree.
