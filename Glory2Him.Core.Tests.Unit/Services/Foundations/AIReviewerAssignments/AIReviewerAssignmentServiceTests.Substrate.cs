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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    public partial class AIReviewerAssignmentServiceTests
    {
        [Fact]
        public async Task ShouldAddAIReviewerAssignmentOnAddingEventAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            var inboundEnvelope = new EventEnvelope<AIReviewerAssignment>
            {
                Content = randomAIReviewerAssignment,
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            AIReviewerAssignment auditAppliedAIReviewerAssignment = randomAIReviewerAssignment.DeepClone();
            AIReviewerAssignment storageAIReviewerAssignment = auditAppliedAIReviewerAssignment.DeepClone();
            AIReviewerAssignment expectedAIReviewerAssignment = storageAIReviewerAssignment.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    randomAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedAIReviewerAssignment.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAIReviewerAssignment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Added))
                        .Returns(new ValueTask<EventPublishResult<AIReviewerAssignment>>(
                            new EventPublishResult<AIReviewerAssignment>()));

            // when
            EventEnvelope<AIReviewerAssignment>? actualEnvelope =
                await this.aiReviewerAssignmentService.OnAddingAIReviewerAssignmentAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then: the event path converges on the same shared do-work as the direct path
            actualEnvelope.Should().NotBeNull();
            actualEnvelope!.Content.Should().BeEquivalentTo(expectedAIReviewerAssignment);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Added),
                Times.Once);
        }

        /// <summary>
        /// A replayed or duplicated delivery replies <c>null</c> and writes nothing, so the
        /// substrate cannot apply the same assignment twice — including a published fact ever
        /// looping back into a request handler.
        /// </summary>
        [Fact]
        public async Task ShouldNotReapplyAnAlreadyProcessedAddingEventAsync()
        {
            // given
            EventEnvelope<AIReviewerAssignment> inboundEnvelope =
                CreateRandomAIReviewerAssignmentEnvelope();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    inboundEnvelope.Metadata.EventId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<AIReviewerAssignment>? actualEnvelope =
                await this.aiReviewerAssignmentService.OnAddingAIReviewerAssignmentAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldModifyAIReviewerAssignmentOnModifyingEventAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            var inboundEnvelope = new EventEnvelope<AIReviewerAssignment>
            {
                Content = randomAIReviewerAssignment,
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            AIReviewerAssignment auditAppliedAIReviewerAssignment = randomAIReviewerAssignment.DeepClone();
            AIReviewerAssignment storageAIReviewerAssignment = auditAppliedAIReviewerAssignment.DeepClone();

            storageAIReviewerAssignment.UpdatedWhen =
                storageAIReviewerAssignment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            AIReviewerAssignment auditPreservedAIReviewerAssignment = auditAppliedAIReviewerAssignment.DeepClone();
            AIReviewerAssignment updatedAIReviewerAssignment = auditPreservedAIReviewerAssignment.DeepClone();
            AIReviewerAssignment expectedAIReviewerAssignment = updatedAIReviewerAssignment.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    randomAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    auditAppliedAIReviewerAssignment.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedAIReviewerAssignment,
                    storageAIReviewerAssignment))
                        .ReturnsAsync(auditPreservedAIReviewerAssignment);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    auditPreservedAIReviewerAssignment, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedAIReviewerAssignment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Modified))
                        .Returns(new ValueTask<EventPublishResult<AIReviewerAssignment>>(
                            new EventPublishResult<AIReviewerAssignment>()));

            // when
            EventEnvelope<AIReviewerAssignment>? actualEnvelope =
                await this.aiReviewerAssignmentService.OnModifyingAIReviewerAssignmentAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().NotBeNull();
            actualEnvelope!.Content.Should().BeEquivalentTo(expectedAIReviewerAssignment);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    auditPreservedAIReviewerAssignment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Modified),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotReapplyAnAlreadyProcessedModifyingEventAsync()
        {
            // given
            EventEnvelope<AIReviewerAssignment> inboundEnvelope =
                CreateRandomAIReviewerAssignmentEnvelope();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    inboundEnvelope.Metadata.EventId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<AIReviewerAssignment>? actualEnvelope =
                await this.aiReviewerAssignmentService.OnModifyingAIReviewerAssignmentAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldNotReapplyAnAlreadyProcessedRemovingEventAsync()
        {
            // given
            EventEnvelope<AIReviewerAssignment> inboundEnvelope =
                CreateRandomAIReviewerAssignmentEnvelope();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    inboundEnvelope.Metadata.EventId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<AIReviewerAssignment>? actualEnvelope =
                await this.aiReviewerAssignmentService.OnRemovingAIReviewerAssignmentByIdAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The signature is what makes the envelope's <c>SecurityContext</c> trustworthy on the
        /// event path: without verification, anyone who can put a message on the address states
        /// their own identity and roles and is believed (§14.6 rule 4). It is checked in the
        /// RECEIVER rather than the transport, because a handler is reachable without going
        /// through the broker.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingEventIfIntegrityCheckFailsAndLogItAsync()
        {
            // given
            EventEnvelope<AIReviewerAssignment> inboundEnvelope =
                CreateRandomAIReviewerAssignmentEnvelope();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            var invalidAIReviewerAssignmentEventException =
                new InvalidAIReviewerAssignmentEventException(
                    message: "Invalid AI reviewer assignment event. " +
                        "Integrity verification failed.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentEventException);

            // when
            ValueTask<EventEnvelope<AIReviewerAssignment>?> onAddingTask =
                this.aiReviewerAssignmentService.OnAddingAIReviewerAssignmentAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingEventIfEnvelopeIsNullAndLogItAsync()
        {
            // given
            EventEnvelope<AIReviewerAssignment> nullEnvelope = null;

            var invalidAIReviewerAssignmentEventException =
                new InvalidAIReviewerAssignmentEventException(
                    message: "Invalid AI reviewer assignment event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentEventException);

            // when
            ValueTask<EventEnvelope<AIReviewerAssignment>?> onAddingTask =
                this.aiReviewerAssignmentService.OnAddingAIReviewerAssignmentAsync(
                    nullEnvelope,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        /// <summary>
        /// The integrity guard is per-handler, not per-service: each handler names the event it
        /// serves when it verifies, so a signature valid for one address must not be accepted on
        /// another. Checked on a second handler for that reason.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemovingEventIfIntegrityCheckFailsAndLogItAsync()
        {
            // given
            EventEnvelope<AIReviewerAssignment> inboundEnvelope =
                CreateRandomAIReviewerAssignmentEnvelope();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            var invalidAIReviewerAssignmentEventException =
                new InvalidAIReviewerAssignmentEventException(
                    message: "Invalid AI reviewer assignment event. " +
                        "Integrity verification failed.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentEventException);

            // when
            ValueTask<EventEnvelope<AIReviewerAssignment>?> onRemovingTask =
                this.aiReviewerAssignmentService.OnRemovingAIReviewerAssignmentByIdAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    onRemovingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldRetrieveAIReviewerAssignmentOnRetrievingEventAsync()
        {
            // given
            AIReviewerAssignment randomAIReviewerAssignment = CreateRandomAIReviewerAssignment();

            var inboundEnvelope = new EventEnvelope<AIReviewerAssignment>
            {
                Content = new AIReviewerAssignment { Id = randomAIReviewerAssignment.Id },
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            AIReviewerAssignment expectedAIReviewerAssignment = randomAIReviewerAssignment.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    randomAIReviewerAssignment.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomAIReviewerAssignment);

            // when
            EventEnvelope<AIReviewerAssignment>? actualEnvelope =
                await this.aiReviewerAssignmentService.OnRetrievingAIReviewerAssignmentByIdAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().NotBeNull();
            actualEnvelope!.Content.Should().BeEquivalentTo(expectedAIReviewerAssignment);

            // A read is naturally idempotent, so it does no ProcessedEvents bookkeeping.
            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.IsAny<Glory2Him.Core.Models.Foundations.ProcessedEvents.ProcessedEvent>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
