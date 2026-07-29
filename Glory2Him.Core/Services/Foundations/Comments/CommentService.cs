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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Comments;

namespace Glory2Him.Core.Services.Foundations.Comments
{
    /// <summary>
    /// Foundation service for comments. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
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

                return await this.storageBroker.SelectAllCommentsAsync(cancellationToken);
            });

        public ValueTask<Comment> RetrieveCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveCommentById(commentId);

                Comment maybeComment =
                    await this.storageBroker.SelectCommentByIdAsync(commentId, cancellationToken);

                ValidateStorageComment(maybeComment, commentId);

                return maybeComment;
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

        private async ValueTask<Comment> DoAddCommentAsync(
            Comment comment,
            EventEnvelope<Comment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
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
            comment = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: comment, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyCommentAsync(
                comment: comment,
                securityContext: inboundEnvelope.SecurityContext);

            Comment maybeComment = await this.storageBroker.SelectCommentByIdAsync(
                commentId: comment.Id,
                cancellationToken: cancellationToken);

            ValidateStorageComment(maybeComment, commentId: comment.Id);

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
            ValidateOnRemoveCommentById(commentId);

            Comment maybeComment =
                await this.storageBroker.SelectCommentByIdAsync(commentId, cancellationToken);

            ValidateStorageComment(maybeComment, commentId);

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
