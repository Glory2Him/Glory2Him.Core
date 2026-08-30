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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        [Fact]
        public async Task ShouldAddApprovalReviewRequestOnAddingEventAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest randomApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            var inboundEnvelope = new EventEnvelope<ApprovalReviewRequest>
            {
                Content = randomApprovalReviewRequest,
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            ApprovalReviewRequest auditAppliedApprovalReviewRequest = randomApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest storageApprovalReviewRequest = auditAppliedApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest expectedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    randomApprovalReviewRequest, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalReviewRequest.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    auditAppliedApprovalReviewRequest, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.Added))
                        .Returns(new ValueTask<EventPublishResult<ApprovalReviewRequest>>(
                            new EventPublishResult<ApprovalReviewRequest>()));

            // when
            EventEnvelope<ApprovalReviewRequest>? actualEnvelope =
                await this.approvalReviewRequestService.OnAddingApprovalReviewRequestAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then: the event path converges on the same shared do-work as the direct path
            actualEnvelope.Should().NotBeNull();
            actualEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalReviewRequest);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    auditAppliedApprovalReviewRequest, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.Added),
                Times.Once);
        }

        /// <summary>
        /// A replayed or duplicated delivery replies <c>null</c> and writes nothing, so the
        /// substrate cannot apply the same invitation twice — including a published fact ever
        /// looping back into a request handler.
        /// </summary>
        [Fact]
        public async Task ShouldNotReapplyAnAlreadyProcessedAddingEventAsync()
        {
            // given
            EventEnvelope<ApprovalReviewRequest> inboundEnvelope =
                CreateRandomApprovalReviewRequestEnvelope();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    inboundEnvelope.Metadata.EventId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalReviewRequest>? actualEnvelope =
                await this.approvalReviewRequestService.OnAddingApprovalReviewRequestAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotReapplyAnAlreadyProcessedRemovingEventAsync()
        {
            // given
            EventEnvelope<ApprovalReviewRequest> inboundEnvelope =
                CreateRandomApprovalReviewRequestEnvelope();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    inboundEnvelope.Metadata.EventId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalReviewRequest>? actualEnvelope =
                await this.approvalReviewRequestService.OnRemovingApprovalReviewRequestByIdAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
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
            EventEnvelope<ApprovalReviewRequest> inboundEnvelope =
                CreateRandomApprovalReviewRequestEnvelope();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            var invalidApprovalReviewRequestEventException =
                new InvalidApprovalReviewRequestEventException(
                    message: "Invalid approval review request event. " +
                        "Integrity verification failed.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalReviewRequestEventException);

            // when
            ValueTask<EventEnvelope<ApprovalReviewRequest>?> onAddingTask =
                this.approvalReviewRequestService.OnAddingApprovalReviewRequestAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingEventIfEnvelopeIsNullAndLogItAsync()
        {
            // given
            EventEnvelope<ApprovalReviewRequest> nullEnvelope = null;

            var invalidApprovalReviewRequestEventException =
                new InvalidApprovalReviewRequestEventException(
                    message: "Invalid approval review request event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalReviewRequestEventException);

            // when
            ValueTask<EventEnvelope<ApprovalReviewRequest>?> onAddingTask =
                this.approvalReviewRequestService.OnAddingApprovalReviewRequestAsync(
                    nullEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldHardRemoveApprovalReviewRequestOnHardRemovingEventAsync()
        {
            // given
            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();

            var inboundEnvelope = new EventEnvelope<ApprovalReviewRequest>
            {
                Content = new ApprovalReviewRequest { Id = randomApprovalReviewRequest.Id },
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            ApprovalReviewRequest deletedApprovalReviewRequest = randomApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest expectedApprovalReviewRequest = deletedApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    randomApprovalReviewRequest.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomApprovalReviewRequest);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteApprovalReviewRequestAsync(
                    randomApprovalReviewRequest, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(deletedApprovalReviewRequest);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.HardRemoved))
                        .Returns(new ValueTask<EventPublishResult<ApprovalReviewRequest>>(
                            new EventPublishResult<ApprovalReviewRequest>()));

            // when
            EventEnvelope<ApprovalReviewRequest>? actualEnvelope =
                await this.approvalReviewRequestService
                    .OnHardRemovingApprovalReviewRequestByIdAsync(
                        inboundEnvelope,
                        TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().NotBeNull();
            actualEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalReviewRequest);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalReviewRequestAsync(
                    randomApprovalReviewRequest, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotReapplyAnAlreadyProcessedHardRemovingEventAsync()
        {
            // given
            EventEnvelope<ApprovalReviewRequest> inboundEnvelope =
                CreateRandomApprovalReviewRequestEnvelope(
                    CreateAuthenticatedSecurityContext(Roles.Administrators));

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    inboundEnvelope.Metadata.EventId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalReviewRequest>? actualEnvelope =
                await this.approvalReviewRequestService
                    .OnHardRemovingApprovalReviewRequestByIdAsync(
                        inboundEnvelope,
                        TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
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
            EventEnvelope<ApprovalReviewRequest> inboundEnvelope =
                CreateRandomApprovalReviewRequestEnvelope();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            var invalidApprovalReviewRequestEventException =
                new InvalidApprovalReviewRequestEventException(
                    message: "Invalid approval review request event. " +
                        "Integrity verification failed.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalReviewRequestEventException);

            // when
            ValueTask<EventEnvelope<ApprovalReviewRequest>?> onRemovingTask =
                this.approvalReviewRequestService.OnRemovingApprovalReviewRequestByIdAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    onRemovingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldRetrieveApprovalReviewRequestOnRetrievingEventAsync()
        {
            // given
            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();

            var inboundEnvelope = new EventEnvelope<ApprovalReviewRequest>
            {
                Content = new ApprovalReviewRequest { Id = randomApprovalReviewRequest.Id },
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            ApprovalReviewRequest expectedApprovalReviewRequest = randomApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    randomApprovalReviewRequest.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync("a-moderator");

            // when
            EventEnvelope<ApprovalReviewRequest>? actualEnvelope =
                await this.approvalReviewRequestService.OnRetrievingApprovalReviewRequestByIdAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().NotBeNull();
            actualEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalReviewRequest);

            // A read is naturally idempotent, so it does no ProcessedEvents bookkeeping.
            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.IsAny<Glory2Him.Core.Models.Foundations.ProcessedEvents.ProcessedEvent>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
