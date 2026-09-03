// §18.6: the global moderation tier every entity type's scoped pair sits above. Exported so a
// wrapper component's default is composed from one place rather than retyped by hand.
export const GlobalModerationRoles = 'Reviewers, Publishers, Administrators';

// §18.6: the global tier plus ONE entity type's own `%EntityType%-Reviewers` /
// `%EntityType%-Publishers` pair.
//
// It stops there because a PANEL KNOWS ONLY ITS OWN END. An association is authorised from both
// of its endpoints (§14.7, §18.6 "Composing an association's role check"), and the other end may
// well be a `ContentItem` carrying a content type — in which case the server also honours
// `ContentItem-Reviewers` and the content-type-scoped `ContentItem-%ContentType%-Publishers`
// tier. A tag panel cannot name that tier, because it does not know what it is hanging off: the
// same TagAssociationPanel renders against a post on postSingle.tsx and against a passage on
// bibleReference.tsx. So this composes the half the component can state truthfully, and a
// surface that knows its host passes the counterpart tier through the overridable
// `moderationRoles` prop.
//
// Composing narrowly is safe because it can only ever HIDE a control from somebody the server
// would have allowed — never show one it would refuse. Every gate here is render-only and the
// foundation services re-decide against the stored row regardless (§14.6).
export const scopedModerationRoles = (entityType: string): string =>
    `${GlobalModerationRoles}, ${entityType}-Reviewers, ${entityType}-Publishers`;
