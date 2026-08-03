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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingBibleReferenceByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<BibleReference>? nullEnvelope = null;

            var invalidBibleReferenceEventException =
                new InvalidBibleReferenceEventException(
                    message: "Invalid bible reference event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceEventException);

            // when
            ValueTask<EventEnvelope<BibleReference>?> onHardRemovingTask =
                this.bibleReferenceService.OnHardRemovingBibleReferenceByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingBibleReferenceByIdEventWhenIdIsInvalidAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<BibleReference>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin),
                Content = new BibleReference { Id = Guid.Empty },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidBibleReferenceException = new InvalidBibleReferenceException(
                message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.UpsertDataList(
                key: nameof(BibleReference.Id),
                value: "Id is required");

            var expectedBibleReferenceValidationException = new BibleReferenceValidationException(
                message: "Bible reference validation error occurred, fix the errors and try again.",
                innerException: invalidBibleReferenceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<BibleReference>?> onHardRemovingTask =
                this.bibleReferenceService.OnHardRemovingBibleReferenceByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingBibleReferenceByIdEventWhenBibleReferenceNotFoundAsync()
        {
            // given
            Guid someBibleReferenceId = Guid.NewGuid();
            BibleReference noBibleReference = null!;

            var requestEnvelope = new EventEnvelope<BibleReference>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin),
                Content = new BibleReference { Id = someBibleReferenceId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundBibleReferenceException = new NotFoundBibleReferenceException(
                message: $"Bible reference not found with id: {someBibleReferenceId}.");

            var expectedBibleReferenceValidationException = new BibleReferenceValidationException(
                message: "Bible reference validation error occurred, fix the errors and try again.",
                innerException: notFoundBibleReferenceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noBibleReference);

            // when
            ValueTask<EventEnvelope<BibleReference>?> onHardRemovingTask =
                this.bibleReferenceService.OnHardRemovingBibleReferenceByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    onHardRemovingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
