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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        [Theory]
        [MemberData(nameof(InvalidEventEnvelopes))]
        public async Task ShouldThrowValidationExceptionOnRetrievingContentItemByIdEventIfEnvelopeIsInvalidAndLogItAsync(
            EventEnvelope<ContentItem>? invalidEnvelope)
        {
            // given
            var invalidContentItemOrchestrationEventException =
                new InvalidContentItemOrchestrationEventException(
                    message: "Invalid content item orchestration event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemOrchestrationEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onRetrievingByIdTask =
                this.contentItemOrchestrationService.OnRetrievingContentItemByIdAsync(
                    invalidEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    onRetrievingByIdTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrievingContentItemByIdEventIfNonPublicAndCallerIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given: a forged or replayed request envelope with no authenticated caller
            // cannot read a non-public version — the visibility posture holds on the
            // event path too, and the answer stays not-found so nothing leaks
            Guid randomContentItemId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            var retrieveRequest = new ContentItem
            {
                Id = randomContentItemId
            };

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: randomContentItemId,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: GetRandomString());

            storageContentItem.IsPublished = false;

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: retrieveRequest,
                securityContext: unauthenticatedSecurityContext!);

            var notFoundContentItemOrchestrationException =
                new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemOrchestrationException);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(randomContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onRetrievingByIdTask =
                this.contentItemOrchestrationService.OnRetrievingContentItemByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    onRetrievingByIdTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(randomContentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            // the outward answer is reason-free, so the true denial reason must land in
            // the server-side log — the event path audits the same way as the direct one
            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Content item read denied. Content item {randomContentItemId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievingContentItemByIdEventIfContentItemIdIsInvalidAndLogItAsync()
        {
            // given: a request envelope whose payload carries no id selects nothing
            var retrieveRequest = new ContentItem
            {
                Id = Guid.Empty
            };

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: retrieveRequest,
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidContentItemOrchestrationException =
                new InvalidContentItemOrchestrationException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemOrchestrationException.AddData(
                key: nameof(ContentItem.Id),
                values: "Id is required");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemOrchestrationException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onRetrievingByIdTask =
                this.contentItemOrchestrationService.OnRetrievingContentItemByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    onRetrievingByIdTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
