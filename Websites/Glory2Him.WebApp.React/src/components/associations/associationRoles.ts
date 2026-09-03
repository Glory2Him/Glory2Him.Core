// §18.6: the global moderation tier every entity type's scoped pair sits above. Exported so a
// wrapper component's default is composed from one place rather than retyped by hand.
export const GlobalModerationRoles = 'Reviewers, Publishers, Administrators';

// §18.6: the global tier plus one entity type's own `%EntityType%-Reviewers` /
// `%EntityType%-Publishers` pair — the "simple two-deep tier" an association endpoint gets,
// since an endpoint is never ContentItem and so never carries the content-type-scoped third
// tier (§18.6 rule 5).
export const scopedModerationRoles = (entityType: string): string =>
    `${GlobalModerationRoles}, ${entityType}-Reviewers, ${entityType}-Publishers`;
