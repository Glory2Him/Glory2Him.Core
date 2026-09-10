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

using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        private static void AddApprovalSettingConfigurations(EntityTypeBuilder<ApprovalSetting> model)
        {
            model.ToTable(
                "ApprovalSettings",
                tableBuilder =>
                {
                    // design §8.4: ContentType may be populated only when EntityType = ContentItem
                    //
                    // THE `IS NOT NULL` TERM IS LOAD-BEARING, and was not needed while EntityType
                    // was NOT NULL. A CHECK constraint passes on UNKNOWN, and `NULL = N'ContentItem'`
                    // is UNKNOWN — so the moment the global tier made EntityType nullable, the
                    // right-hand side stopped refusing anything and a row could be stored naming a
                    // content type under no entity type at all. Nothing downstream would resolve
                    // it, and it would hold the single global-default slot while doing so.
                    tableBuilder.HasCheckConstraint(
                        name: "CK_ApprovalSetting_ContentTypeRequiresContentItem",
                        sql:
                            $"({nameof(ApprovalSetting.ContentType)} IS NULL OR " +
                            $"({nameof(ApprovalSetting.EntityType)} IS NOT NULL AND " +
                            $"{nameof(ApprovalSetting.EntityType)} = N'{nameof(EntityType.ContentItem)}'))");

                    // design §8.4: IsPersonal may be populated only when EntityType = Association.
                    // The same shape as the constraint above, for the same reason — the
                    // personality of a row is a property of an association's UserId (§4.2) and
                    // means nothing on any other entity type — and null-safe for the same reason.
                    tableBuilder.HasCheckConstraint(
                        name: "CK_ApprovalSetting_IsPersonalRequiresAssociation",
                        sql:
                            $"({nameof(ApprovalSetting.IsPersonal)} IS NULL OR " +
                            $"({nameof(ApprovalSetting.EntityType)} IS NOT NULL AND " +
                            $"{nameof(ApprovalSetting.EntityType)} = N'{nameof(EntityType.Association)}'))");

                    // design §8.6.2: the vote is the CHILD switch, and this is what makes that
                    // more than a comment. A row offering the vote while the reviewer itself is
                    // off would describe Berean casting a verdict nobody asked it to attend —
                    // the same shape the two scope checks above refuse for ContentType/IsPersonal,
                    // applied to a feature pair instead of a scope pair.
                    tableBuilder.HasCheckConstraint(
                        name: "CK_ApprovalSetting_AIVoteRequiresAIReviewer",
                        sql:
                            $"({nameof(ApprovalSetting.IsAIAllowedToVote)} = 0 OR " +
                            $"{nameof(ApprovalSetting.IsAIReviewerOffered)} = 1)");

                    // design §8.6.2: both thresholds are values on ConfidenceScore's own
                    // 0.00–10.00 scale (§13.5), and HasPrecision does not enforce a scale —
                    // decimal(4,2) admits -99.99 through 99.99. Until these existed the range
                    // was enforced only by the admin page's number inputs, so any caller that
                    // was not that page — a script, a stale client — stored 50.00 or -3.00 and
                    // kept it. Same SQL as CK_Association_ConfidenceScoreRange because it is
                    // the same scale; not null-safe like that one only because these two
                    // columns are non-nullable (a threshold with no value compares against
                    // nothing).
                    //
                    // One constraint per column rather than one combined check: a check-
                    // constraint violation names no field to the caller, so the constraint
                    // name in the SQL error is the only diagnostic a raw-SQL writer gets.
                    tableBuilder.HasCheckConstraint(
                        name: "CK_ApprovalSetting_AIRejectionThresholdRange",
                        sql:
                            $"({nameof(ApprovalSetting.AIApprovalConfidenceRejectionThreshold)} " +
                            $"BETWEEN 0 AND 10)");

                    tableBuilder.HasCheckConstraint(
                        name: "CK_ApprovalSetting_AIApprovalThresholdRange",
                        sql:
                            $"({nameof(ApprovalSetting.AIApprovalConfidenceApprovalThreshold)} " +
                            $"BETWEEN 0 AND 10)");

                    // design §8.6.2: the approval threshold is the higher of the two, and until
                    // this constraint existed nothing made that true. Inverted — rejection 8.00
                    // with approval 3.00 — a score of 5 satisfies both the reject-below test of
                    // rule 1 and the approve-above test of rule 2 at once, so both fire and the
                    // design defines no behaviour for that state.
                    //
                    // `<=` rather than `<`, deliberately. Rules 1 and 2 are STRICT comparisons
                    // on either side of the pair, so an equal pair only closes the middle band
                    // (rule 3) and leaves the two verdicts disjoint — nothing inverts. It is
                    // also the pair ApprovalPolicyDefaults.SystemDefaultFor ships as its
                    // fail-closed fallback (0.00/0.00, "casts no vote to threshold in the first
                    // place"), which a strict rule would make unstorable.
                    tableBuilder.HasCheckConstraint(
                        name: "CK_ApprovalSetting_AIThresholdOrder",
                        sql:
                            $"({nameof(ApprovalSetting.AIApprovalConfidenceRejectionThreshold)} <= " +
                            $"{nameof(ApprovalSetting.AIApprovalConfidenceApprovalThreshold)})");
                });

            model.HasKey(approvalSetting => approvalSetting.Id);

            model.Property(approvalSetting => approvalSetting.Id)
                 .IsRequired();

            // NULLABLE, and that is the global tier (design §8.4): a row with no entity type
            // is the one every entity-type default narrows.
            model.Property(approvalSetting => approvalSetting.EntityType)
                 .HasConversion<string>()
                 .HasMaxLength(64)
                 .IsRequired(false);

            model.Property(approvalSetting => approvalSetting.ContentType)
                 .HasConversion<string>()
                 .HasMaxLength(32)
                 .IsUnicode(true)
                 .IsRequired(false);

            model.Property(approvalSetting => approvalSetting.IsPersonal)
                 .IsRequired(false);

            model.Property(approvalSetting => approvalSetting.RequireApprovals)
                 .IsRequired()
                 .HasDefaultValue(true);

            model.Property(approvalSetting => approvalSetting.RequiredNumberOfApprovals)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.AllowSelfApproval)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.BlockOnReject)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.BlockOnZeroApprovalScore)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.RequireReapprovalOnChange)
                 .IsRequired()
                 .HasDefaultValue(true);

            model.Property(approvalSetting => approvalSetting.AutoApproveIfAllApprovalRequirementsMet)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.RequireReviewCommentResolutionBeforeApprovals)
                 .IsRequired()
                 .HasDefaultValue(true);

            model.Property(approvalSetting => approvalSetting.DoNotAllowBypassingSettings)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.IsAIReviewerOffered)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.IsAIAllowedToVote)
                 .IsRequired()
                 .HasDefaultValue(false);

            // decimal(4,2), the same precision as Association.ConfidenceScore (design §13.5) —
            // both thresholds are values on that scale, not a narrower one. Non-nullable, unlike
            // ConfidenceScore itself: a threshold with no value would compare against nothing.
            //
            // PRECISION IS NOT THE RANGE, and these two lines are not what keeps a threshold on
            // the scale: decimal(4,2) admits -99.99 through 99.99. The 0.00–10.00 bound and the
            // rejection ≤ approval ordering are the three check constraints in the ToTable block
            // above, with the matching service rules in front of them.
            model.Property(approvalSetting => approvalSetting.AIApprovalConfidenceRejectionThreshold)
                 .HasPrecision(4, 2)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.AIApprovalConfidenceApprovalThreshold)
                 .HasPrecision(4, 2)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.CreatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.CreatedWhen)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.UpdatedBy)
                 .HasMaxLength(255)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.UpdatedWhen)
                 .IsRequired();

            model.Property(approvalSetting => approvalSetting.IsDeleted)
                 .IsRequired()
                 .HasDefaultValue(false);

            model.Property(approvalSetting => approvalSetting.DeletedBy)
                 .HasMaxLength(255)
                 .IsRequired(false);

            model.Property(approvalSetting => approvalSetting.DeletedWhen)
                 .IsRequired(false);

            model.Property(approvalSetting => approvalSetting.DeletionReason)
                 .HasMaxLength(500)
                 .IsRequired(false);

            // design §8.4 resolution tiers — a single UNIQUE(EntityType) would prevent any
            // entity type from ever having more than one row, incompatible with the
            // per-content-type tier. NULL/NULL is not distinct in SQL Server's default
            // unique-index semantics, so this needs two filtered indexes rather than one:

            // Both carry an IsDeleted term, and it is load-bearing rather than tidy. §8.4
            // resolution skips soft-deleted rows at every tier and §14.5 rule 3 hides them from
            // every caller including Administrators, so a deleted row occupying a scope would be
            // invisible and immovable — and the API's delete is a SOFT delete, which made the
            // ordinary way to remove a policy the way that trapped its scope forever. There are
            // eight EntityType members with one default slot each; a trapped one could never be
            // re-created.

            // 1) at most one LIVE global default (EntityType IS NULL). The key column is all
            //    NULL on every row this filter admits, and NULL equals NULL under unique-index
            //    semantics — which is exactly what makes it a single slot rather than none.
            //
            //    NAMED IN THE CALL, as is the next one: EF keys an index on its property list,
            //    so two HasIndex calls over the same column silently replace each other unless
            //    the name is part of the definition. The first cut of this lost the global
            //    index that way and the migration never created it.
            model.HasIndex(
                     approvalSetting => approvalSetting.EntityType,
                     "UX_ApprovalSettings_GlobalDefault")
                 .IsUnique()
                 .HasFilter(
                     $"[{nameof(ApprovalSetting.EntityType)}] IS NULL AND " +
                     $"[{nameof(ApprovalSetting.IsDeleted)}] = 0");

            // 2) at most one LIVE entity-type-level default (no narrowing on either axis)
            model.HasIndex(
                     approvalSetting => approvalSetting.EntityType,
                     "UX_ApprovalSettings_EntityTypeDefault")
                 .IsUnique()
                 .HasFilter(
                     $"[{nameof(ApprovalSetting.EntityType)}] IS NOT NULL AND " +
                     $"[{nameof(ApprovalSetting.ContentType)}] IS NULL AND " +
                     $"[{nameof(ApprovalSetting.IsPersonal)}] IS NULL AND " +
                     $"[{nameof(ApprovalSetting.IsDeleted)}] = 0");

            // 3) at most one LIVE row per (EntityType, ContentType) when ContentType is populated
            model.HasIndex(approvalSetting => new { approvalSetting.EntityType, approvalSetting.ContentType })
                 .IsUnique()
                 .HasFilter(
                     $"[{nameof(ApprovalSetting.ContentType)}] IS NOT NULL AND " +
                     $"[{nameof(ApprovalSetting.IsDeleted)}] = 0")
                 .HasDatabaseName("UX_ApprovalSettings_EntityTypeContentType");

            // 4) at most one LIVE row per (EntityType, IsPersonal) when IsPersonal is populated —
            //    one personal and one editorial policy for associations, at most.
            model.HasIndex(approvalSetting => new { approvalSetting.EntityType, approvalSetting.IsPersonal })
                 .IsUnique()
                 .HasFilter(
                     $"[{nameof(ApprovalSetting.IsPersonal)}] IS NOT NULL AND " +
                     $"[{nameof(ApprovalSetting.IsDeleted)}] = 0")
                 .HasDatabaseName("UX_ApprovalSettings_AssociationPersonality");
        }
    }
}
