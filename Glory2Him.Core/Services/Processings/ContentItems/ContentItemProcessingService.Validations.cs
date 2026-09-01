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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Processings.ContentItems
{
    internal partial class ContentItemProcessingService
    {
        private static void ValidateOnAddContentItem(
            ContentItem contentItem,
            SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);
            ValidateContentItemIsNotNull(contentItem);
            ValidateUserIsNotBlockedFromContentType(securityContext, contentItem.ContentType);
            ValidateContentItem(contentItem);
        }

        private static void ValidateOnModifyContentItem(
            ContentItem contentItem,
            SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);
            ValidateContentItemIsNotNull(contentItem);

            // The item under write, which on this pre-load gate is the caller's copy. It is the
            // coarse half of the answer: the stored row's own type is asked again in
            // ValidateCurrentContentItemIsModifiable below, once it has been read, so a blocked
            // caller relabelling their edit is refused there rather than admitted here.
            ValidateUserIsNotBlockedFromContentType(securityContext, contentItem.ContentType);
            ValidateContentItemOnModify(contentItem);
        }

        private static void ValidateOnRemoveContentItemById(
            Guid contentItemId,
            SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);
            ValidateContentItemIdOnRemove(contentItemId);
        }

        private static void ValidateCurrentContentItemIsRemovable(
            ContentItem currentContentItem,
            string actorUserId,
            SecurityContext securityContext)
        {
            // a remove is idempotent from the caller's point of view, but an already
            // removed row must never be presented as a fresh removal
            if (currentContentItem.IsDeleted)
            {
                throw new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");
            }

            // The veto, against the STORED type and ahead of both branches below. The remove
            // path is handed an id, so this is the first point at which the narrow block can be
            // composed at all — and it covers the holder's own rows, Administrators included
            // (design §18.6 rule 2).
            ValidateUserIsNotBlockedFromContentType(
                securityContext,
                currentContentItem.ContentType);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && currentContentItem.CreatedBy == actorUserId;

            // removing content is a takedown, not a moderation step — the owner may remove
            // their own item and an administrator may remove anyone's; Reviewers and Publishers
            // moderate through the approval workflow instead
            bool isPermitted = isOwner || securityContext.Roles.Contains(Roles.Administrators);

            if (isPermitted is false)
            {
                throw new UnauthorizedContentItemProcessingException(
                    message: "The current user is not allowed to remove this content item.");
            }
        }

        private static void ValidateCurrentContentItemIsModifiable(
            ContentItem currentContentItem,
            bool isLatestVersion,
            string actorUserId,
            SecurityContext securityContext)
        {
            if (currentContentItem.IsDeleted)
            {
                throw new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");
            }

            if (isLatestVersion is false)
            {
                throw new InvalidContentItemProcessingException(
                    message: "Only the latest version of a content item may be modified.");
            }

            // The veto, against the STORED type. ContentType is create-only (§12.4.1 rule 7a),
            // so the pre-load gate's answer came off the caller's copy, and this is what refuses
            // a blocked contributor who relabelled their edit as a type they are free on. It
            // runs ahead of the owner branch: the block covers the holder's own rows.
            ValidateUserIsNotBlockedFromContentType(
                securityContext,
                currentContentItem.ContentType);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && currentContentItem.CreatedBy == actorUserId;

            // a not-yet-decided item may be corrected in place by a holder of Reviewers, Publishers or
            // Administrators during review; a terminal one belongs to its owner alone, because the
            // only edit it admits is a fork onto a fresh version (§3.4 rule 16) and a
            // moderator forking someone else's decided row would author a version in
            // their name
            bool hasModifyRole =
                securityContext.Roles.Contains(Roles.Reviewers)
                    || securityContext.Roles.Contains(Roles.ContentItemReviewers)
                    || securityContext.Roles.Contains(Roles.Publishers)
                    || securityContext.Roles.Contains(Roles.ContentItemPublishers)
                    || securityContext.Roles.Contains(Roles.Administrators);

            bool isTerminal =
                currentContentItem.ApprovalStatus == ApprovalStatus.Approved
                    || currentContentItem.ApprovalStatus == ApprovalStatus.Rejected;

            bool isPermitted = isTerminal
                ? isOwner
                : isOwner || hasModifyRole;

            if (isPermitted is false)
            {
                throw new UnauthorizedContentItemProcessingException(
                    message: "The current user is not allowed to modify this content item.");
            }
        }

        // instance and async, unlike its sibling validations: the caller-facing error is a
        // deliberately reason-free not-found (no existence leak), so this is the only place
        // the true denial reason can be recorded — server-side, never on the exception
        private async ValueTask ValidateCurrentContentItemIsRetrievableAsync(
            ContentItem currentContentItem,
            string actorUserId,
            SecurityContext securityContext)
        {
            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && currentContentItem.CreatedBy == actorUserId;

            // a non-public version may be read by its owner and by the moderation roles for
            // this content type (§16.6, §18.6) for review and audit; everyone else gets
            // not-found so a probe cannot tell a non-public version from a missing one
            bool isPermitted =
                isOwner || HasReviewRole(securityContext, currentContentItem.ContentType);

            if (isPermitted is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Content item read denied. Content item {currentContentItem.Id} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");
            }
        }

        // ContentItem is the one entity type with three role tiers rather than two, because
        // it is the only one carrying a ContentType (design §18.6 rule 5). The tiers widen
        // from narrow to broad — ContentItem-Story-Reviewers ⊂ ContentItem-Reviewers ⊂ Reviewers
        // — and rule 4 binds both directions: holding ANY of them satisfies a check for that
        // content type, and the narrow role NEVER satisfies a check for a different one.

        // the broad tiers, which cover every content type at once and so need no per-row
        // question asked of them
        private static bool HasBroadReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewers)
                || securityContext.Roles.Contains(Roles.ContentItemReviewers)
                || securityContext.Roles.Contains(Roles.Publishers)
                || securityContext.Roles.Contains(Roles.ContentItemPublishers)
                || securityContext.Roles.Contains(Roles.Administrators);

        // the narrow tier: authority over one content type and never over another
        private static bool HasContentTypeReviewRole(
            SecurityContext securityContext,
            ContentType contentType) =>
            securityContext.Roles.Contains(
                    Roles.ReviewersFor(EntityType.ContentItem, contentType))
                || securityContext.Roles.Contains(
                    Roles.PublishersFor(EntityType.ContentItem, contentType));

        // the moderation roles that may read non-public versions of THIS content type for
        // review and audit (§16.6, §18.6)
        private static bool HasReviewRole(
            SecurityContext securityContext,
            ContentType contentType) =>
            HasBroadReviewRole(securityContext)
                || HasContentTypeReviewRole(securityContext, contentType);

        // The content types a narrow-tier caller may review. A collection filter is a
        // queryable predicate and cannot call a role check per row, so the caller's narrow
        // grants are resolved once, here, into a set the predicate can test membership of.
        private static ContentType[] ReviewableContentTypes(SecurityContext securityContext) =>
            Enum.GetValues<ContentType>()
                .Where(contentType => HasContentTypeReviewRole(securityContext, contentType))
                .ToArray();

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedContentItemProcessingException(
                    message: "The current user is not authenticated.");
            }

            bool isBlocked =
                securityContext.Roles.Contains(Roles.ReadOnly)
                    || securityContext.Roles.Contains(Roles.ContentItemReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedContentItemProcessingException(
                    message: "The current user is blocked from contributing content items.");
            }
        }

        // The veto at its narrowest scope, and no grant answers it — not
        // ContentItem-Quote-Publishers, not ContentItem-Publishers, not Publishers, not
        // Administrators, and not the owner. Grants widen upward (§18.6 rule 4); blocks are
        // absolute downward within the scope they cover, and silent outside it, so a
        // ContentItem-Quote-ReadOnly holder writes stories exactly as before (rule 2).
        private static void ValidateUserIsNotBlockedFromContentType(
            SecurityContext securityContext,
            ContentType contentType)
        {
            bool isBlocked = securityContext.Roles.Contains(
                Roles.ReadOnlyFor(EntityType.ContentItem, contentType));

            if (isBlocked)
            {
                throw new UnauthorizedContentItemProcessingException(
                    message: "The current user is blocked from contributing content items.");
            }
        }

        private static void ValidateContentItemIsNotNull(ContentItem contentItem)
        {
            if (contentItem is null)
            {
                throw new NullContentItemProcessingException(message: "Content item is null.");
            }
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The processing service is an
        // event receiver too: it front-loads the contribution / owner / Administrators decision against the
        // inbound envelope's SecurityContext, so without this check a caller who can put a message
        // on a ContentItemProcessing address states their own roles and is believed (design
        // §14.6 rule 4). Verification sits in the receiver, not the transport, because a handler is
        // reachable without going through the broker.
        private async ValueTask ValidateContentItemEventEnvelopeAsync(
            EventEnvelope<ContentItem> envelope,
            ContentItemProcessingEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidContentItemProcessingEventException(
                    message: "Invalid content item processing event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"ContentItemProcessing{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidContentItemProcessingEventException(
                    message: "Invalid content item processing event. " +
                        "Integrity verification failed.");
            }
        }

        private static void ValidateContentItem(ContentItem contentItem) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItem.ContentType), Parameter: nameof(ContentItem.ContentType)),
                (Rule: IsInvalid(contentItem.Content), Parameter: nameof(ContentItem.Content)));

        private static void ValidateContentItemOnModify(ContentItem contentItem) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItem.Id), Parameter: nameof(ContentItem.Id)),
                (Rule: IsInvalid(contentItem.ContentType), Parameter: nameof(ContentItem.ContentType)),
                (Rule: IsInvalid(contentItem.Content), Parameter: nameof(ContentItem.Content)));

        private static void ValidateContentItemIdOnRetrieve(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

        private static void ValidateGroupIdOnRetrieve(Guid groupId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(groupId), Parameter: nameof(ContentItem.GroupId)));

        private static void ValidateContentItemIdOnRemove(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));

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

        // structural validation for an enum crossing a boundary — rejects an out-of-range
        // value; it cannot detect "caller forgot to set it", since ContentType has no
        // unset sentinel
        private static dynamic IsInvalid(ContentType contentType) => new
        {
            Condition = Enum.IsDefined(contentType) == false,
            Message = "Value is not a supported content type"
        };

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidContentItemProcessingException =
                new InvalidContentItemProcessingException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidContentItemProcessingException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidContentItemProcessingException.ThrowIfContainsErrors();
        }
    }
}
