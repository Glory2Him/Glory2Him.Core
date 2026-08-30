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
        public async Task ShouldModifyTagAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag inputTag = randomTag;
            Tag auditAppliedTag = inputTag.DeepClone();
            Tag storageTag = auditAppliedTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            Tag auditPreservedTag = auditAppliedTag.DeepClone();
            Tag updatedTag = auditPreservedTag.DeepClone();
            Tag expectedTag = updatedTag.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    auditAppliedTag.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedTag,
                    storageTag))
                        .ReturnsAsync(auditPreservedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(auditPreservedTag, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<Tag>>(
                        new EventPublishResult<Tag>()));

            // when
            Tag actualTag =
                await this.tagService.ModifyTagAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            // then
            actualTag.Should().BeEquivalentTo(expectedTag);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
                        auditAppliedTag.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedTag,
                        storageTag),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(auditPreservedTag, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldModifyWhenOwnerMovesStatusToSubmittedAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag inputTag = randomTag;
            inputTag.ApprovalStatus = ApprovalStatus.Submitted;
            Tag auditAppliedTag = inputTag.DeepClone();
            Tag storageTag = auditAppliedTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageTag.ApprovalStatus = ApprovalStatus.Draft;
            Tag auditPreservedTag = auditAppliedTag.DeepClone();
            Tag updatedTag = auditPreservedTag.DeepClone();
            Tag expectedTag = updatedTag.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    auditAppliedTag.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedTag,
                    storageTag))
                        .ReturnsAsync(auditPreservedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(auditPreservedTag, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<Tag>>(
                        new EventPublishResult<Tag>()));

            // when
            Tag actualTag =
                await this.tagService.ModifyTagAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            // then
            actualTag.Should().BeEquivalentTo(expectedTag);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
                        auditAppliedTag.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedTag,
                        storageTag),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(auditPreservedTag, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldModifyWhenPublisherMovesStatusToSubmittedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.TagPublishers);
            string actorUserId = GetRandomString();
            string ownerUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, actorUserId);
            Tag inputTag = randomTag;
            inputTag.CreatedBy = ownerUserId;
            inputTag.ApprovalStatus = ApprovalStatus.Draft;
            Tag storageTag = inputTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            inputTag.ApprovalStatus = ApprovalStatus.Submitted;
            Tag auditAppliedTag = inputTag.DeepClone();
            Tag auditPreservedTag = auditAppliedTag.DeepClone();
            Tag updatedTag = auditPreservedTag.DeepClone();
            Tag expectedTag = updatedTag.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    auditAppliedTag.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedTag,
                    storageTag))
                        .ReturnsAsync(auditPreservedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(auditPreservedTag, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<Tag>>(
                        new EventPublishResult<Tag>()));

            // when
            Tag actualTag =
                await this.tagService.ModifyTagAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            // then
            actualTag.Should().BeEquivalentTo(expectedTag);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
                        auditAppliedTag.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedTag,
                        storageTag),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(auditPreservedTag, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
