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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Processings.Links
{
    internal partial class LinkProcessingService
    {
        private static void ValidateOnAddLink(
            Link link,
            SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);
            ValidateLink(link);
        }

        private static void ValidateOnModifyLink(
            Link link,
            SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);
            ValidateLinkOnModify(link);
        }

        private static void ValidateOnRemoveLinkById(
            Guid linkId,
            SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);
            ValidateLinkIdOnRemove(linkId);
        }

        private static void ValidateCurrentLinkIsRemovable(
            Link currentLink,
            string actorUserId,
            SecurityContext securityContext)
        {
            // a remove is idempotent from the caller's point of view, but an already
            // removed row must never be presented as a fresh removal
            if (currentLink.IsDeleted)
            {
                throw new NotFoundLinkProcessingException(
                    message: "The link was not found.");
            }

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && currentLink.CreatedBy == actorUserId;

            // removing content is a takedown, not a moderation step — the owner may remove
            // their own link and an Admin may remove anyone's; Reviewers and Publishers
            // moderate through the approval workflow instead
            bool isPermitted = isOwner || securityContext.Roles.Contains(Roles.Admin);

            if (isPermitted is false)
            {
                throw new UnauthorizedLinkProcessingException(
                    message: "The current user is not allowed to remove this link.");
            }
        }

        private static void ValidateCurrentLinkIsModifiable(
            Link currentLink,
            string actorUserId,
            SecurityContext securityContext)
        {
            if (currentLink.IsDeleted)
            {
                throw new NotFoundLinkProcessingException(
                    message: "The link was not found.");
            }

            if (currentLink.IsLatestVersion is false)
            {
                throw new InvalidLinkProcessingException(
                    message: "Only the latest version of a link may be modified.");
            }

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && currentLink.CreatedBy == actorUserId;

            // a not-yet-decided link may be corrected in place by a Reviewer, Publisher or
            // Admin during review; a terminal one belongs to its owner alone, because the
            // only edit it admits is a fork onto a fresh version (§3.4 rule 16) and a
            // moderator forking someone else's decided row would author a version in
            // their name
            bool hasModifyRole = HasReviewRole(securityContext);

            bool isTerminal =
                currentLink.ApprovalStatus == ApprovalStatus.Approved
                    || currentLink.ApprovalStatus == ApprovalStatus.Rejected;

            bool isPermitted = isTerminal
                ? isOwner
                : isOwner || hasModifyRole;

            if (isPermitted is false)
            {
                throw new UnauthorizedLinkProcessingException(
                    message: "The current user is not allowed to modify this link.");
            }
        }

        // instance and async, unlike its sibling validations: the caller-facing error is a
        // deliberately reason-free not-found (no existence leak), so this is the only place
        // the true denial reason can be recorded — server-side, never on the exception
        private async ValueTask ValidateCurrentLinkIsRetrievableAsync(
            Link currentLink,
            string actorUserId,
            SecurityContext securityContext)
        {
            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && currentLink.CreatedBy == actorUserId;

            // a non-public version may be read by its owner and by the moderation roles for
            // this entity type (§16.6, §18.6) for review and audit; everyone else gets
            // not-found so a probe cannot tell a non-public version from a missing one
            bool isPermitted = isOwner || HasReviewRole(securityContext);

            if (isPermitted is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Link read denied. Link {currentLink.Id} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundLinkProcessingException(
                    message: "The link was not found.");
            }
        }

        // Link has two role tiers, not ContentItem's three: the narrow tier exists only
        // where an entity carries a ContentType, and no entity but ContentItem does
        // (design §18.6 rule 5). So a Link-Reviewer covers every link there is, and there
        // is no per-row question to ask of the caller's grants.
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.LinkReviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.LinkPublisher)
                || securityContext.Roles.Contains(Roles.Admin);

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedLinkProcessingException(
                    message: "The current user is not authenticated.");
            }

            bool isBlocked =
                securityContext.Roles.Contains(Roles.ReadOnly)
                    || securityContext.Roles.Contains(Roles.LinkReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedLinkProcessingException(
                    message: "The current user is blocked from contributing links.");
            }
        }

        private static void ValidateLinkIsNotNull(Link link)
        {
            if (link is null)
            {
                throw new NullLinkProcessingException(message: "Link is null.");
            }
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The processing service is an
        // event receiver too: it front-loads the contribution / owner / Admin decision against the
        // inbound envelope's SecurityContext, so without this check a caller who can put a message
        // on a LinkProcessing address states their own roles and is believed (design §14.6 rule 4).
        // Verification sits in the receiver, not the transport, because a handler is reachable
        // without going through the broker.
        private async ValueTask ValidateLinkEventEnvelopeAsync(
            EventEnvelope<Link> envelope,
            LinkProcessingEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidLinkProcessingEventException(
                    message: "Invalid link processing event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"LinkProcessing{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidLinkProcessingEventException(
                    message: "Invalid link processing event. " +
                        "Integrity verification failed.");
            }
        }

        private static void ValidateLink(Link link) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(link.Name), Parameter: nameof(Link.Name)),
                (Rule: IsInvalid(link.Url), Parameter: nameof(Link.Url)),
                (Rule: IsInvalid(link.LinkType), Parameter: nameof(Link.LinkType)));

        private static void ValidateLinkOnModify(Link link) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(link.Id), Parameter: nameof(Link.Id)),
                (Rule: IsInvalid(link.Name), Parameter: nameof(Link.Name)),
                (Rule: IsInvalid(link.Url), Parameter: nameof(Link.Url)),
                (Rule: IsInvalid(link.LinkType), Parameter: nameof(Link.LinkType)));

        private static void ValidateLinkIdOnRetrieve(Guid linkId) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(linkId), Parameter: nameof(Link.Id)));

        private static void ValidateGroupIdOnRetrieve(Guid groupId) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(groupId), Parameter: nameof(Link.GroupId)));

        private static void ValidateLinkIdOnRemove(Guid linkId) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(linkId), Parameter: nameof(Link.Id)));

        private static dynamic IsInvalid(Guid id) => new
        {
            Condition = id == Guid.Empty,
            Message = "Id is required"
        };

        private static dynamic IsInvalid(string text) => new
        {
            Condition = string.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidLinkProcessingException =
                new InvalidLinkProcessingException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidLinkProcessingException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidLinkProcessingException.ThrowIfContainsErrors();
        }
    }
}
