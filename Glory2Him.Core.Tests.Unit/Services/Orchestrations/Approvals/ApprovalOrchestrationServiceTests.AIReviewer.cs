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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// Berean's half of the invitation flow (§8.6.2): asking for it, asking again once it has
    /// finished, withdrawing it, and reporting where it stands.
    ///
    /// <para>The three upsert branches are the reason this suite exists at all. One click means
    /// "create", "reset" or "nothing to do" depending on what the round's single possible row is
    /// doing, and nothing about the request itself distinguishes them — so each branch is reached
    /// from its own arrangement and asserted on the WRITE it made, never only on what came
    /// back.</para>
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        // §8.6.2's switch, answered off IAccessBroker's own narrow member rather than gathered
        // with the reviewer scope. Unstubbed it answers null, which the service reads as "not
        // offered" — so a test wanting the feature ON has to say so, and one about the fail-closed
        // reading says nothing at all.
        private void SetupAIReviewerOffer(bool isOffered) =>
            this.accessBrokerMock.Setup(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AIReviewerPolicyVerdict { IsOffered = isOffered });

        // The round's ONE live assignment, or the absence of one. Keyed on the approval rather
        // than It.IsAny so a test cannot pass by answering a question about a different round.
        private void SetupStoredAIReviewerAssignment(
            Guid approvalId,
            AIReviewerAssignment storageAssignment) =>
            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssignment);

        // The foundation echoes back what it wrote, so a test can assert on the returned row and
        // on the argument and know they are the same thing.
        private void SetupAIReviewerAssignmentWrites()
        {
            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AIReviewerAssignment assignment, CancellationToken _) =>
                            assignment);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AIReviewerAssignment assignment, CancellationToken _) =>
                            assignment);
        }

        private static AIReviewerAssignment CreateAIReviewerAssignment(
            Guid approvalId,
            bool isAIReviewCompleted = false,
            bool isAIReviewCommentsPresent = false) =>
            new AIReviewerAssignment
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                IsAIReviewCompleted = isAIReviewCompleted,
                IsAIReviewCommentsPresent = isAIReviewCommentsPresent,
            };

        /// <summary>
        /// THE CREATE BRANCH. Nothing stands, so a pending row is written — and the row carries
        /// the approval the SCOPE resolved, not an id the caller could name: the request names an
        /// entity, and which round that entity is on is decided from storage.
        /// </summary>
        [Fact]
        public async Task ShouldAssignTheAIReviewerWhenTheRoundHasNoneAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(approvalId, storageAssignment: null);
            SetupAIReviewerAssignmentWrites();

            // when
            AIReviewerAssignment actualAssignment =
                await this.approvalOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: born pending, on this round
            actualAssignment.ApprovalId.Should().Be(approvalId);
            actualAssignment.Id.Should().NotBe(Guid.Empty);
            actualAssignment.IsAIReviewCompleted.Should().BeFalse();
            actualAssignment.IsAIReviewCommentsPresent.Should().BeFalse();

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.Is<AIReviewerAssignment>(assignment =>
                        assignment.ApprovalId == approvalId
                            && assignment.Id != Guid.Empty
                            && assignment.IsAIReviewCompleted == false
                            && assignment.IsAIReviewCommentsPresent == false),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and nothing was reset — the two writes are alternatives, never both
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// THE NO-OP BRANCH. A pending assignment already stands, so asking again has nothing to
        /// do and the standing row goes back — §7.9 rule 4's "the server dissolves a duplicate
        /// quietly", applied to the reviewer that is not a person.
        ///
        /// <para>The absence of a write is the assertion. Handing the row back while ALSO writing
        /// would spend an <c>AIReviewerAssignment-Modified</c> fact restating what storage already
        /// says, and would reset a pending pass that had started.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnTheStandingAssignmentWhenBereanIsAlreadyPendingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            AIReviewerAssignment pendingAssignment =
                CreateAIReviewerAssignment(approvalId, isAIReviewCompleted: false);

            SetupReviewerScope(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(approvalId, pendingAssignment);
            SetupAIReviewerAssignmentWrites();

            // when
            AIReviewerAssignment actualAssignment =
                await this.approvalOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualAssignment.Id.Should().Be(pendingAssignment.Id);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// THE RESET BRANCH — the re-request. A completed assignment is put back to pending on the
        /// SAME row rather than replaced: withdrawing and re-adding would lose the record that
        /// Berean was ever on this round, and there is no second row for it to live on.
        ///
        /// <para>Both flags are asserted, not just the completion one. <c>IsAIReviewCommentsPresent</c>
        /// records what a finished pass left behind, and a pass that is about to run again has left
        /// nothing — the foundation refuses the pair standing the other way round.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnACompletedAIReviewerAssignmentToPendingWhenAskedAgainAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();

            AIReviewerAssignment completedAssignment = CreateAIReviewerAssignment(
                approvalId,
                isAIReviewCompleted: true,
                isAIReviewCommentsPresent: true);

            SetupReviewerScope(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(approvalId, completedAssignment);
            SetupAIReviewerAssignmentWrites();

            // when
            AIReviewerAssignment actualAssignment =
                await this.approvalOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualAssignment.Id.Should().Be(completedAssignment.Id);
            actualAssignment.IsAIReviewCompleted.Should().BeFalse();
            actualAssignment.IsAIReviewCommentsPresent.Should().BeFalse();

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.Is<AIReviewerAssignment>(assignment =>
                        assignment.Id == completedAssignment.Id
                            && assignment.IsAIReviewCompleted == false
                            && assignment.IsAIReviewCommentsPresent == false),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// §8.6.2's switch is a gate on the WRITE, not decoration on the picker. A caller posting
        /// straight at the endpoint never saw the picker at all, so the offer is resolved fresh
        /// here and a false one refuses.
        ///
        /// <para>Delete the check and this is the only test that notices: every other case in this
        /// file arranges the feature ON, because that is the arrangement in which the branches it
        /// is about exist.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRefuseToAssignTheAIReviewerWhenItIsNotOfferedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: false);
            SetupAIReviewerAssignmentWrites();

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.approvalOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then: a 400, not a 424 — the caller asked for something this round does not offer
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

            // and the refusal came BEFORE the round was read for an assignment, so a switched-off
            // feature costs no foundation call at all
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// FAIL-CLOSED (§8.4 rule 2), and the case the gate's own comment promises: the broker
        /// answers <c>null</c> for a round whose policy it could not resolve, and an unread verdict
        /// reaches the gate looking exactly like a switched-off one.
        ///
        /// <para>Distinct from the case above because the two arrive by different routes and only
        /// one of them involves anybody's decision. A collapse that treated the absence as
        /// permission would open the feature precisely when the system had stopped being able to
        /// say whether it was on.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRefuseToAssignTheAIReviewerWhenThePolicyCannotBeResolvedAsync()
        {
            // given: nothing stubs the policy read, so it answers null
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);
            SetupAIReviewerAssignmentWrites();

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.approvalOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// §7.9 rule 7's window, which applies to Berean identically: an assignment on a round
        /// that is not open could never be answered, because a review may only be written while
        /// the approval is <c>Submitted</c> (§7.7 rule 2b).
        ///
        /// <para>Both closed outcomes and the pre-submission state are named, because the three
        /// fail for the same reason and a guard written against one of them would let the others
        /// through.</para>
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldRefuseToAssignTheAIReviewerWhenTheRoundIsNotOpenAsync(
            ApprovalStatus closedStatus)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId, approvalStatus: closedStatus);
            SetupAIReviewerOffer(isOffered: true);
            SetupAIReviewerAssignmentWrites();

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.approvalOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();

            // and the window is asked BEFORE the policy, so a closed round costs no policy
            // resolution — the ordering, not merely the outcome
            this.accessBrokerMock.Verify(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The shape check runs before anything is resolved, so a malformed request never reaches
        /// storage. Both halves of the entity key are covered: an empty id names no row, and an
        /// integer outside the enum names no type the not-found message could even print.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRequestAIReviewerIfEntityIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            var invalidEntityId = Guid.Empty;

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval orchestration request is invalid, "
                        + "fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: "EntityId",
                value: "Id is required");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.approvalOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    invalidEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            // nothing was resolved, and no caller was even minted — the check precedes the
            // envelope, which is what keeps a malformed request off the storage path entirely
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRequestAIReviewerIfEntityTypeIsUndefinedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            var undefinedEntityType = (EntityType)97;

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval orchestration request is invalid, "
                        + "fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(EntityType),
                value: "Value is not a recognized entity type");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.approvalOrchestrationService.RequestAIReviewerAsync(
                    undefinedEntityType,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The requesting tier is the whole review tier (§7.9 rule 2) — asking for Berean is
        /// coordination, exactly like asking a person — so the caller who must be turned away is
        /// one holding no review standing at all.
        /// </summary>
        [Theory]
        [MemberData(nameof(NonModerationRoleSets))]
        public async Task ShouldRefuseToAssignTheAIReviewerToACallerOutsideTheReviewTierAsync(
            string[] roles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);
            SetupAIReviewerAssignmentWrites();

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.approvalOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then: an unauthorized inner, which the exposer maps to 401 rather than 400
            actualException.InnerException.Should()
                .BeOfType<UnauthorizedApprovalOrchestrationException>();

            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The ordinary withdrawal: the standing assignment is removed through the foundation,
        /// keyed on the row the SCOPE resolved rather than on an id the caller supplied — a
        /// moderation panel names the entity, never the assignment.
        /// </summary>
        [Fact]
        public async Task ShouldWithdrawTheStandingAIReviewerAssignmentAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            AIReviewerAssignment standingAssignment =
                CreateAIReviewerAssignment(approvalId);

            SetupReviewerScope(approvalId: approvalId);
            SetupStoredAIReviewerAssignment(approvalId, standingAssignment);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.RemoveAIReviewerAssignmentByIdAsync(
                    standingAssignment.Id,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(standingAssignment);

            // when
            AIReviewerAssignment actualAssignment =
                await this.approvalOrchestrationService.WithdrawAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualAssignment.Id.Should().Be(standingAssignment.Id);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.RemoveAIReviewerAssignmentByIdAsync(
                    standingAssignment.Id,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Idempotent: nothing standing is a no-op answering <c>null</c>, which the exposer turns
        /// into a <c>204</c>. The same posture the human withdrawal takes, for the same reason — a
        /// stale panel is not a mistake, and telling a moderator "not found" for a state they were
        /// trying to reach anyway is a refusal they can do nothing with.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNothingWhenWithdrawingAnAIReviewerThatWasNeverAssignedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);
            SetupStoredAIReviewerAssignment(approvalId, storageAssignment: null);

            // when
            AIReviewerAssignment actualAssignment =
                await this.approvalOrchestrationService.WithdrawAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualAssignment.Should().BeNull();

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.RemoveAIReviewerAssignmentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// THE ASYMMETRY, and it is deliberate. The request path resolves §8.6.2's offer and
        /// refuses without it; withdrawal does not ask at all — switching the feature off must not
        /// strand the assignments made while it was on, and taking Berean OFF a round is the one
        /// act that has to keep working after the switch closes.
        ///
        /// <para>Arranged with the feature explicitly OFF, because a test that left it on could
        /// not tell "does not ask" from "asked and was allowed".</para>
        /// </summary>
        [Fact]
        public async Task ShouldWithdrawTheAIReviewerEvenAfterTheOfferHasBeenSwitchedOffAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            AIReviewerAssignment strandedAssignment =
                CreateAIReviewerAssignment(approvalId, isAIReviewCompleted: true);

            SetupReviewerScope(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: false);
            SetupStoredAIReviewerAssignment(approvalId, strandedAssignment);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.RemoveAIReviewerAssignmentByIdAsync(
                    strandedAssignment.Id,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(strandedAssignment);

            // when
            AIReviewerAssignment actualAssignment =
                await this.approvalOrchestrationService.WithdrawAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualAssignment.Id.Should().Be(strandedAssignment.Id);

            this.accessBrokerMock.Verify(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The SAME tier gates the withdrawal, and it is the ONLY gate this operation has.
        /// Nothing else stands between a caller and taking Berean off a round: the §8.6.2 offer
        /// is deliberately not asked here, and neither is the round-open window — so deleting it
        /// leaves a caller with no review standing, or one carrying a ReadOnly block, able to
        /// withdraw the reviewer with nothing else to notice.
        /// </summary>
        [Theory]
        [MemberData(nameof(NonModerationRoleSets))]
        public async Task ShouldRefuseToWithdrawTheAIReviewerForACallerOutsideTheReviewTierAsync(
            string[] roles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);

            SetupStoredAIReviewerAssignment(
                approvalId,
                CreateAIReviewerAssignment(approvalId));

            // when
            ValueTask<AIReviewerAssignment> withdrawTask =
                this.approvalOrchestrationService.WithdrawAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    withdrawTask.AsTask);

            // then: an unauthorized inner, which the exposer maps to 401 rather than 400
            actualException.InnerException.Should()
                .BeOfType<UnauthorizedApprovalOrchestrationException>();

            // and the standing assignment was neither read nor removed — the gate runs on the
            // envelope, before the round is resolved at all
            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The status read with an assignment standing: the two flags are projected off the STORED
        /// row rather than defaulted, which is the whole of what a panel renders as "Berean has
        /// finished, and left comments".
        /// </summary>
        [Fact]
        public async Task ShouldReportTheAIReviewerStatusOfAStandingAssignmentAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            AIReviewerAssignment completedAssignment = CreateAIReviewerAssignment(
                approvalId,
                isAIReviewCompleted: true,
                isAIReviewCommentsPresent: true);

            SetupReviewerScope(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(approvalId, completedAssignment);

            // when
            AIReviewerStatus actualStatus =
                await this.approvalOrchestrationService.RetrieveAIReviewerStatusAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualStatus.IsOffered.Should().BeTrue();
            actualStatus.IsRequested.Should().BeTrue();
            actualStatus.IsAIReviewCompleted.Should().BeTrue();
            actualStatus.IsAIReviewCommentsPresent.Should().BeTrue();
        }

        /// <summary>
        /// And with nothing assigned. <c>IsRequested</c> is the absence of the row and the two
        /// flags fall to <c>false</c> — a picker still has to be told the feature is OFFERED, which
        /// is the case this read exists to serve and the one an "assignment or not-found" shape
        /// could not answer.
        /// </summary>
        [Fact]
        public async Task ShouldReportTheAIReviewerAsOfferedButUnrequestedWhenNothingIsAssignedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(approvalId, storageAssignment: null);

            // when
            AIReviewerStatus actualStatus =
                await this.approvalOrchestrationService.RetrieveAIReviewerStatusAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualStatus.IsOffered.Should().BeTrue();
            actualStatus.IsRequested.Should().BeFalse();
            actualStatus.IsAIReviewCompleted.Should().BeFalse();
            actualStatus.IsAIReviewCommentsPresent.Should().BeFalse();
        }

        /// <summary>
        /// The read fails closed on the same unresolved policy the write does. A panel told the
        /// feature is offered renders a control whose POST would be refused, so the two answers
        /// have to agree about an absent verdict as well as about a false one.
        /// </summary>
        [Fact]
        public async Task ShouldReportTheAIReviewerAsNotOfferedWhenThePolicyCannotBeResolvedAsync()
        {
            // given: nothing stubs the policy read, so it answers null
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);
            SetupStoredAIReviewerAssignment(approvalId, storageAssignment: null);

            // when
            AIReviewerStatus actualStatus =
                await this.approvalOrchestrationService.RetrieveAIReviewerStatusAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualStatus.IsOffered.Should().BeFalse();
        }

        /// <summary>
        /// The status read is NOT gated on the round being open, unlike the request beside it. A
        /// closed round still renders its panel, and a reviewer looking at a decided round must be
        /// able to see that Berean ran on it — refusing here would blank the history rather than
        /// prevent an action.
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldReportTheAIReviewerStatusOnAClosedRoundAsync(
            ApprovalStatus closedStatus)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            AIReviewerAssignment completedAssignment = CreateAIReviewerAssignment(
                approvalId,
                isAIReviewCompleted: true);

            SetupReviewerScope(approvalId: approvalId, approvalStatus: closedStatus);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(approvalId, completedAssignment);

            // when
            AIReviewerStatus actualStatus =
                await this.approvalOrchestrationService.RetrieveAIReviewerStatusAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualStatus.IsRequested.Should().BeTrue();
            actualStatus.IsAIReviewCompleted.Should().BeTrue();
        }

        /// <summary>
        /// And the read is gated on the same tier as the two writes. Who Berean has been assigned
        /// to, and how far its pass has gotten, is moderation coordination (§14.5) — never public
        /// and never for everyone who happens to be signed in.
        ///
        /// <para>The foundation's own read gate would answer not-found for the assignment, but
        /// that is not the whole of what this operation returns: <c>IsOffered</c> comes from the
        /// §8.6.2 policy read beside it, so a caller past this gate would still learn whether the
        /// feature is on for somebody else's round.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(NonModerationRoleSets))]
        public async Task ShouldRefuseTheAIReviewerStatusToACallerOutsideTheReviewTierAsync(
            string[] roles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);

            SetupStoredAIReviewerAssignment(
                approvalId,
                CreateAIReviewerAssignment(approvalId, isAIReviewCompleted: true));

            // when
            ValueTask<AIReviewerStatus> statusTask =
                this.approvalOrchestrationService.RetrieveAIReviewerStatusAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    statusTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<UnauthorizedApprovalOrchestrationException>();

            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();

            // and the policy was never resolved either, so the refusal leaks nothing about
            // whether the feature is on for this round
            this.accessBrokerMock.Verify(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveAIReviewerStatusIfEntityIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            var invalidEntityId = Guid.Empty;

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval orchestration request is invalid, "
                        + "fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: "EntityId",
                value: "Id is required");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<AIReviewerStatus> statusTask =
                this.approvalOrchestrationService.RetrieveAIReviewerStatusAsync(
                    EntityType.ContentItem,
                    invalidEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    statusTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnWithdrawAIReviewerIfEntityTypeIsUndefinedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            var undefinedEntityType = (EntityType)97;

            var invalidApprovalOrchestrationException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval orchestration request is invalid, "
                        + "fix the errors and try again.");

            invalidApprovalOrchestrationException.UpsertDataList(
                key: nameof(EntityType),
                value: "Value is not a recognized entity type");

            var expectedValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalOrchestrationException);

            // when
            ValueTask<AIReviewerAssignment> withdrawTask =
                this.approvalOrchestrationService.WithdrawAIReviewerAsync(
                    undefinedEntityType,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    withdrawTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.aiReviewerAssignmentServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
