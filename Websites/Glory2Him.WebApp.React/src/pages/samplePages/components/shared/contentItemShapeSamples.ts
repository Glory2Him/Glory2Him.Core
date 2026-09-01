// THE SHAPES the ContentItemPanel family is fed with, written out once and shown on every
// page that takes them — annotated literals beat a prose description of a projection.

// ONE ELEMENT of contentItemCollection — and the very same shape `contentItem` takes on a
// detail surface: the family has ONE projection, so a list element hands straight to a
// detail view (and seeds its editor) with no further read.
export const contentItemElementShape = `
// ContentItemSearchItem — one SELF-CONTAINED element. Built by toContentItemSearchItem
// from the wire entity plus whatever setting rows the page holds; the §6.4 winner rides
// ON the element, so the panels consult no collection and updating one item is one
// element swapped by the consumer — never a refetch.
const element: ContentItemSearchItem = {
    id: '2f9e6a10-7c41-4d55-9b8e-000000000102',

    contentType: ContentType.Story,        // the NUMBER; chips, roles and $filter use the
                                           // enum member NAME composed from it

    contentItemSetting: { /* ContentItemSetting — THE WINNING ROW for this item:
                             its own override when one exists, its type default
                             behind it (§6.4). See the setting shape below. */ },

    title: 'He carried me through',        // absent when the type's setting has no title
    author: 'Grace Abara',                 // the author of the WORDS, not the submitter
    content: 'When the diagnosis came…',   // WHOLE — surfaces cut it at truncateAt

    imageUrl: '/assets/images/blog/4by3/01.jpg',  // supplied by the consumer; today a
                                                  // per-type placeholder (no image column)

    shareabilityBasis: ShareabilityBasis.OwnedPermissionGranted,
    sharePermission: 'By email, 12 Jan 2026',     // seeds the editor; a card never shows it

    publishedDate: new Date('2026-07-15'),        // PublishDate, falling back to CreatedWhen

    submittedById: 'a7727f95-b509-45e6-…',        // CreatedBy — the [OWNER] gate compares
                                                  // this against the signed-in account id
    submittedByName: 'Grace Abara',               // optional; the public resolver is gated
                                                  // (§16.7.4), so feeds leave it unset

    approvalStatus: ApprovalStatus.Draft,         // the pill and the ribbon wear it when
                                                  // the surface opts in

    // Association-shaped reads, each optional until #318 gives them an exposer — the
    // templates LEAVE OUT what they were not given rather than rendering a zero:
    tags: ['providence', 'healing'],
    bibleReferences: ['Deuteronomy 31:6'],
    reactionSummary: [{ label: 'Amen', glyph: '🙏', count: 12 }],
    viewerReactionLabel: 'Amen',                  // the signed-in reader's own reaction
    commentCount: 4
};
`;

// ONE ROW of contentItemSettingCollection / categorySettingCollection.
export const contentItemSettingShape = `
// ContentItemSetting — one row. contentItemId decides WHAT the row is:
//   null      → the TYPE DEFAULT: a picker tile on the add face, a Category choice in
//               the search bar, and the fallback behind every element's embedded winner
//   a guid    → an OVERRIDE for that one item, beating the default outright (§6.4);
//               never a tile, never a category — it belongs to one existing item
// A soft-deleted row (isDeleted) is out of resolution entirely (§6.6).
const setting: ContentItemSetting = {
    id: '9e1f3c77-0d2a-4b58-9f11-…',
    contentType: ContentType.Story,
    contentItemId: null,

    contentTypeName: 'Story',              // editable display name — the chip TEXT; the
    contentTypeDescription: 'A story.',    // colour keys off the enum member name instead
    contentTypeIconCssClass: 'bi-book',
    sortOrder: 1,                          // picker/category order; ties keep arrival order

    isAvailableAsGeneralUserContribution: true,  // may it be CONTRIBUTED (a tile) — the
                                                 // search Category box ignores this

    // The field shaping the forms obey:
    hasTitle: true,
    hasAuthor: true,
    maxTitleLength: 120,                   // null = no limit; the form caps the input and
    maxAuthorLength: null,                 // refuses a stored value over a lowered ceiling
    maxContentLength: 4000,

    // The §6.5 facet pairs — Allowed says the type ACCEPTS them, Show says this surface
    // renders them (and the section switches AND with these):
    tagsAllowed: true, showTags: true,
    reactionsAllowed: true, showReactions: true, limitReactionsToLoveOnly: false,
    commentsAllowed: true, showComments: true,
    bibleReferenceAllowed: true, showBibleReferences: true,
    linksAllowed: false, showLinks: false,
    attachmentsAllowed: false, showAttachments: false,

    // The audit tail every row carries:
    createdBy: 'seed', createdWhen: '2026-01-01T00:00:00+00:00',
    updatedBy: 'seed', updatedWhen: '2026-01-01T00:00:00+00:00',
    deletedBy: null, deletedWhen: null, isDeleted: false, deletionReason: null
};
`;

// The EMITTED shape — what onAdded/onModified hand the page, and what
// ContentItemEditPanel takes directly.
export const contentItemFormItemShape = `
// ContentItemFormItem — the form register of the same facts. ContentItemPanel derives it
// from the element when Edit is taken (submittedById becomes createdBy, the audit name);
// the add face constructs it from what was typed, stamped with the winning setting.
const formItem: ContentItemFormItem = {
    id: '2f9e6a10-7c41-4d55-9b8e-000000000102',   // absent on an add — no row exists yet
    contentType: ContentType.Story,               // create-only (§12.4.1 rule 7a)
    contentItemSetting: { /* the winner it was shaped with — self-contained, like
                             everything that carries this family's shapes */ },

    title: 'He carried me through',
    author: 'Grace Abara',
    content: 'When the diagnosis came…',

    shareabilityBasis: ShareabilityBasis.OwnedPermissionGranted,
    sharePermission: 'By email, 12 Jan 2026',     // mandatory under a permission basis —
                                                  // the form's first ruled exception

    createdBy: 'a7727f95-b509-45e6-…',            // the [OWNER] gate's other half
    approvalStatus: ApprovalStatus.Draft          // the owner edits at any status; the
                                                  // publisher tier only while live
};
`;
