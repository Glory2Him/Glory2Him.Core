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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingAssociationEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Association>? nullEnvelope = null;

            var invalidAssociationEventException =
                new InvalidAssociationEventException(
                    message: "Invalid content item association event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationEventException);

            // when
            ValueTask<EventEnvelope<Association>?> onAddingTask =
                this.associationService.OnAddingAssociationAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingAssociationEventWhenMetadataIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Association { Id = Guid.NewGuid() },
                Metadata = null!
            };

            var invalidAssociationEventException =
                new InvalidAssociationEventException(
                    message: "Invalid content item association event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationEventException);

            // when
            ValueTask<EventEnvelope<Association>?> onAddingTask =
                this.associationService.OnAddingAssociationAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingAssociationEventWhenContentIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = null!,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidAssociationEventException =
                new InvalidAssociationEventException(
                    message: "Invalid content item association event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationEventException);

            // when
            ValueTask<EventEnvelope<Association>?> onAddingTask =
                this.associationService.OnAddingAssociationAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingAssociationEventWhenIntegrityVerificationFailsAsync()
        {
            // given
            var forgedEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Association { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            string expectedEventName =
                $"{nameof(Association)}{AssociationEventOperation.Adding}";

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            var invalidAssociationEventException =
                new InvalidAssociationEventException(
                    message: "Invalid content item association event. Integrity verification failed.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationEventException);

            // when
            ValueTask<EventEnvelope<Association>?> onAddingTask =
                this.associationService.OnAddingAssociationAsync(
                    forgedEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
