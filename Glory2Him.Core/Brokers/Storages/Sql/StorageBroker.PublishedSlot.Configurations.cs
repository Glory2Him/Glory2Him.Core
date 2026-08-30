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

using Glory2Him.Core.Models.Bases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        // The published slot (§3.4.1): at most one LIVE published row per group.
        //
        // Declared once for every versioned entity rather than written out per configuration,
        // because written by hand the three drifted to the same wrong shape — Attachment,
        // Link and ContentItem all filtered on the flag alone, so a soft-deleted published
        // row went on holding its group's slot and no later version could ever take it
        // (§5.6.4 rule 4). One declaration is what stops the next entity repeating it.
        //
        // The IsDeleted term is not redundant against §9.7.6 rule 1's unpublish-on-remove
        // mandate. That is the flow half — and ContentItem and Link have it, in the unfiltered
        // incumbent probe their promote path runs. This is the defence-in-depth half, for any
        // row that reaches the state another way.
        //
        // GroupId alone is the key: the filter pins IsPublished to 1, so as a second key
        // column it carries no selectivity and only obscures that the rule is one row per
        // group. The index NAMES still say IsPublished because they name the rule.
        private static void AddPublishedSlotIndex<TEntity>(
            EntityTypeBuilder<TEntity> model,
            string indexName)
                where TEntity : class, IAudit, IVersion, IApproval =>
            model.HasIndex(nameof(IVersion.GroupId))
                 .IsUnique()
                 .HasFilter(
                    $"[{nameof(IApproval.IsPublished)}] = 1 "
                        + $"AND [{nameof(IAudit.IsDeleted)}] = 0")
                 .HasDatabaseName(indexName);
    }
}
