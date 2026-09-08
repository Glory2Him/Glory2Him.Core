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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    /// <summary>
    /// §8.6.2's THRESHOLD rules on the MODIFY path, over the same
    /// <c>InvalidThresholdPairs</c> the add path is held to.
    ///
    /// <para>Repeated here rather than left to the add tests because the PUT is the path a
    /// script or a stale client actually takes: a policy row is seeded, not posted, so almost
    /// every threshold an administrator ever states arrives as a modify. A rule threaded into
    /// <c>ValidateOnAdd</c> alone would leave the one vector that matters open.</para>
    ///
    /// <para>The boundaries of the scale are proved on the add path, where the accepting case can
    /// be asserted end to end — both paths call the same two rules, so the comparisons themselves
    /// are established once.</para>
    /// </summary>
    public partial class ApprovalSettingServiceTests
    {
        [Theory]
        [MemberData(nameof(InvalidThresholdPairs))]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheThresholdsAreNotAPairEightPointSixPointTwoAllowsAsync(
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
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);

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
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSetting> modifyApprovalSettingTask =
                this.approvalSettingService.ModifyApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    modifyApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()),
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

            // and nothing was read or written — a pair the design refuses never reaches storage
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
