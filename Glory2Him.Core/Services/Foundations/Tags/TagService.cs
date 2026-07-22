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
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    /// <summary>
    /// Foundation service for tags. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    public partial class TagService : ITagService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeFactory eventEnvelopeFactory;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public TagService(
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

        public ValueTask<Tag> AddTagAsync(
            Tag tag,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateTagIsNotNull(tag);

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: tag);

                return await DoAddTagAsync(
                    tag: tag,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<Tag>> RetrieveAllTagsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllTagsAsync(cancellationToken);
            });

        public ValueTask<Tag> RetrieveTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveTagById(tagId);

                Tag maybeTag =
                    await this.storageBroker.SelectTagByIdAsync(tagId, cancellationToken);

                ValidateStorageTag(maybeTag, tagId);

                return maybeTag;
            });

        public ValueTask<Tag> ModifyTagAsync(
            Tag tag,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateTagIsNotNull(tag);

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: tag);

                return await DoModifyTagAsync(
                    tag: tag,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Tag> RemoveTagByIdAsync(
            Guid tagId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new Tag
                {
                    Id = tagId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: removeRequest);

                return await DoRemoveTagByIdAsync(
                    tagId: tagId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<Tag> HardRemoveTagByIdAsync(
            Guid tagId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new Tag
                {
                    Id = tagId
                };

                EventEnvelope<Tag> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveTagByIdAsync(
                    tagId: tagId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<Tag> DoAddTagAsync(
            Tag tag,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            tag = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: tag, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddTagAsync(
                tag: tag,
                securityContext: inboundEnvelope.SecurityContext);

            Tag addedTag =
                await this.storageBroker.InsertTagAsync(tag, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Tag> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedTag);

            await this.eventBroker.PublishTagAsync(
                envelope: outboundEnvelope,
                operation: TagEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                cancellationToken: cancellationToken);

            return addedTag;
        }

        private async ValueTask<Tag> DoModifyTagAsync(
            Tag tag,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            tag = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: tag, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyTagAsync(
                tag: tag,
                securityContext: inboundEnvelope.SecurityContext);

            Tag maybeTag = await this.storageBroker.SelectTagByIdAsync(
                tagId: tag.Id,
                cancellationToken: cancellationToken);

            ValidateStorageTag(maybeTag, tagId: tag.Id);

            tag = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: tag,
                    storageEntity: maybeTag);

            ValidateAgainstStorageTagOnModify(
                inputTag: tag,
                storageTag: maybeTag);

            Tag updatedTag =
                await this.storageBroker.UpdateTagAsync(tag, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Tag> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedTag);

            await this.eventBroker.PublishTagAsync(
                envelope: outboundEnvelope,
                operation: TagEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedTag;
        }

        private async ValueTask<Tag> DoRemoveTagByIdAsync(
            Guid tagId,
            string? deletionReason,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveTagById(tagId);

            Tag maybeTag =
                await this.storageBroker.SelectTagByIdAsync(tagId, cancellationToken);

            ValidateStorageTag(maybeTag, tagId);

            if (maybeTag.IsDeleted)
                return maybeTag;

            if (deletionReason is not null)
                maybeTag.DeletionReason = deletionReason;

            Tag auditedTag =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeTag,
                    securityContext: inboundEnvelope.SecurityContext);

            Tag removedTag = await this.storageBroker.UpdateTagAsync(
                tag: auditedTag,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnRemovingTagByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Tag> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedTag);

            await this.eventBroker.PublishTagAsync(
                envelope: outboundEnvelope,
                operation: TagEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnRemovingTagByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedTag;
        }

        private async ValueTask<Tag> DoHardRemoveTagByIdAsync(
            Guid tagId,
            EventEnvelope<Tag> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnHardRemoveTagById(tagId);

            Tag maybeTag =
                await this.storageBroker.SelectTagByIdAsync(tagId, cancellationToken);

            ValidateStorageTag(maybeTag, tagId);

            Tag deletedTag =
                await this.storageBroker.DeleteTagAsync(maybeTag, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnHardRemovingTagByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<Tag> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedTag);

            await this.eventBroker.PublishTagAsync(
                envelope: outboundEnvelope,
                operation: TagEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.TagOnHardRemovingTagByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedTag;
        }
    }
}
