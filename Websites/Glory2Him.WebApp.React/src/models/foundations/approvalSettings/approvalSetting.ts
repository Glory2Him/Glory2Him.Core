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

// ONLY ContentItem CARRIES A CONTENT TYPE, and ONLY Association A PERSONALITY (design §8.4).
// Every other entity type must leave them null, and the database enforces both with CHECK
// constraints rather than the service — so a bad pair comes back as a dependency failure with
// no field to hang it on. The form prevents it instead of explaining it afterwards.
export const allowsContentTypeScope = (entityType: EntityType | null): boolean =>
    entityType === EntityType.ContentItem;

export const allowsPersonalScope = (entityType: EntityType | null): boolean =>
    entityType === EntityType.Association;

// The heading a row is known by. Null is the global tier, which has no member to name.
export const entityTypeLabelOf = (entityType: EntityType | null): string =>
    entityType == null ? 'Every entity type' : entityTypeLabels[entityType] ?? 'Unknown';

// What narrows a row below its entity type, said the way the admin surface says it — or what
// tier the row IS, when nothing narrows it.
export const scopeLabelOf = (approvalSetting: {
    entityType: EntityType | null;
    contentType: ContentType | null;
    isPersonal: boolean | null;
}): string => {
    if (approvalSetting.entityType == null) {
        return 'The global default';
    }

    if (approvalSetting.isPersonal === true) {
        return 'Personal associations only';
    }

    if (approvalSetting.isPersonal === false) {
        return 'Editorial associations only';
    }

    return 'Default for the entity type';
};

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
    // Null means "every entity type" — the global default tier, the one row every entity-type
    // default narrows and the last stored row before the fail-closed system default.
    entityType: EntityType | null;

    // Null means "every content type of this entity type" — the entity-type default tier. A row
    // naming a content type beats it for that type. Legal on ContentItem only.
    contentType: ContentType | null;

    // Whether the row governs personal associations (the row's UserId is set — a user's own
    // reaction, §4.2) or editorial ones. Null means "every association". Legal on Association
    // only.
    isPersonal: boolean | null;

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

// POST api/ApprovalSettings — a new row as the client is entitled to compose it: the scope and
// the policy, with an id of its own, and NO audit fields. The server stamps those from the
// caller's identity before validating, and an empty string in a DateTimeOffset is refused in
// model binding before any service sees the row — with a body that names no message.
export type ApprovalSettingAddRequest = Omit<
    ApprovalSetting,
    'createdBy' | 'createdWhen' | 'updatedBy' | 'updatedWhen'>;

// The create form edits a whole ApprovalSetting (it is also the edit form), so the audit
// fields it carries are blanks. PICKED rather than spread-and-omitted: a field added to the
// entity later lands here as a compile error to decide about, not as a value quietly sent.
export const toApprovalSettingAddRequest = (
    approvalSetting: ApprovalSetting): ApprovalSettingAddRequest => ({
    id: approvalSetting.id,
    entityType: approvalSetting.entityType,
    contentType: approvalSetting.contentType,
    isPersonal: approvalSetting.isPersonal,
    requireApprovals: approvalSetting.requireApprovals,
    requiredNumberOfApprovals: approvalSetting.requiredNumberOfApprovals,
    autoApproveIfAllApprovalRequirementsMet: approvalSetting.autoApproveIfAllApprovalRequirementsMet,
    allowSelfApproval: approvalSetting.allowSelfApproval,
    blockOnReject: approvalSetting.blockOnReject,
    blockOnZeroApprovalScore: approvalSetting.blockOnZeroApprovalScore,
    requireReapprovalOnChange: approvalSetting.requireReapprovalOnChange,

    requireReviewCommentResolutionBeforeApprovals:
        approvalSetting.requireReviewCommentResolutionBeforeApprovals,

    doNotAllowBypassingSettings: approvalSetting.doNotAllowBypassingSettings,
    isDeleted: approvalSetting.isDeleted
});

// What a NEW row opens on: the HOUSE POLICY, the same nine values ApprovalSettingSeedData writes
// for every entity-type default. A content-type row an administrator adds narrows a seeded
// default, so it opens matching that default and the administrator changes only what they
// mean to — a form that opened looser than the row it overrides would make the ninth policy
// quietly weaker than the eight.
export const newApprovalSetting = (id: string): ApprovalSetting => ({
    id,
    entityType: EntityType.ContentItem,
    contentType: null,
    isPersonal: null,
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
