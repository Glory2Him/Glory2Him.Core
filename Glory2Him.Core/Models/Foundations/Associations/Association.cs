// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Models.Foundations.Associations
{
    /// <summary>
    /// A link between two entities. Both endpoints are generic and symmetric (design §4):
    /// neither is hard-wired to a <c>ContentItem</c>, so a Bible reference may be associated
    /// to another Bible reference — the *Related Passages* panel — exactly as a tag is
    /// associated to a story.
    ///
    /// <para><b>There is no <c>Kind</c> and no <c>SourceEndpoint</c>.</b> The
    /// <c>(EntityType, ContentType)</c> pair on each endpoint already carries the meaning,
    /// and direction falls out of the asymmetry: a <c>Series</c> paired with a <c>Story</c>
    /// is always container-to-member, because the reverse is not a thing that exists. A
    /// separate discriminator would be a second source of truth for something the endpoints
    /// already say.</para>
    ///
    /// <para><b>Endpoints are stored in canonical order</b>, A before B, computed on add
    /// (design §4.4). One row therefore serves both endpoints' lists, and "is X linked to Y"
    /// is one lookup rather than two.</para>
    /// </summary>
    public class Association : IKey, IAudit, IApproval, ISortOrder, IConfidence
    {
        /// <summary>
        /// Primary key identifier for the association.
        /// </summary>
        public Guid Id { get; set; }

        // ── Endpoint A — the canonical low side (design §4.4) ─────────────────────────

        /// <summary>
        /// Type of the endpoint. Create-only: reclassifying an association is forbidden
        /// (design §4.5 rule 4), so this is pinned against storage on every modify.
        /// </summary>
        public EntityType EntityAType { get; set; }

        /// <summary>
        /// Identifier of the specific row this endpoint points at — the version, for a
        /// versioned entity type. Create-only.
        /// </summary>
        public Guid EntityAKeyId { get; set; }

        /// <summary>
        /// Identifier of the version group this endpoint belongs to. Equal to
        /// <see cref="EntityAKeyId"/> when the entity type is not versioned, so every
        /// endpoint has a group id and the same rules apply to both kinds. Create-only —
        /// and because it never changes, a scope toggle can never force A and B to swap
        /// columns.
        /// </summary>
        public Guid EntityAGroupId { get; set; }

        /// <summary>
        /// Whether the association follows this endpoint across versions. <b>Derived, never
        /// accepted from a caller</b> — a non-versioned entity type resolves to
        /// <see cref="Scope.ThisVersionOnly"/>, a versioned one defaults to
        /// <see cref="Scope.AllVersions"/> (design §7.5.1). This is the only endpoint field
        /// that may change after creation, through the set-scope operation.
        /// </summary>
        public Scope EntityAScope { get; set; }

        /// <summary>
        /// The identifier this endpoint effectively resolves to: the group id under
        /// <see cref="Scope.AllVersions"/>, the row id under
        /// <see cref="Scope.ThisVersionOnly"/>. Computed and persisted by the database, never
        /// written by application code — which is why it is the read predicate every panel
        /// seeks on, and why two rows that mean the same thing collapse to one key.
        /// </summary>
        public Guid EntityAEffectiveId { get; private set; }

        /// <summary>
        /// Classification of the endpoint, denormalised so an association's authorization
        /// can be composed from the row alone (design §18.6). Null unless
        /// <see cref="EntityAType"/> is <c>ContentItem</c> — no other entity type has a
        /// sub-classification. Derived from the resolved endpoint, never caller-supplied: it
        /// is an authorization input, so a caller who could set it could claim authority
        /// over a content type they hold no role for.
        /// </summary>
        public ContentType? EntityAContentType { get; set; }

        // ── Endpoint B — the canonical high side, an identical set ───────────────────

        /// <inheritdoc cref="EntityAType"/>
        public EntityType EntityBType { get; set; }

        /// <inheritdoc cref="EntityAKeyId"/>
        public Guid EntityBKeyId { get; set; }

        /// <inheritdoc cref="EntityAGroupId"/>
        public Guid EntityBGroupId { get; set; }

        /// <inheritdoc cref="EntityAScope"/>
        public Scope EntityBScope { get; set; }

        /// <inheritdoc cref="EntityAEffectiveId"/>
        public Guid EntityBEffectiveId { get; private set; }

        /// <inheritdoc cref="EntityAContentType"/>
        public ContentType? EntityBContentType { get; set; }

        // ── Everything else ───────────────────────────────────────────────────────────

        /// <summary>
        /// The user this association belongs to, set only where the association is personal
        /// rather than editorial — today a <c>Reaction</c> endpoint. Null means the
        /// association is editorial, and because SQL Server treats <c>NULL = NULL</c> as a
        /// duplicate in a unique index, that null is what makes "exactly one of these
        /// globally" and "exactly one per user" the same index.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Position within the containing endpoint's list. Null where neither endpoint is a
        /// container — canonical ordering means one row serves both lists, so a bare integer
        /// would have no owner (design §11.7).
        /// </summary>
        public int? SortOrder { get; set; }

        /// <inheritdoc/>
        public decimal? ConfidenceScore { get; set; }

        /// <inheritdoc/>
        public string? ConfidenceReason { get; set; }

        /// <inheritdoc/>
        public Guid? SourceBatchId { get; set; }

        /// <inheritdoc/>
        public string? ModelVersion { get; set; }

        /// <summary>
        /// User identifier for who created the association.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the association was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the association.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the association was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }

        /// <summary>
        /// User identifier for who deleted the association.
        /// </summary>
        public string? DeletedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the association was deleted.
        /// </summary>
        public DateTimeOffset? DeletedWhen { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the association is deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Reason for deletion, if applicable.
        /// </summary>
        public string? DeletionReason { get; set; }

        /// <summary>
        /// The date and time when the association was published.
        /// This is nullable to allow for drafts that have not yet been published.
        /// </summary>
        public DateTimeOffset? PublishDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the association is published.
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// A denormalized field to indicate if the association has been approved.
        /// This is used to optimize queries for approved associations without
        /// needing to join with the approvals table.
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    }
}
