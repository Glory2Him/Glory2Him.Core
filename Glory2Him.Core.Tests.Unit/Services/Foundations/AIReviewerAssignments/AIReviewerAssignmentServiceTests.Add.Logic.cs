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
        public async Task ShouldAddAIReviewerAssignmentAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment auditAppliedAIReviewerAssignment = inputAIReviewerAssignment.DeepClone();
            AIReviewerAssignment storageAIReviewerAssignment = auditAppliedAIReviewerAssignment.DeepClone();
            AIReviewerAssignment expectedAIReviewerAssignment = storageAIReviewerAssignment.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputAIReviewerAssignment, It.IsAny<SecurityContext>()))
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
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    inputAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(
                        inputAIReviewerAssignment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertAIReviewerAssignmentAsync(
                        auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName == EventBrokerIdentifiers
                            .AIReviewerAssignmentOnAddingAIReviewerAssignmentSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Every role in the review tier may assign Berean to a round, including the
        /// entity-scoped ones the foundation recognizes by the §16.6 suffix — the same tier
        /// that may request a human reviewer, because bringing Berean onto a round is the same
        /// kind of round-coordination act.
        /// </summary>
        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldAddAIReviewerAssignmentWhenUserHasReviewRoleAsync(string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment auditAppliedAIReviewerAssignment = inputAIReviewerAssignment.DeepClone();
            AIReviewerAssignment storageAIReviewerAssignment = auditAppliedAIReviewerAssignment.DeepClone();
            AIReviewerAssignment expectedAIReviewerAssignment = storageAIReviewerAssignment.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputAIReviewerAssignment, It.IsAny<SecurityContext>()))
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
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    inputAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);

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
        /// The two system-facing bools default false and this pass never sets them — the future
        /// automated review process (design §8.6.2) is what flips them. An add that leaves both
        /// at their default lands as an ordinary record, exactly like every other field a caller
        /// did not set.
        /// </summary>
        [Fact]
        public async Task ShouldAddAIReviewerAssignmentWithDefaultFlagsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            randomAIReviewerAssignment.IsAIReviewCompleted = false;
            randomAIReviewerAssignment.IsAIReviewCommentsPresent = false;
            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment auditAppliedAIReviewerAssignment = inputAIReviewerAssignment.DeepClone();
            AIReviewerAssignment storageAIReviewerAssignment = auditAppliedAIReviewerAssignment.DeepClone();
            AIReviewerAssignment expectedAIReviewerAssignment = storageAIReviewerAssignment.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputAIReviewerAssignment, It.IsAny<SecurityContext>()))
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
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    inputAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);
            actualAIReviewerAssignment.IsAIReviewCompleted.Should().BeFalse();
            actualAIReviewerAssignment.IsAIReviewCommentsPresent.Should().BeFalse();

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
