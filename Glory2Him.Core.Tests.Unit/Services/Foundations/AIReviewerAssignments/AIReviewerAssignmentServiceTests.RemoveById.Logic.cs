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
        [Fact]
        public async Task ShouldRemoveAIReviewerAssignmentByIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            Guid inputAIReviewerAssignmentId = randomAIReviewerAssignment.Id;
            string randomDeletionReason = GetRandomString();
            AIReviewerAssignment storageAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment auditAppliedAIReviewerAssignment = storageAIReviewerAssignment.DeepClone();
            auditAppliedAIReviewerAssignment.IsDeleted = true;
            AIReviewerAssignment removedAIReviewerAssignment = auditAppliedAIReviewerAssignment.DeepClone();
            AIReviewerAssignment expectedAIReviewerAssignment = removedAIReviewerAssignment.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(
                    storageAIReviewerAssignment,
                    It.IsAny<SecurityContext>(),
                    randomDeletionReason))
                        .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(removedAIReviewerAssignment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Removed))
                        .Returns(new ValueTask<EventPublishResult<AIReviewerAssignment>>(
                            new EventPublishResult<AIReviewerAssignment>()));

            // when
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId,
                    randomDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName == EventBrokerIdentifiers
                            .AIReviewerAssignmentOnRemovingAIReviewerAssignmentByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        /// <summary>
        /// Every review role — the same tier that may add or modify an assignment — may remove
        /// one. There is no owner-only nuance to test against here, unlike ApprovalReviewRequest's
        /// withdrawal: Berean is not a person a "requester" could differ from.
        /// </summary>
        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRemoveAIReviewerAssignmentWhenUserHasReviewRoleAsync(string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            Guid inputAIReviewerAssignmentId = randomAIReviewerAssignment.Id;
            AIReviewerAssignment storageAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment auditAppliedAIReviewerAssignment = storageAIReviewerAssignment.DeepClone();
            auditAppliedAIReviewerAssignment.IsDeleted = true;
            AIReviewerAssignment removedAIReviewerAssignment = auditAppliedAIReviewerAssignment.DeepClone();
            AIReviewerAssignment expectedAIReviewerAssignment = removedAIReviewerAssignment.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(
                    storageAIReviewerAssignment,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<string>()))
                        .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(removedAIReviewerAssignment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Removed))
                        .Returns(new ValueTask<EventPublishResult<AIReviewerAssignment>>(
                            new EventPublishResult<AIReviewerAssignment>()));

            // when
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Removing an already-removed assignment is a no-op rather than an error: the caller
        /// asked for a state the row is already in. Nothing is written and no fact is published,
        /// so a retried removal cannot emit a second <c>-Removed</c>.
        /// </summary>
        [Fact]
        public async Task ShouldNotReremoveAnAlreadyRemovedAIReviewerAssignmentAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            randomAIReviewerAssignment.IsDeleted = true;
            Guid inputAIReviewerAssignmentId = randomAIReviewerAssignment.Id;
            AIReviewerAssignment storageAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment expectedAIReviewerAssignment = storageAIReviewerAssignment.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAIReviewerAssignment);

            // when
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService.RemoveAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<AIReviewerAssignmentEventOperation>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
