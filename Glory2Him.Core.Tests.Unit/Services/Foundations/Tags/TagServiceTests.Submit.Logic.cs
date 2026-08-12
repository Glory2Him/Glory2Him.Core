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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldSubmitTagByOwnerAsync()
        {
            // given: the owner submitting their own draft — no moderation role required
            Tag storageTag = CreateSubmittableStorageTag();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Tag submittedTag = storageTag.DeepClone();
            submittedTag.ApprovalStatus = ApprovalStatus.Submitted;

            Tag auditAppliedTag = submittedTag.DeepClone();
            Tag updatedTag = auditAppliedTag.DeepClone();
            Tag expectedTag = updatedTag.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            SetupTagStorageRead(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    auditAppliedTag,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Submitted))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(
                            new EventPublishResult<Tag>()));

            // when
            Tag actualTag =
                await this.tagService.SubmitTagByIdAsync(
                    storageTag.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualTag.Should().BeEquivalentTo(expectedTag);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
                        storageTag.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<Tag>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        auditAppliedTag,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Submitted),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .TagOnSubmittingTagSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            // submit never consults the cross-entity decision — that is the approve's gate
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSubmitTagByPublisherWhoIsNotTheOwnerAsync()
        {
            // given: the publisher tier may move a submission status too — the same set the §9.2
            // modify carve-out admits. The caller is NOT the owner, so this proves the
            // publisher-tier branch rather than the ownership branch.
            Tag storageTag = CreateSubmittableStorageTag();

            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"someone-else-{Guid.NewGuid()}");

            SetupTagStorageRead(storageTag);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Tag entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Tag entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    It.IsAny<TagEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(
                            new EventPublishResult<Tag>()));

            // when
            await this.tagService.SubmitTagByIdAsync(
                storageTag.Id,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheStatusFieldOnSubmitAsync()
        {
            // given: submit owns ONLY the approval status. It drives Draft -> Submitted and must
            // leave every other field exactly as stored — a content edit is the general modify's
            // job, not submit's. Asserting the whole row against the pre-act snapshot, excluding
            // only the one field submit owns, catches any stray write.
            Tag storageTag = CreateSubmittableStorageTag();
            Tag expectedStorageTag = storageTag.DeepClone();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageTag.CreatedBy);

            // when
            Tag savedTag = await CaptureSavedTagOnSubmitAsync(storageTag);

            // then
            savedTag.Should().NotBeNull();
            savedTag.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            savedTag.Should().BeEquivalentTo(
                expectedStorageTag,
                options => options.Excluding(tag => tag.ApprovalStatus));
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnSubmitAsync()
        {
            // given: like every transition, submit publishes its own fact and never Modified —
            // the approval workflow's cycle-breaker (design §9.7.1, issue #111 case 1).
            Tag storageTag = CreateSubmittableStorageTag();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageTag.CreatedBy);

            // when
            await CaptureSavedTagOnSubmitAsync(storageTag);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Submitted),
                Times.Once);
        }

        // Runs a permitted submit end to end (owner already set up by the caller) and hands back
        // a snapshot of the row that reached the storage broker.
        private async ValueTask<Tag> CaptureSavedTagOnSubmitAsync(Tag storageTag)
        {
            Tag savedTag = null;

            SetupTagStorageRead(storageTag);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Tag entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Tag, CancellationToken>(
                            (entity, _) => savedTag = entity.DeepClone())
                        .ReturnsAsync((Tag entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    It.IsAny<TagEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(
                            new EventPublishResult<Tag>()));

            await this.tagService.SubmitTagByIdAsync(
                storageTag.Id,
                TestContext.Current.CancellationToken);

            return savedTag;
        }
    }
}
