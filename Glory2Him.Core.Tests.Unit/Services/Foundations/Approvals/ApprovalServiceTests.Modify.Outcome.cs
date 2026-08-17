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
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        /// <summary>
        /// What lands on the row is DERIVED from the decision's verdict, never copied from the
        /// payload. The third case is the one that matters most: a bypass was requested but the
        /// conditions were already met, so nothing was waived — the verdict comes back
        /// <c>IsBypassUsed = false</c> and the row must record no waiver, or the audit trail
        /// gains a waiver that never happened.
        /// </summary>
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public async Task ShouldDeriveTheBypassPairFromTheDecisionVerdictOnApproveAsync(
            bool bypassRequested,
            bool verdictBypassUsed)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            string bypassReason = bypassRequested ? GetRandomString() : null;

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            inputApproval.ApprovalStatus = ApprovalStatus.Approved;
            inputApproval.IsApprovedByBypass = bypassRequested;
            inputApproval.ApprovedByBypassReason = bypassReason;

            Approval storageApproval = randomApproval.DeepClone();
            storageApproval.ApprovalStatus = ApprovalStatus.Submitted;
            storageApproval.IsApprovedByBypass = false;
            storageApproval.ApprovedByBypassReason = null;

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);
            SetupOutcomeDecisionToReturn(PermittedOutcomeVerdict(verdictBypassUsed));

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(inputApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(It.IsAny<Approval>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval approval, CancellationToken _) => approval);

            // when
            Approval actualApproval = await this.approvalService.ModifyApprovalAsync(
                inputApproval,
                TestContext.Current.CancellationToken);

            // then
            bool expectedFlag = verdictBypassUsed;
            string expectedReason = verdictBypassUsed ? bypassReason : null;

            actualApproval.IsApprovedByBypass.Should().Be(expectedFlag);
            actualApproval.ApprovedByBypassReason.Should().Be(expectedReason);

            // the payload pair went to the decision as the REQUEST...
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    inputApproval.Id,
                    ApprovalDecision.Approve,
                    bypassRequested,
                    bypassReason,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // ...and the VERDICT is what was written
            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.IsApprovedByBypass == expectedFlag
                            && approval.ApprovedByBypassReason == expectedReason),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// The amend gate deliberately admits the submitter (§14.7 posture D rule 3), so on its
        /// own it would let a role-less submitter approve their own round. The outcome gate is
        /// the second, DIFFERENT question — and its refusal must stop the write.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyWhenTheOutcomeDecisionRefusesAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            inputApproval.ApprovalStatus = ApprovalStatus.Approved;

            Approval storageApproval = randomApproval.DeepClone();
            storageApproval.ApprovalStatus = ApprovalStatus.Submitted;

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);

            SetupOutcomeDecisionToReturn(new AccessVerdict
            {
                IsPermitted = false,
                DenialReason = AccessDenialReason.NotInPublisherTier,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "the actor holds no publisher-tier role for this entity",
            });

            // when
            ValueTask<Approval> modifyApprovalTask =
                this.approvalService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    modifyApprovalTask.AsTask);

            // then
            actualApprovalValidationException.InnerException
                .Should().BeOfType<UnauthorizedApprovalException>();

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalAsync(
                        It.IsAny<Approval>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// A rejection is an outcome too — HR-3 gives it to the publisher tier, and without this
        /// consult a plain reviewer, or the submitter, could set <c>Rejected</c> through modify.
        /// Nothing is waived by refusing, so the bypass pair stays pinned on this path.
        /// </summary>
        [Fact]
        public async Task ShouldConsultTheDecisionWithRejectWhenRejectingAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            inputApproval.ApprovalStatus = ApprovalStatus.Rejected;

            Approval storageApproval = randomApproval.DeepClone();
            storageApproval.ApprovalStatus = ApprovalStatus.Submitted;

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);
            SetupOutcomeDecisionToReturn(PermittedOutcomeVerdict(isBypassUsed: false));

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(inputApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(It.IsAny<Approval>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval approval, CancellationToken _) => approval);

            // when
            Approval actualApproval = await this.approvalService.ModifyApprovalAsync(
                inputApproval,
                TestContext.Current.CancellationToken);

            // then
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    inputApproval.Id,
                    ApprovalDecision.Reject,
                    false,
                    null,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            actualApproval.IsApprovedByBypass.Should().BeFalse();
            actualApproval.ApprovedByBypassReason.Should().BeNull();
        }

        /// <summary>
        /// Draft↔Submitted moves are amendment, not outcome — §14.7 posture D rule 3's whole
        /// point is that the submitter can resubmit without holding any role, and asking the
        /// §8.6.1 decision here would refuse them for not being a publisher.
        /// </summary>
        [Fact]
        public async Task ShouldNotConsultTheOutcomeDecisionWhenMovingAmongWorkflowStatusesAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            inputApproval.ApprovalStatus = ApprovalStatus.Submitted;

            Approval storageApproval = randomApproval.DeepClone();
            storageApproval.ApprovalStatus = ApprovalStatus.Draft;

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(inputApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(It.IsAny<Approval>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval approval, CancellationToken _) => approval);

            // when
            await this.approvalService.ModifyApprovalAsync(
                inputApproval,
                TestContext.Current.CancellationToken);

            // then
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalAsync(
                        It.IsAny<Approval>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// The pin stands on every path that is not the becoming-Approved one — a rejection
        /// waives nothing, so even a caller the decision PERMITS to reject may not touch the
        /// waiver record on the way through.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseTouchingTheBypassPairWhileRejectingAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            inputApproval.ApprovalStatus = ApprovalStatus.Rejected;
            inputApproval.IsApprovedByBypass = true;
            inputApproval.ApprovedByBypassReason = "smuggled in on the rejection";

            Approval storageApproval = randomApproval.DeepClone();
            storageApproval.ApprovalStatus = ApprovalStatus.Submitted;
            storageApproval.IsApprovedByBypass = false;
            storageApproval.ApprovedByBypassReason = null;

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);
            SetupOutcomeDecisionToReturn(PermittedOutcomeVerdict(isBypassUsed: false));

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(inputApproval);

            // when
            ValueTask<Approval> modifyApprovalTask =
                this.approvalService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    modifyApprovalTask.AsTask);

            // then
            actualApprovalValidationException.InnerException!.Data.Keys
                .Cast<string>().Should().Contain(nameof(Approval.IsApprovedByBypass));

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalAsync(
                        It.IsAny<Approval>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The cap is 500 (design §7.2) and the boundary is pinned both ways, so an off-by-one
        /// would either refuse a legitimate reason or hand the refusal to the column as a
        /// dependency failure. Both sides of the pin carry the same long value so only the cap
        /// can fire.
        /// </summary>
        [Theory]
        [InlineData(500, false)]
        [InlineData(501, true)]
        public async Task ShouldEnforceTheBypassReasonCapAtExactlyFiveHundredOnModifyAsync(
            int reasonLength,
            bool expectRefusal)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            string longReason = new string('x', reasonLength);

            Approval randomApproval = CreateRandomModifyApproval(randomDateTimeOffset, randomUserId);
            Approval inputApproval = randomApproval;
            inputApproval.IsApprovedByBypass = true;
            inputApproval.ApprovedByBypassReason = longReason;

            Approval storageApproval = randomApproval.DeepClone();

            storageApproval.UpdatedWhen =
                storageApproval.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupModifyApprovalRun(inputApproval, storageApproval, randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<Approval>()))
                        .ReturnsAsync(inputApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(It.IsAny<Approval>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval approval, CancellationToken _) => approval);

            // when
            ValueTask<Approval> modifyApprovalTask =
                this.approvalService.ModifyApprovalAsync(
                    inputApproval,
                    TestContext.Current.CancellationToken);

            // then
            if (expectRefusal)
            {
                ApprovalValidationException actualException =
                    await Assert.ThrowsAsync<ApprovalValidationException>(
                        modifyApprovalTask.AsTask);

                actualException.InnerException!.Data.Keys
                    .Cast<string>().Should().Contain(nameof(Approval.ApprovedByBypassReason));
            }
            else
            {
                await modifyApprovalTask;

                this.storageBrokerMock.Verify(broker =>
                        broker.UpdateApprovalAsync(
                            It.IsAny<Approval>(),
                            It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        private void SetupOutcomeDecisionToReturn(AccessVerdict accessVerdict) =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(accessVerdict);

        private static AccessVerdict PermittedOutcomeVerdict(bool isBypassUsed) =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = isBypassUsed,
                BypassedBlockReason = isBypassUsed
                    ? AccessDenialReason.ApprovalThresholdNotMet
                    : AccessDenialReason.None,
                Explanation = "permitted",
            };
    }
}
