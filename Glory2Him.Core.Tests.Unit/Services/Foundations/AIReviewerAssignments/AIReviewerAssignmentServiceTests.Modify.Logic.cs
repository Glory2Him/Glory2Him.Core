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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    public partial class AIReviewerAssignmentServiceTests
    {
        /// <summary>
        /// The two system-facing bools are the only fields a caller could legitimately change on
        /// a modify — nothing calls this with them set true yet (the future automated review
        /// process, design §8.6.2, is what will), but the capability must exist and behave like
        /// every other modify in this codebase.
        /// </summary>
        [Fact]
        public async Task ShouldModifyAIReviewerAssignmentAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            randomAIReviewerAssignment.IsAIReviewCompleted = true;
            randomAIReviewerAssignment.IsAIReviewCommentsPresent = true;
            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment auditAppliedAIReviewerAssignment = inputAIReviewerAssignment.DeepClone();
            AIReviewerAssignment storageAIReviewerAssignment = auditAppliedAIReviewerAssignment.DeepClone();

            storageAIReviewerAssignment.UpdatedWhen =
                storageAIReviewerAssignment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            storageAIReviewerAssignment.IsAIReviewCompleted = false;
            storageAIReviewerAssignment.IsAIReviewCommentsPresent = false;
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
                    inputAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    auditAppliedAIReviewerAssignment.Id,
                    It.IsAny<CancellationToken>()))
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
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    inputAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        inputAIReviewerAssignment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectAIReviewerAssignmentByIdAsync(
                        auditAppliedAIReviewerAssignment.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedAIReviewerAssignment,
                        storageAIReviewerAssignment),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAIReviewerAssignmentAsync(
                        auditPreservedAIReviewerAssignment,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishAIReviewerAssignmentAsync(
                        It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                        AIReviewerAssignmentEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName == EventBrokerIdentifiers
                            .AIReviewerAssignmentOnModifyingAIReviewerAssignmentSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Clearing both flags together is how the orchestration re-requests Berean on a round it
        /// has already reviewed, so the pairing invariant must not stand in its way. It is asked
        /// of the INPUT alone for exactly this reason: pinned against storage instead, a row that
        /// currently says "completed, with comments" could never be reset, because the caller's
        /// cleared comments flag would be measured against the stored completion it is replacing.
        /// </summary>
        [Fact]
        public async Task ShouldModifyAIReviewerAssignmentWhenBothReviewFlagsAreClearedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            randomAIReviewerAssignment.IsAIReviewCompleted = false;
            randomAIReviewerAssignment.IsAIReviewCommentsPresent = false;
            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment auditAppliedAIReviewerAssignment = inputAIReviewerAssignment.DeepClone();
            AIReviewerAssignment storageAIReviewerAssignment = auditAppliedAIReviewerAssignment.DeepClone();

            storageAIReviewerAssignment.UpdatedWhen =
                storageAIReviewerAssignment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            // the row as Berean left it: a finished pass that filed comments
            storageAIReviewerAssignment.IsAIReviewCompleted = true;
            storageAIReviewerAssignment.IsAIReviewCommentsPresent = true;
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
                    inputAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    auditAppliedAIReviewerAssignment.Id,
                    It.IsAny<CancellationToken>()))
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
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    inputAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);
            actualAIReviewerAssignment.IsAIReviewCompleted.Should().BeFalse();
            actualAIReviewerAssignment.IsAIReviewCommentsPresent.Should().BeFalse();

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

        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldModifyAIReviewerAssignmentWhenUserHasReviewRoleAsync(string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment auditAppliedAIReviewerAssignment = inputAIReviewerAssignment.DeepClone();
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
                    inputAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    auditAppliedAIReviewerAssignment.Id,
                    It.IsAny<CancellationToken>()))
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
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    inputAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);

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
    }
}
