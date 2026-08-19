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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldReturnAnUnblockedApprovableVerdictWhenNothingBlocksAndTheCallerMayApproveAsync()
        {
            // given: the three counts are pinned to three DIFFERENT values. They travel from the
            // conditions verdict to the approval verdict unchanged, and a service that copied the
            // wrong one — or invented its own totals — would still satisfy assertions written
            // against numbers that happened to coincide.
            int recordedApprovals = 3;
            int requiredApprovals = 2;
            int unresolvedComments = 7;
            var entityId = Guid.NewGuid();

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));

            SetupConditions(CreateMetConditions(
                approvalCount: recordedApprovals,
                requiredNumberOfApprovals: requiredApprovals,
                unresolvedApprovalCommentCount: unresolvedComments));

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.Link,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then
            actualVerdict.BlockReasons.Should().BeEmpty();
            actualVerdict.IsBlocked.Should().BeFalse();
            actualVerdict.CanApprove.Should().BeTrue();

            // The counts ride along even when nothing blocks. A moderator is shown progress —
            // and the outstanding comments this policy chose not to gate on — rather than only
            // the blocked/not-blocked bit (§16.7.2).
            actualVerdict.ApprovalCount.Should().Be(recordedApprovals);
            actualVerdict.RequiredNumberOfApprovals.Should().Be(requiredApprovals);
            actualVerdict.UnresolvedApprovalCommentCount.Should().Be(unresolvedComments);
        }

        [Fact]
        public async Task ShouldEchoTheProbedApprovalIdentityAndAskThePolicyAboutThatApprovalAsync()
        {
            // given: the approval's own id and the entity's id are deliberately DIFFERENT values,
            // and the entity type is deliberately not the zero member. Both mistakes this guards
            // against — echoing the wrong id, and asking the policy about the entity instead of
            // the approval the probe found — are invisible when the two ids share a variable or
            // the enum happens to default to the value asserted.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            EntityType entityType = EntityType.Link;
            ApprovalStatus storedStatus = ApprovalStatus.Approved;

            SetupApprovalProbe(CreateApprovalMatch(storedStatus, approvalId));
            SetupConditions(CreateMetConditions());

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            // then
            actualVerdict.ApprovalId.Should().Be(approvalId);
            actualVerdict.EntityType.Should().Be(entityType);
            actualVerdict.EntityId.Should().Be(entityId);

            // The status is carried so a caller can tell a round that has not opened from one
            // already decided without inferring it from the reason set.
            actualVerdict.ApprovalStatus.Should().Be(storedStatus);

            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    entityType,
                    entityId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Both policy questions are asked about the APPROVAL id the probe returned.
            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    approvalId,
                    ApprovalDecision.Approve,
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            // The envelope is what captures the ambient caller the tier gate runs against, and it
            // names the entity being asked about (§16.7.2).
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is<Approval>(approval =>
                    approval.EntityType == entityType
                        && approval.EntityId == entityId)),
                Times.Once);
        }

        [Fact]
        public async Task ShouldReportEverySimultaneousBlockReasonInOrderEachWithItsOwnMessageAsync()
        {
            // given: three conditions failing at once, plus a caller-specific refusal. The whole
            // set is reported rather than the first failure — an approver told only about the
            // threshold adds a reviewer, retries, and only then learns about the comments they
            // could have settled in the same visit (§16.7.2).
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));

            SetupConditions(CreateBlockedConditions(
                blockReasons: new List<AccessDenialReason>
                {
                    AccessDenialReason.ApprovalThresholdNotMet,
                    AccessDenialReason.BlockedByUnresolvedApprovalComment,
                    AccessDenialReason.BlockedByZeroConfidenceScore,
                }));

            SetupAccessDecisions(
                decisionVerdict: RefusedVerdict(AccessDenialReason.ApprovalConditionsNotMet),
                bypassVerdict: RefusedVerdict(AccessDenialReason.BypassNotPermitted));

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.Link,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: the conditions' own precedence order is preserved and the caller-specific
            // refusal is appended after it, never interleaved.
            actualVerdict.BlockReasons.Select(reason => reason.Code).Should().Equal(
                AccessDenialReason.ApprovalThresholdNotMet,
                AccessDenialReason.BlockedByUnresolvedApprovalComment,
                AccessDenialReason.BlockedByZeroConfidenceScore,
                AccessDenialReason.ApprovalConditionsNotMet);

            // Every code arrives with prose of its own. Distinctness is what proves the mapping
            // ran per code rather than stamping one sentence across the set.
            actualVerdict.BlockReasons.Should().AllSatisfy(reason =>
                reason.Message.Should().NotBeNullOrWhiteSpace());

            actualVerdict.BlockReasons.Select(reason => reason.Message)
                .Should().OnlyHaveUniqueItems();

            actualVerdict.IsBlocked.Should().BeTrue();

            // The BYPASS probe's refusal is an availability answer, not a block. Reporting it
            // would tell a moderator that bypass being closed is a reason the item is blocked.
            actualVerdict.BlockReasons.Select(reason => reason.Code)
                .Should().NotContain(AccessDenialReason.BypassNotPermitted);
        }

        [Fact]
        public async Task ShouldRenderTheApprovalShortfallNumbersInTheThresholdMessageAsync()
        {
            // given: 1 recorded against 3 required. The two differ on purpose — a message that
            // rendered the pair the wrong way round, or dropped the numbers for the bare "the
            // threshold is not met", is the failure this catches. A moderator needs to know how
            // far off it is, not merely that it is off.
            int recordedApprovals = 1;
            int requiredApprovals = 3;

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));

            SetupConditions(CreateBlockedConditions(
                blockReasons: new List<AccessDenialReason>
                {
                    AccessDenialReason.ApprovalThresholdNotMet,
                },
                approvalCount: recordedApprovals,
                requiredNumberOfApprovals: requiredApprovals,
                unresolvedApprovalCommentCount: 0));

            SetupAccessDecisions(
                decisionVerdict: RefusedVerdict(AccessDenialReason.ApprovalConditionsNotMet),
                bypassVerdict: RefusedVerdict(AccessDenialReason.BypassNotPermitted));

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.Link,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            ApprovalBlockReason thresholdReason = actualVerdict.BlockReasons.Single(reason =>
                reason.Code == AccessDenialReason.ApprovalThresholdNotMet);

            thresholdReason.Message.Should().Be("1 of 3 required approvals recorded.");
        }

        [Theory]
        [InlineData(1, "1 review comment is still unresolved.")]
        [InlineData(4, "4 review comments are still unresolved.")]
        public async Task ShouldRenderTheUnresolvedCommentMessageInTheMatchingNumberAsync(
            int unresolvedComments,
            string expectedMessage)
        {
            // given: the count reaches the sentence, and the sentence agrees with itself. "1
            // review comments are still unresolved" is the kind of wording that makes a
            // moderation screen look unfinished, so the singular is a real branch and is tested
            // as one.
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));

            SetupConditions(CreateBlockedConditions(
                blockReasons: new List<AccessDenialReason>
                {
                    AccessDenialReason.BlockedByUnresolvedApprovalComment,
                },
                unresolvedApprovalCommentCount: unresolvedComments));

            SetupAccessDecisions(
                decisionVerdict: RefusedVerdict(AccessDenialReason.ApprovalConditionsNotMet),
                bypassVerdict: RefusedVerdict(AccessDenialReason.BypassNotPermitted));

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.Link,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            ApprovalBlockReason commentReason = actualVerdict.BlockReasons.Single(reason =>
                reason.Code == AccessDenialReason.BlockedByUnresolvedApprovalComment);

            commentReason.Message.Should().Be(expectedMessage);
            actualVerdict.UnresolvedApprovalCommentCount.Should().Be(unresolvedComments);
        }

        [Fact]
        public async Task ShouldReportOnlyTheDraftReasonWhenTheApprovalHasNotBeenSubmittedAsync()
        {
            // given: a DRAFT approval whose conditions verdict nonetheless reports blockers, and
            // a caller who is refused on top. A draft has not entered a round, so the §8.5
            // conditions are not merely unmet but not yet asked — reporting "1 of 3 approvals"
            // beside it would invite a moderator to chase reviewers for something nobody has
            // submitted. The one action it needs is to amend and submit (§16.7.3).
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Draft));

            SetupConditions(CreateBlockedConditions(
                blockReasons: new List<AccessDenialReason>
                {
                    AccessDenialReason.ApprovalThresholdNotMet,
                    AccessDenialReason.BlockedByUnresolvedApprovalComment,
                }));

            SetupAccessDecisions(
                decisionVerdict: RefusedVerdict(AccessDenialReason.SelfApprovalNotPermitted),
                bypassVerdict: RefusedVerdict(AccessDenialReason.BypassNotPermitted));

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.Link,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: exactly one reason, and it is the draft one
            actualVerdict.BlockReasons.Should().ContainSingle();

            actualVerdict.BlockReasons.Single().Code
                .Should().Be(AccessDenialReason.BlockedDueToDraftStatus);

            actualVerdict.BlockReasons.Single().Message.Should().NotBeNullOrWhiteSpace();
            actualVerdict.IsBlocked.Should().BeTrue();

            // and the suppressed set really is suppressed — the short-circuit is what is under
            // test, so the reasons it must swallow are named rather than left to the count.
            actualVerdict.BlockReasons.Select(reason => reason.Code).Should().NotContain(
                new[]
                {
                    AccessDenialReason.ApprovalThresholdNotMet,
                    AccessDenialReason.BlockedByUnresolvedApprovalComment,
                    AccessDenialReason.SelfApprovalNotPermitted,
                });
        }

        [Fact]
        public async Task ShouldAppendTheCallerRefusalWhenTheConditionsAreMetButThisCallerMayNotApproveAsync()
        {
            // given: nothing blocks the APPROVAL — the conditions are fully met — but this caller
            // submitted the item and the content type forbids self-approval (HR-2). Without the
            // appended reason the verdict would report nothing blocking beside a disabled approve
            // button, which is the one outcome guaranteed to look like a bug.
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));
            SetupConditions(CreateMetConditions());

            SetupAccessDecisions(
                decisionVerdict: RefusedVerdict(AccessDenialReason.SelfApprovalNotPermitted),
                bypassVerdict: RefusedVerdict(AccessDenialReason.BypassNotPermitted));

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.Link,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualVerdict.BlockReasons.Should().ContainSingle();

            actualVerdict.BlockReasons.Single().Code
                .Should().Be(AccessDenialReason.SelfApprovalNotPermitted);

            actualVerdict.IsBlocked.Should().BeTrue();
            actualVerdict.CanApprove.Should().BeFalse();
            actualVerdict.IsBypassAllowedForCurrentUser.Should().BeFalse();

            // The message is composed HERE. The decision client's own explanation is built from
            // resolved policy values and must never travel outward (§14.5 rule 2), so the
            // refusing verdict's explanation token must not appear in what the caller reads.
            actualVerdict.BlockReasons.Single().Message.Should().NotBeNullOrWhiteSpace();
            actualVerdict.BlockReasons.Single().Message.Should().NotBe("refused");
        }

        [Fact]
        public async Task ShouldNotRepeatTheCallerRefusalWhenTheConditionsAlreadyReportedThatCodeAsync()
        {
            // given: the conditions report the threshold shortfall AND the decision is refused for
            // the same code. Appending it a second time would show a moderator the same sentence
            // twice and inflate every "how many things are wrong" count built on the set.
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));

            SetupConditions(CreateBlockedConditions(
                blockReasons: new List<AccessDenialReason>
                {
                    AccessDenialReason.ApprovalThresholdNotMet,
                    AccessDenialReason.BlockedByRejection,
                }));

            SetupAccessDecisions(
                decisionVerdict: RefusedVerdict(AccessDenialReason.ApprovalThresholdNotMet),
                bypassVerdict: RefusedVerdict(AccessDenialReason.BypassNotPermitted));

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.Link,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualVerdict.BlockReasons.Select(reason => reason.Code).Should().Equal(
                AccessDenialReason.ApprovalThresholdNotMet,
                AccessDenialReason.BlockedByRejection);

            actualVerdict.BlockReasons
                .Count(reason => reason.Code == AccessDenialReason.ApprovalThresholdNotMet)
                .Should().Be(1);
        }

        [Fact]
        public async Task ShouldReportBypassAvailableWhenOnlyTheBypassProbeIsPermittedAsync()
        {
            // given: the plain approve is refused and the bypass is permitted. The two are asked
            // as SEPARATE questions because they close for different reasons — a caller refused an
            // ordinary approve may still hold the standing to waive the §8.5 conditions
            // wholesale (§9.7.5). A verdict that inferred bypass availability from the plain
            // refusal would hide the approve-with-bypass route from the people who have it.
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));

            SetupConditions(CreateBlockedConditions(
                blockReasons: new List<AccessDenialReason>
                {
                    AccessDenialReason.ApprovalThresholdNotMet,
                }));

            SetupAccessDecisions(
                decisionVerdict: RefusedVerdict(AccessDenialReason.ApprovalConditionsNotMet),
                bypassVerdict: PermittedVerdict());

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.Link,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualVerdict.CanApprove.Should().BeFalse();
            actualVerdict.IsBypassAllowedForCurrentUser.Should().BeTrue();
            actualVerdict.IsBlocked.Should().BeTrue();

            // The bypass probe carries a reason. A blank one is refused as BypassReasonRequired
            // before the decision reaches the question this is actually asking, so the probe
            // would report bypass closed for everyone.
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    ApprovalDecision.Approve,
                    true,
                    It.Is<string>(bypassReason =>
                        string.IsNullOrWhiteSpace(bypassReason) == false),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and the plain question is asked without one, so it is not accidentally the bypass
            // question asked twice.
            this.accessBrokerMock.Verify(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    ApprovalDecision.Approve,
                    false,
                    It.Is<string>(bypassReason => bypassReason == null),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldReportBypassUnavailableWhenOnlyThePlainDecisionIsPermittedAsync()
        {
            // given: the inverse. The plain approve is permitted while the bypass route is closed
            // to everyone by DoNotAllowBypassingSettings (HR-4 route 3). Reading the plain answer
            // into the bypass field would offer an approve-with-bypass the policy forbids.
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));
            SetupConditions(CreateMetConditions());

            SetupAccessDecisions(
                decisionVerdict: PermittedVerdict(),
                bypassVerdict: RefusedVerdict(AccessDenialReason.BypassNotPermitted));

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.Link,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualVerdict.CanApprove.Should().BeTrue();
            actualVerdict.IsBypassAllowedForCurrentUser.Should().BeFalse();
            actualVerdict.IsBlocked.Should().BeFalse();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldKeepIsBlockedInAgreementWithTheReasonSetInBothDirectionsAsync(
            bool isSomethingBlocking)
        {
            // given: IsBlocked is DERIVED from the reason set rather than set beside it, so a
            // caller checking the flag and a caller checking the set can never reach opposite
            // conclusions — a settable flag is how they end up doing exactly that.
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));

            SetupConditions(isSomethingBlocking
                ? CreateBlockedConditions(new List<AccessDenialReason>
                {
                    AccessDenialReason.BlockedByRejection,
                })
                : CreateMetConditions());

            SetupAccessDecisions(
                decisionVerdict: isSomethingBlocking
                    ? RefusedVerdict(AccessDenialReason.ApprovalConditionsNotMet)
                    : PermittedVerdict(),
                bypassVerdict: PermittedVerdict());

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.Link,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualVerdict.IsBlocked.Should().Be(isSomethingBlocking);

            if (isSomethingBlocking)
            {
                actualVerdict.BlockReasons.Should().NotBeEmpty();
            }
            else
            {
                actualVerdict.BlockReasons.Should().BeEmpty();
            }
        }
    }
}
