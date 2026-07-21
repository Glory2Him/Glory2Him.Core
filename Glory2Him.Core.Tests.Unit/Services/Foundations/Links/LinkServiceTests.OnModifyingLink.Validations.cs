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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyingLinkEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Link>? nullEnvelope = null;

            var invalidLinkEventException =
                new InvalidLinkEventException(
                    message: "Invalid link event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkEventException);

            // when
            ValueTask<EventEnvelope<Link>?> onModifyingTask =
                this.linkService.OnModifyingLinkAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    onModifyingTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyingLinkEventWhenLinkNotFoundAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Link inputLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link noLink = null!;

            var requestEnvelope = new EventEnvelope<Link>
            {
                Content = inputLink,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundLinkException = new NotFoundLinkException(
                message: $"Link not found with id: {inputLink.Id}.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: notFoundLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnModifyingLinkSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputLink);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    inputLink.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noLink);

            // when
            ValueTask<EventEnvelope<Link>?> onModifyingTask =
                this.linkService.OnModifyingLinkAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    onModifyingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    inputLink.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
