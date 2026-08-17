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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Glory2Him.Core.Services.Foundations.Links;

namespace Glory2Him.Core.Services.Processings.Links
{
    internal partial class LinkProcessingService : ILinkProcessingService
    {
        private readonly ILinkService linkService;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly IEventBroker eventBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public LinkProcessingService(
            ILinkService linkService,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            IEventBroker eventBroker,
            ISecurityAuditBroker securityAuditBroker,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.linkService = linkService;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.eventBroker = eventBroker;
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

        public ValueTask<IQueryable<Link>> RetrieveAllLinksAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // an unfiltered collection read carries no instruction beyond the caller's
                // identity, so the request payload is empty — the envelope exists to capture
                // the ambient security context the visibility filter runs against
                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new Link());

                return await DoRetrieveAllLinksAsync(
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<Link>> RetrieveAllPublicLinksAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the public projection is caller-independent, so no envelope is minted —
                // there is no security context to capture and nothing downstream reads one
                return await DoRetrieveAllPublicLinksAsync(cancellationToken);
            });

        public ValueTask<IQueryable<Link>> RetrieveLinksByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new Link
                {
                    GroupId = groupId
                };

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveLinksByGroupIdAsync(
                    groupId: groupId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Link> RetrieveLatestLinkByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new Link
                {
                    GroupId = groupId
                };

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveLatestLinkByGroupIdAsync(
                    groupId: groupId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Link> RetrievePublishedLinkByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new Link
                {
                    GroupId = groupId
                };

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrievePublishedLinkByGroupIdAsync(
                    groupId: groupId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<Link> DoAddLinkAsync(
            Link link,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnAddLink(link, inboundEnvelope.SecurityContext);

            // PublishDate is deliberately absent, for the same reason it is absent from the
            // version fork below. It is an IApproval member (§9.7.1 rule 2), and the add
            // surface may carry an ApprovalStatus of Draft or Submitted and nothing else —
            // never IsPublished, never PublishDate (rule 1). Taking it from the caller here
            // would let them schedule their own publication on the way in, on a row that is
            // otherwise landed unpublished and in Draft precisely so it cannot.
            Link newLink = new Link
            {
                Id = await this.identifierBroker.GetIdentifierAsync(),
                Name = link.Name,
                Url = link.Url,
                LinkType = link.LinkType,
                GroupId = await this.identifierBroker.GetIdentifierAsync(),
                Version = 1,
                IsLatestVersion = true,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            Link addedLink = await this.linkService.AddLinkAsync(
                link: newLink,
                cancellationToken: cancellationToken);

            await PublishLinkProcessingFactAsync(
                inboundEnvelope: inboundEnvelope,
                link: addedLink,
                operation: LinkProcessingEventOperation.Added);

            return addedLink;
        }

        private async ValueTask<Link> DoModifyLinkAsync(
            Link link,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnModifyLink(link, inboundEnvelope.SecurityContext);

            Link currentLink = await this.linkService.RetrieveLinkByIdAsync(
                linkId: link.Id,
                cancellationToken: cancellationToken);

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: inboundEnvelope.SecurityContext);

            ValidateCurrentLinkIsModifiable(
                currentLink: currentLink,
                actorUserId: actorUserId,
                securityContext: inboundEnvelope.SecurityContext);

            // a terminal row is immutable in place — the owner's modify forks a new version
            // (§3.4 rules 7–8, rule 16). Rejected forks for the same reason Approved does:
            // the row is the record of a decision, and editing it would rewrite what was
            // decided. A fork off a Rejected row leaves the group with no published row
            // until the new version is approved, which is correct — a rejected row was
            // never published. Dismissed is deliberately absent: it is not a decision this
            // service may fork off, and refusing it belongs to the foundation's modify.
            bool shouldForkNewVersion =
                currentLink.ApprovalStatus == ApprovalStatus.Approved
                    || currentLink.ApprovalStatus == ApprovalStatus.Rejected;

            Link modifiedLink = shouldForkNewVersion
                ? await ForkLinkVersionAsync(
                    link: link,
                    currentLink: currentLink,
                    cancellationToken: cancellationToken)

                : await ModifyLinkInPlaceAsync(
                    link: link,
                    currentLink: currentLink,
                    cancellationToken: cancellationToken);

            // one fact per completed process: a fork writes two foundation rows, but the
            // processing service announces the amend exactly once, after both writes have landed
            await PublishLinkProcessingFactAsync(
                inboundEnvelope: inboundEnvelope,
                link: modifiedLink,
                operation: LinkProcessingEventOperation.Modified);

            return modifiedLink;
        }

        private async ValueTask<Link> DoRemoveLinkByIdAsync(
            Guid linkId,
            string? deletionReason,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveLinkById(linkId, inboundEnvelope.SecurityContext);

            Link currentLink = await this.linkService.RetrieveLinkByIdAsync(
                linkId: linkId,
                cancellationToken: cancellationToken);

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: inboundEnvelope.SecurityContext);

            ValidateCurrentLinkIsRemovable(
                currentLink: currentLink,
                actorUserId: actorUserId,
                securityContext: inboundEnvelope.SecurityContext);

            // the foundation owns the soft-delete control fields (IsDeleted, DeletedBy,
            // DeletedWhen, DeletionReason) and leaves ApprovalStatus alone
            Link removedLink = await this.linkService.RemoveLinkByIdAsync(
                linkId: linkId,
                deletionReason: deletionReason,
                cancellationToken: cancellationToken);

            await PublishLinkProcessingFactAsync(
                inboundEnvelope: inboundEnvelope,
                link: removedLink,
                operation: LinkProcessingEventOperation.Removed);

            return removedLink;
        }

        private async ValueTask<Link> DoRetrieveLinkByIdAsync(
            Guid linkId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateLinkIdOnRetrieve(linkId);

            Link link = await this.linkService.RetrieveLinkByIdAsync(
                linkId: linkId,
                cancellationToken: cancellationToken);

            // a removed row is gone for every caller, privileged or not — review and audit
            // reads cover the approval workflow, not takedowns
            if (link.IsDeleted)
            {
                // the caller-facing error stays a reason-free not-found (no existence
                // leak), so the true reason is recorded server-side before the throw
                await this.loggingBroker.LogInformationAsync(
                    message: $"Link read denied. Link {linkId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundLinkProcessingException(
                    message: "The link was not found.");
            }

            return await ApplySingleReadVisibilityPostureAsync(
                link: link,
                securityContext: inboundEnvelope.SecurityContext);
        }

        private async ValueTask<IQueryable<Link>> DoRetrieveAllLinksAsync(
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            IQueryable<Link> allLinks =
                await this.linkService.RetrieveAllLinksAsync(cancellationToken);

            return await ApplyCollectionReadVisibilityFilterAsync(
                links: allLinks,
                securityContext: inboundEnvelope.SecurityContext);
        }

        private async ValueTask<IQueryable<Link>> DoRetrieveAllPublicLinksAsync(
            CancellationToken cancellationToken)
        {
            IQueryable<Link> allLinks =
                await this.linkService.RetrieveAllLinksAsync(cancellationToken);

            // running the collection filter without a security context yields exactly the
            // canonical visible set (§14.1) — a privileged caller reads the same set an
            // anonymous visitor would
            return await ApplyCollectionReadVisibilityFilterAsync(
                links: allLinks,
                securityContext: null);
        }

        private async ValueTask<IQueryable<Link>> DoRetrieveLinksByGroupIdAsync(
            Guid groupId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateGroupIdOnRetrieve(groupId);

            IQueryable<Link> allLinks =
                await this.linkService.RetrieveAllLinksAsync(cancellationToken);

            IQueryable<Link> groupLinks = allLinks.Where(link =>
                link.GroupId == groupId);

            return await ApplyCollectionReadVisibilityFilterAsync(
                links: groupLinks,
                securityContext: inboundEnvelope.SecurityContext);
        }

        private async ValueTask<Link> DoRetrieveLatestLinkByGroupIdAsync(
            Guid groupId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateGroupIdOnRetrieve(groupId);

            IQueryable<Link> allLinks =
                await this.linkService.RetrieveAllLinksAsync(cancellationToken);

            // the edit tip of the group (§3.4.1) — at most one non-deleted row per group
            // carries IsLatestVersion under the unique filtered index
            Link? latestLink = allLinks.FirstOrDefault(link =>
                link.GroupId == groupId
                    && link.IsLatestVersion
                    && link.IsDeleted == false);

            if (latestLink is null)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Link read denied. Group {groupId} has no " +
                        "non-deleted latest version; reported to the caller as not found.");

                throw new NotFoundLinkProcessingException(
                    message: "The link was not found.");
            }

            return await ApplySingleReadVisibilityPostureAsync(
                link: latestLink,
                securityContext: inboundEnvelope.SecurityContext);
        }

        private async ValueTask<Link> DoRetrievePublishedLinkByGroupIdAsync(
            Guid groupId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateGroupIdOnRetrieve(groupId);

            IQueryable<Link> allLinks =
                await this.linkService.RetrieveAllLinksAsync(cancellationToken);

            // the row the public currently reads — it stays published while a newer draft
            // moves through review, so it is found independently of IsLatestVersion
            Link? publishedLink = allLinks.FirstOrDefault(link =>
                link.GroupId == groupId
                    && link.IsPublished
                    && link.IsDeleted == false);

            if (publishedLink is null)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Link read denied. Group {groupId} has no " +
                        "non-deleted published version; reported to the caller as not found.");

                throw new NotFoundLinkProcessingException(
                    message: "The link was not found.");
            }

            return await ApplySingleReadVisibilityPostureAsync(
                link: publishedLink,
                securityContext: inboundEnvelope.SecurityContext);
        }

        // the shared read posture of design §14.1/§16.6 for single-row reads: a publicly
        // visible version is readable by anyone — reads carry no contribution gate and the
        // block roles only block contributions; a non-public version answers not-found —
        // never unauthorized — to everyone but the owner and the review roles
        private async ValueTask<Link> ApplySingleReadVisibilityPostureAsync(
            Link link,
            SecurityContext securityContext)
        {
            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                link.ApprovalStatus == ApprovalStatus.Approved
                    && link.IsPublished
                    && (link.PublishDate is null
                        || link.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return link;
            }

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                // the caller-facing error stays a reason-free not-found (no existence
                // leak), so the true reason is recorded server-side before the throw
                await this.loggingBroker.LogWarningAsync(
                    message: $"Link read denied. Link {link.Id} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundLinkProcessingException(
                    message: "The link was not found.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            await ValidateCurrentLinkIsRetrievableAsync(
                currentLink: link,
                actorUserId: actorUserId,
                securityContext: securityContext);

            return link;
        }

        // the collection twin of the single-row posture: instead of throwing not-found, a
        // row the caller may not see simply drops out of the set, so a collection read never
        // reveals how many non-public versions exist
        private async ValueTask<IQueryable<Link>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<Link> links,
            SecurityContext? securityContext)
        {
            // a removed row is gone for every caller, privileged or not — review and audit
            // reads cover the approval workflow, not takedowns
            IQueryable<Link> visibleLinks = links.Where(link =>
                link.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            // a review-role caller audits the whole pipeline: every non-deleted row,
            // including drafts and future-scheduled rows — the clock and the caller's
            // identity are never consulted. Unlike ContentItem there is no narrow tier to
            // resolve here: only ContentItem carries a ContentType, so Link's review roles
            // already span every row of the type (§18.6 rule 5).
            if (isAuthenticated && HasReviewRole(securityContext!))
            {
                return visibleLinks;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            string? actorUserId = isAuthenticated
                ? await this.securityAuditBroker.GetUserIdAsync(securityContext: securityContext!)
                : null;

            // an authenticated caller follows their own links through the workflow, so their
            // own rows join the publicly visible set; an anonymous caller (or one whose
            // identity cannot be resolved) sees the public set alone
            bool includeOwnLinks = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleLinks.Where(link =>
                (link.ApprovalStatus == ApprovalStatus.Approved
                    && link.IsPublished
                    && (link.PublishDate == null
                        || link.PublishDate <= currentDateTime))
                || (includeOwnLinks && link.CreatedBy == actorUserId));
        }

        // this service's own completion fact, distinct from the foundation's entity
        // fact: it asserts that this process finished with its gates passed and its
        // invariants restored, which is what downstream processes chain off
        private async ValueTask PublishLinkProcessingFactAsync(
            EventEnvelope<Link> inboundEnvelope,
            Link link,
            LinkProcessingEventOperation operation)
        {
            EventEnvelope<Link> outboundEnvelope = await this.eventEnvelopeBroker.CreateNextAsync(
                sourceEnvelope: inboundEnvelope,
                content: link);

            await this.eventBroker.PublishLinkProcessingAsync(
                envelope: outboundEnvelope,
                operation: operation);
        }

        private async ValueTask<Link> ModifyLinkInPlaceAsync(
            Link link,
            Link currentLink,
            CancellationToken cancellationToken)
        {
            MapPermittedFields(
                targetLink: currentLink,
                sourceLink: link);

            return await this.linkService.ModifyLinkAsync(
                link: currentLink,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<Link> ForkLinkVersionAsync(
            Link link,
            Link currentLink,
            CancellationToken cancellationToken)
        {
            // PublishDate is deliberately absent, so the new version starts with none. It is
            // an IApproval member (§9.7.1 rule 2) and the fork is still the modify operation,
            // so taking it from the caller here would simply reopen the door MapPermittedFields
            // just closed: edit a terminal link and your publish date rides in on the fork.
            // A fresh draft has no publish date until the approve operation grants one, which
            // is the same reason IsPublished starts false and the status starts Draft.
            var newVersionLink = new Link
            {
                Id = await this.identifierBroker.GetIdentifierAsync(),
                Name = link.Name,
                Url = link.Url,
                LinkType = link.LinkType,
                GroupId = currentLink.GroupId,
                Version = currentLink.Version + 1,
                IsLatestVersion = true,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            // The previous latest is demoted before the new row is inserted — the unique
            // filtered index allows only one IsLatestVersion = true per group at any time.
            // IsLatestVersion only marks the edit tip; IsPublished is untouched here, so the
            // previously published row stays publicly visible until the new version is
            // approved and published (§3.4.1). A fork off a Rejected row has no published
            // row to preserve, so the group is simply dark until the new version lands.
            //
            // Through the narrow demote verb rather than the general modify: IsLatestVersion is
            // an IVersion member, which the modify pins against storage like every other
            // non-content field (§9.7.1 rule 2). Demoting through the modify asked the one path
            // required to refuse this write to make it, and left the foundation unable to tell
            // the fork apart from a caller tampering with version bookkeeping.
            await this.linkService.DemoteLinkVersionAsync(
                linkId: currentLink.Id,
                cancellationToken: cancellationToken);

            return await this.linkService.AddLinkAsync(
                link: newVersionLink,
                cancellationToken: cancellationToken);
        }

        // The content fields, and only those. Under §9.7.1 rule 2's subtraction rule every
        // IApproval member — ApprovalStatus, IsPublished and PublishDate — belongs to the
        // approve operation as one unit, so none of them is carried here. PublishDate is the
        // one that looks like content and is not: a caller who could set it through the
        // general modify would schedule their own publication without ever meeting the gate
        // that owns it. The foundation pins all three against storage as well (§8.6.1 — a
        // rule enforced only at the processing layer is not enforced).
        private static void MapPermittedFields(
            Link targetLink,
            Link sourceLink)
        {
            targetLink.Name = sourceLink.Name;
            targetLink.Url = sourceLink.Url;
            targetLink.LinkType = sourceLink.LinkType;
        }
    }
}
