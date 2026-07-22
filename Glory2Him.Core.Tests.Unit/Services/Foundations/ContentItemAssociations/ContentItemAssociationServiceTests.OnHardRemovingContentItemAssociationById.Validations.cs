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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingContentItemAssociationByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ContentItemAssociation>? nullEnvelope = null;

            var invalidContentItemAssociationEventException =
                new InvalidContentItemAssociationEventException(
                    message: "Invalid content item association event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationEventException);

            // when
            ValueTask<EventEnvelope<ContentItemAssociation>?> onHardRemovingTask =
                this.contentItemAssociationService.OnHardRemovingContentItemAssociationByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingContentItemAssociationByIdEventWhenIdIsInvalidAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ContentItemAssociation>
            {
                Content = new ContentItemAssociation { Id = Guid.Empty },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidContentItemAssociationException = new InvalidContentItemAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.UpsertDataList(
                key: nameof(ContentItemAssociation.Id),
                value: "Id is required");

            var expectedContentItemAssociationValidationException = new ContentItemAssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers
                        .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<ContentItemAssociation>?> onHardRemovingTask =
                this.contentItemAssociationService.OnHardRemovingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers
                        .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingContentItemAssociationByIdEventWhenContentItemAssociationNotFoundAsync()
        {
            // given
            Guid someContentItemAssociationId = Guid.NewGuid();
            ContentItemAssociation noContentItemAssociation = null!;

            var requestEnvelope = new EventEnvelope<ContentItemAssociation>
            {
                Content = new ContentItemAssociation { Id = someContentItemAssociationId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundContentItemAssociationException = new NotFoundContentItemAssociationException(
                message: $"Content item association not found with id: {someContentItemAssociationId}.");

            var expectedContentItemAssociationValidationException = new ContentItemAssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: notFoundContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers
                        .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noContentItemAssociation);

            // when
            ValueTask<EventEnvelope<ContentItemAssociation>?> onHardRemovingTask =
                this.contentItemAssociationService.OnHardRemovingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    onHardRemovingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
