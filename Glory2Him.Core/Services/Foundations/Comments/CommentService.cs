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
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;

namespace Glory2Him.Core.Services.Foundations.Comments
{
    /// <summary>
    /// Foundation service for comments. Every operation is both callable directly (the
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
    internal partial class CommentService : ICommentService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public CommentService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            ISecurityAuditBroker securityAuditBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<Comment> AddCommentAsync(
            Comment comment,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateCommentIsNotNull(comment);

                EventEnvelope<Comment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: comment);

                return await DoAddCommentAsync(
                    comment: comment,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<Comment>> RetrieveAllCommentsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // the envelope exists to capture the ambient security context the
                // visibility filter runs against — the request payload is empty
                EventEnvelope<Comment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new Comment());

                IQueryable<Comment> allComments =
                    await this.storageBroker.SelectAllCommentsAsync(cancellationToken);

                return await ApplyCollectionReadVisibilityFilterAsync(
                    comments: allComments,
                    securityContext: envelope.SecurityContext);
            });

        public ValueTask<Comment> RetrieveCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var retrieveRequest = new Comment
                {
                    Id = commentId
                };

                EventEnvelope<Comment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: retrieveRequest);

                return await DoRetrieveCommentByIdAsync(
                    commentId: commentId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Comment> ModifyCommentAsync(
            Comment comment,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateCommentIsNotNull(comment);

                EventEnvelope<Comment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: comment);

                return await DoModifyCommentAsync(
                    comment: comment,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Comment> RemoveCommentByIdAsync(
            Guid commentId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new Comment
                {
                    Id = commentId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<Comment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: removeRequest);

                return await DoRemoveCommentByIdAsync(
                    commentId: commentId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Comment> HardRemoveCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new Comment
                {
                    Id = commentId
                };

                EventEnvelope<Comment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveCommentByIdAsync(
                    commentId: commentId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        // the shared read posture of design §14.1/§14.5/§14.6: a publicly visible version
        // is readable by anyone; a non-public version answers not-found — never
        // unauthorized — to everyone but the owner and the review roles, with the true
        // denial reason logged server-side only
        private async ValueTask<Comment> DoRetrieveCommentByIdAsync(
            Guid commentId,
            EventEnvelope<Comment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRetrieveCommentById(commentId);

            Comment maybeComment = await this.storageBroker.SelectCommentByIdAsync(
                commentId: commentId,
                cancellationToken: cancellationToken);

            ValidateStorageComment(maybeComment, commentId);

            if (maybeComment.IsDeleted)
            {
                await this.loggingBroker.LogInformationAsync(
                    message: $"Comment read denied. Comment {commentId} is " +
                        "soft-deleted; reported to the caller as not found.");

                throw new NotFoundCommentException(
                    message: $"Comment not found with id: {commentId}.");
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            bool isPubliclyVisible =
                maybeComment.ApprovalStatus == ApprovalStatus.Approved
                    && maybeComment.IsPublished
                    && (maybeComment.PublishDate is null
                        || maybeComment.PublishDate <= currentDateTime);

            if (isPubliclyVisible)
            {
                return maybeComment;
            }

            SecurityContext? securityContext = inboundEnvelope.SecurityContext;

            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Comment read denied. Comment {commentId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found.");

                throw new NotFoundCommentException(
                    message: $"Comment not found with id: {commentId}.");
            }

            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(
                securityContext: securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && maybeComment.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                await this.loggingBroker.LogWarningAsync(
                    message: $"Comment read denied. Comment {commentId} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found.");

                throw new NotFoundCommentException(
                    message: $"Comment not found with id: {commentId}.");
            }

            return maybeComment;
        }

        // the collection twin of the single-row posture: a row the caller may not see
        // drops out of the set instead of erroring, so a collection read never reveals
        // how many non-public rows exist
        private async ValueTask<IQueryable<Comment>> ApplyCollectionReadVisibilityFilterAsync(
            IQueryable<Comment> comments,
            SecurityContext? securityContext)
        {
            IQueryable<Comment> visibleComments = comments.Where(comment =>
                comment.IsDeleted == false);

            bool isAuthenticated =
                securityContext is not null && securityContext.IsAuthenticated;

            if (isAuthenticated && HasReviewRole(securityContext!))
            {
                return visibleComments;
            }

            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            string? actorUserId = isAuthenticated
                ? await this.securityAuditBroker.GetUserIdAsync(securityContext: securityContext!)
                : null;

            bool includeOwnComments = string.IsNullOrWhiteSpace(actorUserId) is false;

            return visibleComments.Where(comment =>
                (comment.ApprovalStatus == ApprovalStatus.Approved
                    && comment.IsPublished
                    && (comment.PublishDate == null
                        || comment.PublishDate <= currentDateTime))
                || (includeOwnComments && comment.CreatedBy == actorUserId));
        }

        private async ValueTask<Comment> DoAddCommentAsync(
            Comment comment,
            EventEnvelope<Comment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            comment = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: comment, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddCommentAsync(
                comment: comment,
                securityContext: inboundEnvelope.SecurityContext);

            Comment addedComment =
                await this.storageBroker.InsertCommentAsync(comment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Comment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedComment);

            await this.eventBroker.PublishCommentAsync(
                envelope: outboundEnvelope,
                operation: CommentEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName,
                cancellationToken: cancellationToken);

            return addedComment;
        }

        private async ValueTask<Comment> DoModifyCommentAsync(
            Comment comment,
            EventEnvelope<Comment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);

            comment = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: comment, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyCommentAsync(
                comment: comment,
                securityContext: inboundEnvelope.SecurityContext);

            Comment maybeComment = await this.storageBroker.SelectCommentByIdAsync(
                commentId: comment.Id,
                cancellationToken: cancellationToken);

            ValidateStorageComment(maybeComment, commentId: comment.Id);

            await ValidateUserCanModifyStorageCommentAsync(
                storageComment: maybeComment,
                securityContext: inboundEnvelope.SecurityContext);

            comment = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: comment,
                    storageEntity: maybeComment);

            ValidateAgainstStorageCommentOnModify(
                inputComment: comment,
                storageComment: maybeComment);

            Comment updatedComment =
                await this.storageBroker.UpdateCommentAsync(comment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.CommentOnModifyingCommentSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Comment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedComment);

            await this.eventBroker.PublishCommentAsync(
                envelope: outboundEnvelope,
                operation: CommentEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.CommentOnModifyingCommentSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedComment;
        }

        private async ValueTask<Comment> DoRemoveCommentByIdAsync(
            Guid commentId,
            string? deletionReason,
            EventEnvelope<Comment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
            ValidateOnRemoveCommentById(commentId);

            Comment maybeComment =
                await this.storageBroker.SelectCommentByIdAsync(commentId, cancellationToken);

            ValidateStorageComment(maybeComment, commentId);

            // permission comes before the idempotent short-circuit, so an unauthorized
            // caller learns nothing about the row's deletion state
            await ValidateUserCanRemoveStorageCommentAsync(
                storageComment: maybeComment,
                securityContext: inboundEnvelope.SecurityContext);

            if (maybeComment.IsDeleted)
                return maybeComment;

            if (deletionReason is not null)
                maybeComment.DeletionReason = deletionReason;

            Comment auditedComment =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeComment,
                    securityContext: inboundEnvelope.SecurityContext);

            Comment removedComment = await this.storageBroker.UpdateCommentAsync(
                comment: auditedComment,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Comment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedComment);

            await this.eventBroker.PublishCommentAsync(
                envelope: outboundEnvelope,
                operation: CommentEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedComment;
        }

        private async ValueTask<Comment> DoHardRemoveCommentByIdAsync(
            Guid commentId,
            EventEnvelope<Comment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserCanHardRemoveComment(inboundEnvelope.SecurityContext);
            ValidateOnHardRemoveCommentById(commentId);

            Comment maybeComment =
                await this.storageBroker.SelectCommentByIdAsync(commentId, cancellationToken);

            ValidateStorageComment(maybeComment, commentId);

            Comment deletedComment =
                await this.storageBroker.DeleteCommentAsync(maybeComment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Comment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedComment);

            await this.eventBroker.PublishCommentAsync(
                envelope: outboundEnvelope,
                operation: CommentEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedComment;
        }
    }
}
