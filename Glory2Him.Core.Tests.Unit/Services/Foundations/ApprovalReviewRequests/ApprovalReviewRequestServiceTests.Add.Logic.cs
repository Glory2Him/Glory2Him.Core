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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        [Fact]
        public async Task ShouldAddApprovalReviewRequestAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest randomApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            ApprovalReviewRequest inputApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest auditAppliedApprovalReviewRequest = inputApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest storageApprovalReviewRequest = auditAppliedApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest expectedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputApprovalReviewRequest, It.IsAny<SecurityContext>()))
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
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    inputApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(
                        inputApprovalReviewRequest, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertApprovalReviewRequestAsync(
                        auditAppliedApprovalReviewRequest, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName == EventBrokerIdentifiers
                            .ApprovalReviewRequestOnAddingApprovalReviewRequestSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Every role in the review tier may issue an invitation (§7.9 rule 2), including the
        /// entity-scoped ones the foundation recognizes by the §16.6 suffix. Reviewers are in
        /// deliberately: HR-3 bars them from SETTING an approval status, and an invitation sets
        /// nothing — it is coordination of the round, which is everyone's inside it.
        /// </summary>
        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldAddApprovalReviewRequestWhenUserHasReviewRoleAsync(string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest randomApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            ApprovalReviewRequest inputApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest auditAppliedApprovalReviewRequest = inputApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest storageApprovalReviewRequest = auditAppliedApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest expectedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputApprovalReviewRequest, It.IsAny<SecurityContext>()))
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
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    inputApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);

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
        /// The display name is presentation, not identity, and the identity store may legitimately
        /// hold none. Refusing the invitation over a cosmetic field would block a request the
        /// policy allows, so a blank name lands as an ordinary record.
        ///
        /// <para>Deliberately a positive assertion rather than a hole in the invalid-input theory:
        /// this states the rule outright, so re-adding a required check would fail HERE, where the
        /// reason is written down.</para>
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldAddApprovalReviewRequestWithoutADisplayNameAsync(string blankDisplayName)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest randomApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            randomApprovalReviewRequest.RequestedUserDisplayName = blankDisplayName;
            ApprovalReviewRequest inputApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest auditAppliedApprovalReviewRequest = inputApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest storageApprovalReviewRequest = auditAppliedApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest expectedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputApprovalReviewRequest, It.IsAny<SecurityContext>()))
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
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    inputApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            // then: it lands, blank name and all
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);
            actualApprovalReviewRequest.RequestedUserDisplayName.Should().Be(blankDisplayName);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    auditAppliedApprovalReviewRequest, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
