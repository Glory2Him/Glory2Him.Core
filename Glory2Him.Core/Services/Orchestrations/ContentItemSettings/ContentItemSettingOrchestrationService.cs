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
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;

namespace Glory2Him.Core.Services.Orchestrations.ContentItemSettings
{
    /// <summary>
    /// Coordinates the one content item setting flow that spans a second entity type (§12.1
    /// rule 2): deriving an override's <c>ContentType</c> from the content item it names.
    ///
    /// <para><b>Why this layer exists at all.</b> §12.5.2 business rule 6 lets the publisher tier
    /// for a content type write an override OF that type, and the foundation composes both the
    /// grant and the §18.6 block from the <c>ContentType</c> sitting on the row. While that value
    /// is the caller's to set, the row decides who may write it. A holder of
    /// <c>ContentItem-Devotional-Publishers</c> could post a row labelled <c>Devotional</c>
    /// against a <i>quote's</i> id, pass the gate, and take that quote's only override scope —
    /// <c>UX_ContentItemSettings_OverridePerEntity</c> is keyed on <c>ContentItemId</c> alone —
    /// leaving the quote's own publishers unable to create one (409) or remove theirs (the removal
    /// gate reads the stored type). This resolves the item and overwrites the field, so the gate
    /// composes from what the item IS.</para>
    ///
    /// <para><b>Why the foundation could not do it.</b> §12.3 gives a foundation the rules for ONE
    /// entity, and §12.1 rule 1 confines a processing service to one entity type — so reading
    /// <c>ContentItem</c> from either is the same breach, which is why this is an orchestration
    /// and not the processing service §12.5.2's banner once called for. That banner's premise was
    /// that the flow stays inside one entity type; this flow does not.</para>
    ///
    /// <para><b>The precedent is exact.</b> <c>AssociationOrchestrationService</c> resolves its
    /// endpoints and derives their content type for the same reason and in the same words — "the
    /// content type is an authorization input" — with the foundation left validating shape only.
    /// </para>
    ///
    /// <para><b>It publishes nothing of its own.</b> <c>ContentItemSetting</c> is neither
    /// approvable nor versioned, so §10.17's fork and approval-invalidation rules do not apply and
    /// the foundation's own past-tense facts remain the whole story. An orchestration fact would
    /// be a second address for the same event with no subscriber that needs it.</para>
    ///
    /// <para><b>Both entry paths reach it.</b> The HTTP exposer binds here, and so does the
    /// <c>ContentItemSetting-Adding</c> subscription — see the <c>.Substrate</c> partial. While
    /// that subscription bound the foundation, the derivation was a property of one path rather
    /// than of the entity, which is precisely what §14.6 rule 1 refuses to allow (#456).</para>
    /// </summary>
    internal partial class ContentItemSettingOrchestrationService : IContentItemSettingOrchestrationService
    {
        private readonly IContentItemSettingService contentItemSettingService;
        private readonly IContentItemService contentItemService;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentItemSettingOrchestrationService(
            IContentItemSettingService contentItemSettingService,
            IContentItemService contentItemService,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.contentItemSettingService = contentItemSettingService;
            this.contentItemService = contentItemService;
            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<ContentItemSetting> AddContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateContentItemSettingIsNotNull(contentItemSetting);

                // AN OVERRIDE'S TYPE IS THE ITEM'S TYPE, whatever the caller wrote. A default
                // names no item, so there is nothing to resolve and its content type is the whole
                // of what it declares — an administrator saying "this is the Devotional default".
                if (contentItemSetting.ContentItemId is not null)
                {
                    ContentItem contentItem =
                        await ResolveContentItemAsync(
                            contentItemSetting.ContentItemId.Value,
                            cancellationToken);

                    contentItemSetting.ContentType = contentItem.ContentType;
                }

                return await this.contentItemSettingService.AddContentItemSettingAsync(
                    contentItemSetting,
                    cancellationToken);
            });

        public ValueTask<ContentItemSetting> ModifyContentItemSettingAsync(
            ContentItemSetting contentItemSetting,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Nothing to derive: the foundation pins ContentType and ContentItemId against
                // the stored row, so a modify cannot move a row to another scope and there is no
                // claim left to check.
                return await this.contentItemSettingService.ModifyContentItemSettingAsync(
                    contentItemSetting,
                    cancellationToken);
            });

        public ValueTask<IQueryable<ContentItemSetting>> RetrieveAllContentItemSettingsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatchQueryable(async () =>
                await this.contentItemSettingService.RetrieveAllContentItemSettingsAsync(
                    cancellationToken));

        public ValueTask<ContentItemSetting> RetrieveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
                await this.contentItemSettingService.RetrieveContentItemSettingByIdAsync(
                    contentItemSettingId,
                    cancellationToken));

        public ValueTask<ContentItemSetting> RemoveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
                await this.contentItemSettingService.RemoveContentItemSettingByIdAsync(
                    contentItemSettingId,
                    deletionReason,
                    cancellationToken));

        public ValueTask<ContentItemSetting> HardRemoveContentItemSettingByIdAsync(
            Guid contentItemSettingId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
                await this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    contentItemSettingId,
                    cancellationToken));

    }
}
