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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    internal partial class ContentItemOrchestrationService
    {
        private static void ValidateOnAddContentItem(
            ContentItem contentItem,
            SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);
            ValidateContentItem(contentItem);
        }

        private static void ValidateOnModifyContentItem(
            ContentItem contentItem,
            SecurityContext securityContext)
        {
            ValidateUserIsAllowedToContribute(securityContext);
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
                throw new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");
            }

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && currentContentItem.CreatedBy == actorUserId;

            // removing content is a takedown, not a moderation step — the owner may remove
            // their own item and an Admin may remove anyone's; Reviewers and Publishers
            // moderate through the approval workflow instead
            bool isPermitted = isOwner || securityContext.Roles.Contains(Roles.Admin);

            if (isPermitted is false)
            {
                throw new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is not allowed to remove this content item.");
            }
        }

        private static void ValidateCurrentContentItemIsModifiable(
            ContentItem currentContentItem,
            string actorUserId,
            SecurityContext securityContext)
        {
            if (currentContentItem.IsDeleted)
            {
                throw new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");
            }

            if (currentContentItem.IsLatestVersion is false)
            {
                throw new InvalidContentItemOrchestrationException(
                    message: "Only the latest version of a content item may be modified.");
            }

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && currentContentItem.CreatedBy == actorUserId;

            // an approved item belongs to its owner alone — the modify then forks a new
            // version; a not-yet-approved item may also be modified in place by a
            // Reviewer, Publisher or Admin
            bool hasModifyRole =
                securityContext.Roles.Contains(Roles.Reviewer)
                    || securityContext.Roles.Contains(Roles.ContentItemReviewer)
                    || securityContext.Roles.Contains(Roles.Publisher)
                    || securityContext.Roles.Contains(Roles.ContentItemPublisher)
                    || securityContext.Roles.Contains(Roles.Admin);

            bool isPermitted = currentContentItem.ApprovalStatus == ApprovalStatus.Approved
                ? isOwner
                : isOwner || hasModifyRole;

            if (isPermitted is false)
            {
                throw new UnauthorizedContentItemOrchestrationException(
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

            // a non-public version may be read by its owner and by the moderation roles
            // (Reviewer, Publisher, Admin — global or ContentItem-scoped, §16.6) for
            // review and audit; everyone else gets not-found so a probe cannot tell a
            // non-public version from a missing one
            bool isPermitted = isOwner || HasReviewRole(securityContext);

            if (isPermitted is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Content item read denied. Content item {currentContentItem.Id} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");
            }
        }

        // the moderation roles that may read non-public versions for review and audit
        // (Reviewer, Publisher, Admin — global or ContentItem-scoped, §16.6)
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.ContentItemReviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.ContentItemPublisher)
                || securityContext.Roles.Contains(Roles.Admin);

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is not authenticated.");
            }

            bool isBlocked =
                securityContext.Roles.Contains(Roles.ReadOnly)
                    || securityContext.Roles.Contains(Roles.ContentItemReadOnly);

            if (isBlocked)
            {
                throw new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is blocked from contributing content items.");
            }
        }

        private static void ValidateContentItemIsNotNull(ContentItem contentItem)
        {
            if (contentItem is null)
            {
                throw new NullContentItemOrchestrationException(message: "Content item is null.");
            }
        }

        private static void ValidateContentItemEventEnvelope(EventEnvelope<ContentItem> envelope)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidContentItemOrchestrationEventException(
                    message: "Invalid content item orchestration event. " +
                        "The event envelope, its content and metadata are required.");
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

        private static void ValidateContentItemGroupIdOnRetrieve(Guid contentItemGroupId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemGroupId), Parameter: nameof(ContentItem.ContentItemGroupId)));

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
            var invalidContentItemOrchestrationException =
                new InvalidContentItemOrchestrationException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidContentItemOrchestrationException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidContentItemOrchestrationException.ThrowIfContainsErrors();
        }
    }
}
