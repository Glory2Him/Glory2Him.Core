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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldSubmitContentItemByOwnerAsync()
        {
            // given: the owner submitting their own draft — no moderation role required
            ContentItem storageContentItem = CreateSubmittableStorageContentItem();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem submittedContentItem = storageContentItem.DeepClone();
            submittedContentItem.ApprovalStatus = ApprovalStatus.Submitted;

            ContentItem auditAppliedContentItem = submittedContentItem.DeepClone();
            ContentItem updatedContentItem = auditAppliedContentItem.DeepClone();
            ContentItem expectedContentItem = updatedContentItem.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageContentItem.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            SetupContentItemStorageRead(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(
                    auditAppliedContentItem,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    ContentItemEventOperation.Submitted))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));

            // when
            ContentItem actualContentItem =
                await this.contentItemService.SubmitContentItemByIdAsync(
                    storageContentItem.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        storageContentItem.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        auditAppliedContentItem,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Submitted),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ContentItemOnSubmittingContentItemSubscriptionName),
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
        public async Task ShouldSubmitContentItemByPublisherWhoIsNotTheOwnerAsync()
        {
            // given: the publisher tier may move a submission status too — the same set the §9.2
            // modify carve-out admits. The caller is NOT the owner, so this proves the
            // publisher-tier branch rather than the ownership branch.
            ContentItem storageContentItem = CreateSubmittableStorageContentItem();

            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"someone-else-{Guid.NewGuid()}");

            SetupContentItemStorageRead(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ContentItem entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ContentItem entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));

            // when
            await this.contentItemService.SubmitContentItemByIdAsync(
                storageContentItem.Id,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheStatusFieldOnSubmitAsync()
        {
            // given: submit owns ONLY the approval status. It drives Draft -> Submitted and must
            // leave every other field exactly as stored — a content edit is the general modify's
            // job, not submit's.
            ContentItem storageContentItem = CreateSubmittableStorageContentItem();
            ContentItem expectedStorageContentItem = storageContentItem.DeepClone();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem savedContentItem = null;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageContentItem.CreatedBy);

            SetupContentItemStorageRead(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ContentItem entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ContentItem, CancellationToken>(
                            (entity, _) => savedContentItem = entity.DeepClone())
                        .ReturnsAsync((ContentItem entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));

            // when
            await this.contentItemService.SubmitContentItemByIdAsync(
                storageContentItem.Id,
                TestContext.Current.CancellationToken);

            // then
            savedContentItem.Should().NotBeNull();

            // the one field submit owns
            savedContentItem.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            // everything else is exactly as stored
            savedContentItem.Content.Should().Be(expectedStorageContentItem.Content);
            savedContentItem.Title.Should().Be(expectedStorageContentItem.Title);
            savedContentItem.ContentType.Should().Be(expectedStorageContentItem.ContentType);
            savedContentItem.CreatedBy.Should().Be(expectedStorageContentItem.CreatedBy);
            savedContentItem.IsPublished.Should().Be(expectedStorageContentItem.IsPublished);
            savedContentItem.PublishDate.Should().Be(expectedStorageContentItem.PublishDate);
            savedContentItem.IsApprovedByBypass.Should().Be(
                expectedStorageContentItem.IsApprovedByBypass);
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnSubmitAsync()
        {
            // given: like every transition, submit publishes its own fact and never Modified —
            // the approval workflow's cycle-breaker (design §9.7.1, issue #111 case 1).
            ContentItem storageContentItem = CreateSubmittableStorageContentItem();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageContentItem.CreatedBy);

            SetupContentItemStorageRead(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ContentItem entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ContentItem entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));

            // when
            await this.contentItemService.SubmitContentItemByIdAsync(
                storageContentItem.Id,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Submitted),
                Times.Once);
        }
    }
}
