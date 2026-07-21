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
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Factories.Events;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.BibleReferences;

namespace Glory2Him.Core.Services.Foundations.BibleReferences
{
    /// <summary>
    /// Foundation service for bible references. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    public partial class BibleReferenceService : IBibleReferenceService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeFactory eventEnvelopeFactory;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public BibleReferenceService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeFactory eventEnvelopeFactory,
            ISecurityAuditBroker securityAuditBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeFactory = eventEnvelopeFactory;
            this.securityAuditBroker = securityAuditBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<BibleReference> AddBibleReferenceAsync(
            BibleReference bibleReference,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateBibleReferenceIsNotNull(bibleReference);

                EventEnvelope<BibleReference> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: bibleReference);

                return await DoAddBibleReferenceAsync(
                    bibleReference: bibleReference,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<BibleReference>> RetrieveAllBibleReferencesAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllBibleReferencesAsync(cancellationToken);
            });

        public ValueTask<BibleReference> RetrieveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveBibleReferenceById(bibleReferenceId);

                BibleReference maybeBibleReference =
                    await this.storageBroker.SelectBibleReferenceByIdAsync(bibleReferenceId, cancellationToken);

                ValidateStorageBibleReference(maybeBibleReference, bibleReferenceId);

                return maybeBibleReference;
            });

        public ValueTask<BibleReference> ModifyBibleReferenceAsync(
            BibleReference bibleReference,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateBibleReferenceIsNotNull(bibleReference);

                EventEnvelope<BibleReference> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: bibleReference);

                return await DoModifyBibleReferenceAsync(
                    bibleReference: bibleReference,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<BibleReference> RemoveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new BibleReference
                {
                    Id = bibleReferenceId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<BibleReference> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: removeRequest);

                return await DoRemoveBibleReferenceByIdAsync(
                    bibleReferenceId: bibleReferenceId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<BibleReference> HardRemoveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new BibleReference
                {
                    Id = bibleReferenceId
                };

                EventEnvelope<BibleReference> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveBibleReferenceByIdAsync(
                    bibleReferenceId: bibleReferenceId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<BibleReference> DoAddBibleReferenceAsync(
            BibleReference bibleReference,
            EventEnvelope<BibleReference> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            bibleReference = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: bibleReference, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddBibleReferenceAsync(
                bibleReference: bibleReference,
                securityContext: inboundEnvelope.SecurityContext);

            BibleReference addedBibleReference =
                await this.storageBroker.InsertBibleReferenceAsync(bibleReference, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<BibleReference> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedBibleReference);

            await this.eventBroker.PublishBibleReferenceAsync(
                envelope: outboundEnvelope,
                operation: BibleReferenceEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName,
                cancellationToken: cancellationToken);

            return addedBibleReference;
        }

        private async ValueTask<BibleReference> DoModifyBibleReferenceAsync(
            BibleReference bibleReference,
            EventEnvelope<BibleReference> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            bibleReference = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: bibleReference, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyBibleReferenceAsync(
                bibleReference: bibleReference,
                securityContext: inboundEnvelope.SecurityContext);

            BibleReference maybeBibleReference = await this.storageBroker.SelectBibleReferenceByIdAsync(
                bibleReferenceId: bibleReference.Id,
                cancellationToken: cancellationToken);

            ValidateStorageBibleReference(maybeBibleReference, bibleReferenceId: bibleReference.Id);

            bibleReference = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: bibleReference,
                    storageEntity: maybeBibleReference);

            ValidateAgainstStorageBibleReferenceOnModify(
                inputBibleReference: bibleReference,
                storageBibleReference: maybeBibleReference);

            BibleReference updatedBibleReference =
                await this.storageBroker.UpdateBibleReferenceAsync(bibleReference, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnModifyingBibleReferenceSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<BibleReference> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedBibleReference);

            await this.eventBroker.PublishBibleReferenceAsync(
                envelope: outboundEnvelope,
                operation: BibleReferenceEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnModifyingBibleReferenceSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedBibleReference;
        }

        private async ValueTask<BibleReference> DoRemoveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            string? deletionReason,
            EventEnvelope<BibleReference> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveBibleReferenceById(bibleReferenceId);

            BibleReference maybeBibleReference =
                await this.storageBroker.SelectBibleReferenceByIdAsync(bibleReferenceId, cancellationToken);

            ValidateStorageBibleReference(maybeBibleReference, bibleReferenceId);

            if (maybeBibleReference.IsDeleted)
                return maybeBibleReference;

            if (deletionReason is not null)
                maybeBibleReference.DeletionReason = deletionReason;

            BibleReference auditedBibleReference =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeBibleReference,
                    securityContext: inboundEnvelope.SecurityContext);

            BibleReference removedBibleReference = await this.storageBroker.UpdateBibleReferenceAsync(
                bibleReference: auditedBibleReference,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<BibleReference> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedBibleReference);

            await this.eventBroker.PublishBibleReferenceAsync(
                envelope: outboundEnvelope,
                operation: BibleReferenceEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedBibleReference;
        }

        private async ValueTask<BibleReference> DoHardRemoveBibleReferenceByIdAsync(
            Guid bibleReferenceId,
            EventEnvelope<BibleReference> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnHardRemoveBibleReferenceById(bibleReferenceId);

            BibleReference maybeBibleReference =
                await this.storageBroker.SelectBibleReferenceByIdAsync(bibleReferenceId, cancellationToken);

            ValidateStorageBibleReference(maybeBibleReference, bibleReferenceId);

            BibleReference deletedBibleReference =
                await this.storageBroker.DeleteBibleReferenceAsync(maybeBibleReference, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<BibleReference> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedBibleReference);

            await this.eventBroker.PublishBibleReferenceAsync(
                envelope: outboundEnvelope,
                operation: BibleReferenceEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedBibleReference;
        }
    }
}
