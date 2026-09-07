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
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
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
        public async Task ShouldThrowValidationExceptionOnAddingContentItemEventIfEnvelopeIsInvalidAndLogItAsync(
            EventEnvelope<ContentItem>? invalidEnvelope)
        {
            // given
            var invalidContentItemProcessingEventException =
                new InvalidContentItemProcessingEventException(
                    message: "Invalid content item processing event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemProcessingService.OnAddingContentItemAsync(
                    invalidEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnAddingContentItemEventIfCallerIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: unauthenticatedSecurityContext!);

            var unauthorizedContentItemProcessingException =
                new UnauthorizedContentItemProcessingException(
                    message: "The current user is not authenticated.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemProcessingException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemProcessingService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldThrowValidationExceptionOnAddingContentItemEventIfCallerHasBlockRoleAndLogItAsync(
            string blockRole)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext(blockRole));

            var unauthorizedContentItemProcessingException =
                new UnauthorizedContentItemProcessingException(
                    message: "The current user is blocked from contributing content items.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemProcessingException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemProcessingService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A replayed or duplicated submission request lands on this path too, and the
        // duplicate-content rule still keeps it from ever creating twice — it just does so
        // quietly now (§3.4.2 rule 6), so the case is a logic test and lives in
        // OnAddingContentItem.Logic.cs rather than here.

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingContentItemEventWhenIntegrityVerificationFailsAndLogItAsync()
        {
            // given
            var forgedEnvelope = new EventEnvelope<ContentItem>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ContentItem { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            string expectedEventName =
                $"ContentItemProcessing{ContentItemProcessingEventOperation.Adding}";

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            var invalidContentItemProcessingEventException =
                new InvalidContentItemProcessingEventException(
                    message: "Invalid content item processing event. " +
                        "Integrity verification failed.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingEventException);

            // when
            ValueTask<EventEnvelope<ContentItem>?> onAddingTask =
                this.contentItemProcessingService.OnAddingContentItemAsync(
                    forgedEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    onAddingTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
