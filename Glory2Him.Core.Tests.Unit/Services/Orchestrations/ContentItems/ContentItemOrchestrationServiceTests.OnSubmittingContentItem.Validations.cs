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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        public static TheoryData<EventEnvelope<ContentItem>?> InvalidEventEnvelopes() =>
            new TheoryData<EventEnvelope<ContentItem>?>
            {
                null,

                new EventEnvelope<ContentItem>
                {
                    Content = null!,
                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                },

                new EventEnvelope<ContentItem>
                {
                    Content = new ContentItem { Id = Guid.NewGuid() },
                    Metadata = null!
                }
            };

        [Theory]
        [MemberData(nameof(InvalidEventEnvelopes))]
        public async Task ShouldThrowValidationExceptionOnSubmittingContentItemEventIfEnvelopeIsInvalidAndLogItAsync(
            EventEnvelope<ContentItem>? invalidEnvelope)
        {
            // given
            var invalidContentItemOrchestrationEventException =
                new InvalidContentItemOrchestrationEventException(
                    message: "Invalid content item submission event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemOrchestrationEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onSubmittingTask =
                this.contentItemOrchestrationService.OnSubmittingContentItemAsync(
                    invalidEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    onSubmittingTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeFactoryMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmittingContentItemEventIfCallerIsNotAuthenticatedAndLogItAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: new SecurityContext { IsAuthenticated = false });

            var unauthorizedContentItemOrchestrationException =
                new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is not authenticated.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemOrchestrationException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onSubmittingTask =
                this.contentItemOrchestrationService.OnSubmittingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    onSubmittingTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeFactoryMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldThrowValidationExceptionOnSubmittingContentItemEventIfCallerHasBlockRoleAndLogItAsync(
            string blockRole)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext(blockRole));

            var unauthorizedContentItemOrchestrationException =
                new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is blocked from contributing content items.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemOrchestrationException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onSubmittingTask =
                this.contentItemOrchestrationService.OnSubmittingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    onSubmittingTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeFactoryMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
