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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    /// <summary>
    /// §8.6.2's THRESHOLD rules, refused at the service — the twin of
    /// <c>ApprovalSettingServiceTests.Add.Scope</c>, and there for the same reason.
    ///
    /// <para><b>Why these are rules and not just a column type.</b> Both thresholds are values on
    /// <c>ConfidenceScore</c>'s own 0.00–10.00 scale (§13.5), and <c>decimal(4,2)</c> is not that
    /// scale: it admits -99.99 through 99.99. Until the three check constraints and these rules
    /// arrived, the range was enforced by the admin page's number inputs alone, so any caller that
    /// was not that page — a script, a stale client — stored 50.00 and kept it.</para>
    ///
    /// <para><b>Why the ordering matters.</b> §8.6.2 rule 1 rejects below the rejection threshold
    /// and rule 2 approves above the approval one. Inverted, a single score satisfies both tests
    /// at once and both verdicts fire, a state the design defines no behaviour for. EQUAL is
    /// permitted throughout: it closes the middle band and leaves the two verdicts disjoint, and
    /// it is the pair <c>ApprovalPolicyDefaults</c> ships as its fail-closed fallback.</para>
    ///
    /// <para>The check constraints behind these rules are the defence in depth §14.6 rule 2 asks
    /// for; they surface as a dependency failure naming no field. These rules name the field.</para>
    /// </summary>
    public partial class ApprovalSettingServiceTests
    {
        public static TheoryData<decimal, decimal, string, string> InvalidThresholdPairs() =>
            new()
            {
                // Off the scale on either side, on either threshold. Each row states ONE fault:
                // the ordering rule stays silent while a threshold is off the scale, so a caller
                // is sent to one box rather than two.
                {
                    -0.01m, 7.50m,
                    nameof(ApprovalSetting.AIApprovalConfidenceRejectionThreshold),
                    "Confidence threshold must be between 0 and 10."
                },
                {
                    10.01m, 7.50m,
                    nameof(ApprovalSetting.AIApprovalConfidenceRejectionThreshold),
                    "Confidence threshold must be between 0 and 10."
                },
                {
                    2.50m, -0.01m,
                    nameof(ApprovalSetting.AIApprovalConfidenceApprovalThreshold),
                    "Confidence threshold must be between 0 and 10."
                },
                {
                    2.50m, 10.01m,
                    nameof(ApprovalSetting.AIApprovalConfidenceApprovalThreshold),
                    "Confidence threshold must be between 0 and 10."
                },

                // Both on the scale, in the wrong order — the fault the range rules cannot see.
                {
                    7.50m, 2.50m,
                    nameof(ApprovalSetting.AIApprovalConfidenceApprovalThreshold),
                    "Approval threshold must not be below the rejection threshold."
                },
            };

        /// <summary>
        /// The pairs a policy may legally sit on at the very edges of the scale, including the
        /// equal pairs. These pass, and a rule written with the wrong comparison would refuse
        /// them — "approve nothing below 10.00" is a policy, not a mistake.
        /// </summary>
        public static TheoryData<decimal, decimal> BoundaryThresholdPairs() =>
            new()
            {
                { 0.00m, 0.00m },
                { 0.00m, 10.00m },
                { 10.00m, 10.00m },
            };

        [Theory]
        [MemberData(nameof(InvalidThresholdPairs))]
        public async Task ShouldThrowValidationExceptionOnAddIfTheThresholdsAreNotAPairEightPointSixPointTwoAllowsAsync(
            decimal invalidRejectionThreshold,
            decimal invalidApprovalThreshold,
            string expectedParameter,
            string expectedMessage)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalSetting invalidApprovalSetting =
                CreateApprovalSettingFiller(randomDateTimeOffset, randomUserId).Create();

            invalidApprovalSetting.AIApprovalConfidenceRejectionThreshold = invalidRejectionThreshold;
            invalidApprovalSetting.AIApprovalConfidenceApprovalThreshold = invalidApprovalThreshold;

            var invalidApprovalSettingException =
                new InvalidApprovalSettingException(
                    message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.AddData(
                key: expectedParameter,
                values: expectedMessage);

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSetting> addApprovalSettingTask =
                this.approvalSettingService.AddApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    addApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The boundaries are IN range and an equal pair is legal, so this write reaches storage
        /// whole. Asserted through the add rather than against the rule directly: a range rule
        /// that refused its own edges would leave the admin page unable to state either extreme,
        /// and nothing else in the suite would say so.
        /// </summary>
        [Theory]
        [MemberData(nameof(BoundaryThresholdPairs))]
        public async Task ShouldAddApprovalSettingIfTheThresholdsSitOnTheEdgesOfTheScaleAsync(
            decimal boundaryRejectionThreshold,
            decimal boundaryApprovalThreshold)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSetting randomApprovalSetting = CreateApprovalSettingFiller(randomDateTimeOffset).Create();
            ApprovalSetting inputApprovalSetting = randomApprovalSetting;
            inputApprovalSetting.AIApprovalConfidenceRejectionThreshold = boundaryRejectionThreshold;
            inputApprovalSetting.AIApprovalConfidenceApprovalThreshold = boundaryApprovalThreshold;
            ApprovalSetting auditAppliedApprovalSetting = inputApprovalSetting.DeepClone();
            ApprovalSetting storageApprovalSetting = auditAppliedApprovalSetting.DeepClone();
            ApprovalSetting expectedApprovalSetting = storageApprovalSetting.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalSetting.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertApprovalSettingAsync(auditAppliedApprovalSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSetting>>(
                        new EventPublishResult<ApprovalSetting>()));

            // when
            ApprovalSetting actualApprovalSetting =
                await this.approvalSettingService.AddApprovalSettingAsync(
                    inputApprovalSetting,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSetting.Should().BeEquivalentTo(expectedApprovalSetting);

            actualApprovalSetting.AIApprovalConfidenceRejectionThreshold.Should()
                .Be(boundaryRejectionThreshold);

            actualApprovalSetting.AIApprovalConfidenceApprovalThreshold.Should()
                .Be(boundaryApprovalThreshold);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertApprovalSettingAsync(auditAppliedApprovalSetting, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
