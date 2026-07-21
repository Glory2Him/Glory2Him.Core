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
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Services.Foundations.Links
{
    /// <summary>
    /// Foundation service for links. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    public partial class LinkService : ILinkService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeFactory eventEnvelopeFactory;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public LinkService(
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

        public ValueTask<Link> AddLinkAsync(
            Link link,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateLinkIsNotNull(link);

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: link);

                return await DoAddLinkAsync(
                    link: link,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<Link>> RetrieveAllLinksAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllLinksAsync(cancellationToken);
            });

        public ValueTask<Link> RetrieveLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveLinkById(linkId);

                Link maybeLink =
                    await this.storageBroker.SelectLinkByIdAsync(linkId, cancellationToken);

                ValidateStorageLink(maybeLink, linkId);

                return maybeLink;
            });

        public ValueTask<Link> ModifyLinkAsync(
            Link link,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateLinkIsNotNull(link);

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: link);

                return await DoModifyLinkAsync(
                    link: link,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Link> RemoveLinkByIdAsync(
            Guid linkId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new Link
                {
                    Id = linkId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: removeRequest);

                return await DoRemoveLinkByIdAsync(
                    linkId: linkId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Link> HardRemoveLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new Link
                {
                    Id = linkId
                };

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveLinkByIdAsync(
                    linkId: linkId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<Link> DoAddLinkAsync(
            Link link,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            link = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: link, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddLinkAsync(
                link: link,
                securityContext: inboundEnvelope.SecurityContext);

            Link addedLink =
                await this.storageBroker.InsertLinkAsync(link, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.LinkOnAddingLinkSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Link> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedLink);

            await this.eventBroker.PublishLinkAsync(
                envelope: outboundEnvelope,
                operation: LinkEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.LinkOnAddingLinkSubscriptionName,
                cancellationToken: cancellationToken);

            return addedLink;
        }

        private async ValueTask<Link> DoModifyLinkAsync(
            Link link,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            link = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: link, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyLinkAsync(
                link: link,
                securityContext: inboundEnvelope.SecurityContext);

            Link maybeLink = await this.storageBroker.SelectLinkByIdAsync(
                linkId: link.Id,
                cancellationToken: cancellationToken);

            ValidateStorageLink(maybeLink, linkId: link.Id);

            link = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: link,
                    storageEntity: maybeLink);

            ValidateAgainstStorageLinkOnModify(
                inputLink: link,
                storageLink: maybeLink);

            Link updatedLink =
                await this.storageBroker.UpdateLinkAsync(link, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.LinkOnModifyingLinkSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Link> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedLink);

            await this.eventBroker.PublishLinkAsync(
                envelope: outboundEnvelope,
                operation: LinkEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.LinkOnModifyingLinkSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedLink;
        }

        private async ValueTask<Link> DoRemoveLinkByIdAsync(
            Guid linkId,
            string? deletionReason,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveLinkById(linkId);

            Link maybeLink =
                await this.storageBroker.SelectLinkByIdAsync(linkId, cancellationToken);

            ValidateStorageLink(maybeLink, linkId);

            if (maybeLink.IsDeleted)
                return maybeLink;

            if (deletionReason is not null)
                maybeLink.DeletionReason = deletionReason;

            Link auditedLink =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeLink,
                    securityContext: inboundEnvelope.SecurityContext);

            Link removedLink = await this.storageBroker.UpdateLinkAsync(
                link: auditedLink,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.LinkOnRemovingLinkByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Link> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedLink);

            await this.eventBroker.PublishLinkAsync(
                envelope: outboundEnvelope,
                operation: LinkEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.LinkOnRemovingLinkByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedLink;
        }

        private async ValueTask<Link> DoHardRemoveLinkByIdAsync(
            Guid linkId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnHardRemoveLinkById(linkId);

            Link maybeLink =
                await this.storageBroker.SelectLinkByIdAsync(linkId, cancellationToken);

            ValidateStorageLink(maybeLink, linkId);

            Link deletedLink =
                await this.storageBroker.DeleteLinkAsync(maybeLink, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Link> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedLink);

            await this.eventBroker.PublishLinkAsync(
                envelope: outboundEnvelope,
                operation: LinkEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedLink;
        }
    }
}
