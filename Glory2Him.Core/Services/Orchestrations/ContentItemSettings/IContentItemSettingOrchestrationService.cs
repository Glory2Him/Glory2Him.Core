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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;

namespace Glory2Him.Core.Services.Orchestrations.ContentItemSettings
{
    public partial interface IContentItemSettingOrchestrationService
    {
        /// <summary>
        /// Adds a content item setting, DERIVING <c>ContentType</c> from the content item an
        /// override names rather than accepting the caller's word for it.
        ///
        /// <para>The content type is an authorization input: §12.5.2 business rule 6 lets the
        /// publisher tier for a content type write an override of that type, and the foundation
        /// composes that tier — and the matching §18.6 block — from the value on the row. A row
        /// that names its own type therefore decides who may write it, which is the wrong way
        /// round. This resolves the referenced item and overwrites the field, so the gate below
        /// composes from what the item IS.</para>
        ///
        /// <para>A per-type DEFAULT names no item, so there is nothing to derive from and the
        /// caller's content type stands — it is the whole of what that row is declaring, and only
        /// an administrator may write one.</para>
        /// </summary>
        ValueTask<ContentItemSetting> AddContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifies a content item setting. NOTHING IS DERIVED HERE, and that is not an omission:
        /// the foundation pins both <c>ContentType</c> and <c>ContentItemId</c> against the stored
        /// row, so a modify cannot move a row to another scope and there is no claim left to
        /// verify. Delegated unchanged.
        /// </summary>
        ValueTask<ContentItemSetting> ModifyContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Every content item setting. Delegated unchanged — a read spans one entity.
        /// </summary>
        ValueTask<IQueryable<ContentItemSetting>> RetrieveAllContentItemSettingsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// One content item setting by id. Delegated unchanged.
        /// </summary>
        ValueTask<ContentItemSetting> RetrieveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft-removes a content item setting. Delegated unchanged: the gate reads the STORED
        /// row, whose content type was derived when it was created.
        /// </summary>
        ValueTask<ContentItemSetting> RemoveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently removes a content item setting. Delegated unchanged, for the same reason
        /// as the soft removal.
        /// </summary>
        ValueTask<ContentItemSetting> HardRemoveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken = default);
    }
}
