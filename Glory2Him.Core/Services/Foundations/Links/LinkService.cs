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
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;

namespace Glory2Him.Core.Services.Foundations.Links
{
    /// <summary>
    /// Foundation service for links. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain. Per design §14.6 the foundation enforces security itself — the
    /// contribution gate on writes, owner-or-moderation-role write permission (removal by
    /// owner or Admin, hard removal by Admin only), and the §14.1/§14.5 read visibility
    /// posture — never assuming an upstream orchestration already gated the caller.
    /// </summary>
    internal partial class LinkService : ILinkService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public LinkService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            ISecurityAuditBroker securityAuditBroker,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
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
                    await this.eventEnvelopeBroker.CreateAsync(content: link);

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

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new Link());

                IQueryable<Link> allLinks =
                    await this.storageBroker.SelectAllLinksAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    links: allLinks,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<Link> RetrieveLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new Link
                {
                    Id = linkId
                };

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveLinkByIdAsync(
                    linkId: linkId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Link> ModifyLinkAsync(
            Link link,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateLinkIsNotNull(link);

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: link);

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
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

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
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveLinkByIdAsync(
                    linkId: linkId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a publicly visible version
        // is readable by anyone; a non-public version answers not-found — never
        // unauthorized — to everyone but the owner and the review roles, with the true
        // denial reason logged server-side only
        private async ValueTask<Link> DoRetrieveLinkByIdAsync(
            Guid linkId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveLinkById(linkId);

            Link maybeLink = await this.storageBroker.SelectLinkByIdAsync(
                linkId: linkId,
                cancellationToken: cancellationToken);

            ValidateStorageLink(maybeLink, linkId);

            if (maybeLink.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Link read denied. Link {linkId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundLinkException(
                    message: $"Link not found with id: {linkId}.");
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                maybeLink.ApprovalStatus == ApprovalStatus.Approved
                    && maybeLink.IsPublished
                    && (maybeLink.PublishDate is null
                        || maybeLink.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return maybeLink;
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Link read denied. Link {linkId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundLinkException(
                    message: $"Link not found with id: {linkId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeLink.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Link read denied. Link {linkId} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundLinkException(
                    message: $"Link not found with id: {linkId}.");
            }

            return maybeLink;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many non-public rows exist
        private async ValueTask<IQueryable<Link>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<Link> links,
            SecurityContext? securityContext)
        {
            IQueryable<Link> visibleLinks = links.Where(link =>
                link.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated && HasReviewRole(securityContext!))
            {
                return visibleLinks;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            string? actorUserId = isAuthenticated
                ? await this.securityAuditBroker.GetUserIdAsync(securityContext: securityContext!)
                : null;

            bool includeOwnLinks = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleLinks.Where(link =>
                (link.ApprovalStatus == ApprovalStatus.Approved
                    && link.IsPublished
                    && (link.PublishDate == null
                        || link.PublishDate <= currentDateTime))
                || (includeOwnLinks && link.CreatedBy == actorUserId));
        }

        private async ValueTask<Link> DoAddLinkAsync(
            Link link,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

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
                await this.eventEnvelopeBroker.CreateNextAsync(
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
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            link = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: link, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyLinkAsync(
                link: link,
                securityContext: inboundEnvelope.SecurityContext);

            Link maybeLink = await this.storageBroker.SelectLinkByIdAsync(
                linkId: link.Id,
                cancellationToken: cancellationToken);

            ValidateStorageLink(maybeLink, linkId: link.Id);

            bool mayTransitionApprovalStatus =
                await ValidateUserCanModifyStorageLinkAsync(
                    storageLink: maybeLink,
                    securityContext: inboundEnvelope.SecurityContext);

            link = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: link,
                    storageEntity: maybeLink);

            ValidateAgainstStorageLinkOnModify(
                inputLink: link,
                storageLink: maybeLink,
                mayTransitionApprovalStatus: mayTransitionApprovalStatus);

            Link updatedLink =
                await this.storageBroker.UpdateLinkAsync(link, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.LinkOnModifyingLinkSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Link> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
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
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnRemoveLinkById(linkId);

            Link maybeLink =
                await this.storageBroker.SelectLinkByIdAsync(linkId, cancellationToken);

            ValidateStorageLink(maybeLink, linkId);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageLinkAsync(
                storageLink: maybeLink,
                securityContext: inboundEnvelope.SecurityContext);

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
                await this.eventEnvelopeBroker.CreateNextAsync(
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
            ValidateUserCanHardRemoveLink(inboundEnvelope.SecurityContext);
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
                await this.eventEnvelopeBroker.CreateNextAsync(
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
