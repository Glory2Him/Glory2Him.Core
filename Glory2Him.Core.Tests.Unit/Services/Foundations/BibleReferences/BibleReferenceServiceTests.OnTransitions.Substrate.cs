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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        // ── OnSubmitting ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldSubmitOnSubmittingBibleReferenceEventAsync()
        {
            // given: the event path carries the id in the envelope; the do-work reads only the
            // id off it and drives the row Draft -> Submitted, exactly as the direct path does
            BibleReference storageBibleReference = CreateSubmittableStorageBibleReference();

            var requestEnvelope = new EventEnvelope<BibleReference>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new BibleReference { Id = storageBibleReference.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageBibleReference.CreatedBy);

            SetupBibleReferenceStorageRead(storageBibleReference);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((BibleReference entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateBibleReferenceAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((BibleReference entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    It.IsAny<BibleReferenceEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                            new EventPublishResult<BibleReference>()));

            // when
            EventEnvelope<BibleReference>? actualReplyEnvelope =
                await this.bibleReferenceService.OnSubmittingBibleReferenceAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishBibleReferenceAsync(
                        It.IsAny<EventEnvelope<BibleReference>>(),
                        BibleReferenceEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipSubmitAndReplyNullWhenSubmittingBibleReferenceEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<BibleReference>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new BibleReference { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.BibleReferenceOnSubmittingBibleReferenceSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<BibleReference>? actualReplyEnvelope =
                await this.bibleReferenceService.OnSubmittingBibleReferenceAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.BibleReferenceOnSubmittingBibleReferenceSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmittingBibleReferenceEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<BibleReference>?> onSubmittingTask =
                this.bibleReferenceService.OnSubmittingBibleReferenceAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    onSubmittingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        // ── OnApproving ──────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldApproveOnApprovingBibleReferenceEventAsync()
        {
            // given
            BibleReference storageBibleReference = CreateApprovableStorageBibleReference();

            var requestEnvelope = new EventEnvelope<BibleReference>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers),
                Content = CreateApprovalDecision(storageBibleReference.Id),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupBibleReferenceStorageRead(storageBibleReference);
            SetupAccessBrokerToPermit();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((BibleReference entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateBibleReferenceAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((BibleReference entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    It.IsAny<BibleReferenceEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                            new EventPublishResult<BibleReference>()));

            // when
            EventEnvelope<BibleReference>? actualReplyEnvelope =
                await this.bibleReferenceService.OnApprovingBibleReferenceAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishBibleReferenceAsync(
                        It.IsAny<EventEnvelope<BibleReference>>(),
                        BibleReferenceEventOperation.Approved),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipApproveAndReplyNullWhenApprovingBibleReferenceEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<BibleReference>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers),
                Content = CreateApprovalDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.BibleReferenceOnApprovingBibleReferenceSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<BibleReference>? actualReplyEnvelope =
                await this.bibleReferenceService.OnApprovingBibleReferenceAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: a duplicate approve neither re-decides nor re-announces
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.BibleReferenceOnApprovingBibleReferenceSubscriptionName,
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
        public async Task ShouldThrowValidationExceptionOnApprovingBibleReferenceEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<BibleReference>?> onApprovingTask =
                this.bibleReferenceService.OnApprovingBibleReferenceAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    onApprovingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
