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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        // ── OnSubmitting ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldSubmitOnSubmittingContentItemEventAsync()
        {
            // given: the event path carries the id in the envelope; the do-work reads only the
            // id off it and drives the row Draft -> Submitted, exactly as the direct path does
            ContentItem storageContentItem = CreateSubmittableStorageContentItem();

            var requestEnvelope = new EventEnvelope<ContentItem>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ContentItem { Id = storageContentItem.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

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
            EventEnvelope<ContentItem>? actualReplyEnvelope =
                await this.contentItemService.OnSubmittingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipSubmitAndReplyNullWhenSubmittingContentItemEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ContentItem>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ContentItem { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnSubmittingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ContentItem>? actualReplyEnvelope =
                await this.contentItemService.OnSubmittingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnSubmittingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmittingContentItemEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ContentItem>? nullEnvelope = null;

            var invalidContentItemEventException =
                new InvalidContentItemEventException(
                    message: "Invalid content item event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onSubmittingTask =
                this.contentItemService.OnSubmittingContentItemAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    onSubmittingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        // ── OnApproving ──────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldApproveOnApprovingContentItemEventAsync()
        {
            // given
            ContentItem storageContentItem = CreateApprovableStorageContentItem();

            var requestEnvelope = new EventEnvelope<ContentItem>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers),
                Content = CreateApprovalDecision(storageContentItem.Id),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupContentItemStorageRead(storageContentItem);
            SetupAccessBrokerToPermit();

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
            EventEnvelope<ContentItem>? actualReplyEnvelope =
                await this.contentItemService.OnApprovingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Approved),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipApproveAndReplyNullWhenApprovingContentItemEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ContentItem>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers),
                Content = CreateApprovalDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnApprovingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ContentItem>? actualReplyEnvelope =
                await this.contentItemService.OnApprovingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: a duplicate approve neither re-decides nor re-announces
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemOnApprovingContentItemSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApprovingContentItemEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ContentItem>? nullEnvelope = null;

            var invalidContentItemEventException =
                new InvalidContentItemEventException(
                    message: "Invalid content item event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onApprovingTask =
                this.contentItemService.OnApprovingContentItemAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    onApprovingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
