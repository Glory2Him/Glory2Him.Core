import { ContentType } from '../contentItemSettings/contentType';

// Mirrors Glory2Him.Core.Models.Enums.EntityType, including its numbering: the wire carries the
// NUMBER (the host registers no JsonStringEnumConverter), and an approval setting's scope is
// keyed on it, so these values are a contract rather than a convenience.
export enum EntityType {
    ContentItem = 0,
    Tag = 1,
    Reaction = 2,
    BibleReference = 3,
    Comment = 4,
    Link = 5,
    Attachment = 6,
    Association = 7,
}

// Declaration order, for the pickers. Object.keys over a numeric enum yields the reverse mapping
// alongside the members, so the list is stated rather than derived.
export const entityTypeMembers: ReadonlyArray<EntityType> = [
    EntityType.ContentItem,
    EntityType.Tag,
    EntityType.Reaction,
    EntityType.BibleReference,
    EntityType.Comment,
    EntityType.Link,
    EntityType.Attachment,
    EntityType.Association,
];

export const entityTypeLabels: Readonly<Record<EntityType, string>> = {
    [EntityType.ContentItem]: 'Content item',
    [EntityType.Tag]: 'Tag',
    [EntityType.Reaction]: 'Reaction',
    [EntityType.BibleReference]: 'Bible reference',
    [EntityType.Comment]: 'Comment',
    [EntityType.Link]: 'Link',
    [EntityType.Attachment]: 'Attachment',
    [EntityType.Association]: 'Association',
};

// ONLY ContentItem CARRIES A CONTENT TYPE (design §8.4). Every other entity type must leave it
// null, and the database enforces that with a CHECK constraint rather than the service — so a
// bad pair comes back as a dependency failure with no field to hang it on. The form prevents it
// instead of explaining it afterwards.
export const allowsContentTypeScope = (entityType: EntityType): boolean =>
    entityType === EntityType.ContentItem;

// Wire shape of api/ApprovalSettings, camelCased by the host's default System.Text.Json policy.
//
// THE WHOLE ROW GOES BACK ON A SAVE. PUT binds an ApprovalSetting and validates Id, CreatedBy and
// CreatedWhen against storage BEFORE reading it, so an edit is the fetched row with the policy
// fields changed — never a fresh object. UpdatedBy and UpdatedWhen are server-stamped and
// whatever a caller sends for them is ignored.
export type ApprovalSetting = {
    // Supplied by the CALLER on create: the service never generates one, and refuses an empty
    // Guid. There is no POST that mints an id for you.
    id: string;

    // ── Scope (§8.4) ──────────────────────────────────────────────────────────
    entityType: EntityType;

    // Null means "every content type of this entity type" — the entity-type default tier. A row
    // naming a content type beats it for that type.
    contentType: ContentType | null;

    // ── Policy ────────────────────────────────────────────────────────────────
    requireApprovals: boolean;
    requiredNumberOfApprovals: number;
    autoApproveIfAllApprovalRequirementsMet: boolean;
    allowSelfApproval: boolean;
    blockOnReject: boolean;
    blockOnZeroApprovalScore: boolean;
    requireReapprovalOnChange: boolean;
    requireReviewCommentResolutionBeforeApprovals: boolean;
    doNotAllowBypassingSettings: boolean;

    // ── Audit ─────────────────────────────────────────────────────────────────
    // Carried rather than displayed only: the save round-trips CreatedBy and CreatedWhen, which
    // the foundation compares against storage before it will accept the write.
    createdBy: string;
    createdWhen: string;
    updatedBy: string;
    updatedWhen: string;
    isDeleted: boolean;
};

// What a NEW row opens on: the HOUSE POLICY, the same nine values ApprovalSettingSeedData writes
// for every entity-type default. A content-type row an administrator adds narrows a seeded
// default, so it opens matching that default and the administrator changes only what they
// mean to — a form that opened looser than the row it overrides would make the ninth policy
// quietly weaker than the eight.
export const newApprovalSetting = (id: string): ApprovalSetting => ({
    id,
    entityType: EntityType.ContentItem,
    contentType: null,
    requireApprovals: true,
    requiredNumberOfApprovals: 2,
    autoApproveIfAllApprovalRequirementsMet: false,
    allowSelfApproval: false,
    blockOnReject: true,
    blockOnZeroApprovalScore: true,
    requireReapprovalOnChange: true,
    requireReviewCommentResolutionBeforeApprovals: true,
    doNotAllowBypassingSettings: false,
    createdBy: '',
    createdWhen: '',
    updatedBy: '',
    updatedWhen: '',
    isDeleted: false
});
