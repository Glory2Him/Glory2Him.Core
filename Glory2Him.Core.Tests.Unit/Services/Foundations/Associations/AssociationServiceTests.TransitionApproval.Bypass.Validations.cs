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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnBypassApproveIfTheAccessBrokerRefusesAsync()
        {
            // given: the caller holds the global Publishers role, so the row-local tier check
            // passes and the cross-entity decision is the ONLY thing left that can refuse. The
            // policy closed the bypass route entirely (DoNotAllowBypassingSettings), which the
            // client reports as BypassNotPermitted — the one refusal nobody outranks, publishers
            // and administrators included.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association decision = CreateApprovalDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);
            SetupAccessBrokerToRefuse(AccessDenialReason.BypassNotPermitted);

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is not allowed to approve " +
                        "this content item association.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            decision.IsApprovedByBypass = true;
            decision.ApprovedByBypassReason = GetRandomString();

            // when
            ValueTask<Association> bypassApproveAssociationTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    bypassApproveAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectAssociationByIdAsync(
                        decision.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // nothing was written. A refused bypass that still saved would record a waiver the
            // policy forbade outright.
            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            // and nothing was announced — a refused bypass that still broadcast Approved would
            // tell every subscriber the row is live
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishAssociationAsync(
                        It.IsAny<EventEnvelope<Association>>(),
                        It.IsAny<AssociationEventOperation>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogWarningAsync(It.IsAny<string>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogErrorAsync(It.Is(
                        SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldLogTheDenialAsAWarningBeforeThrowingOnBypassApproveAsync()
        {
            // given: §14.5 — the true reason is recorded server-side and the caller is told
            // nothing about the policy. It has to be recorded BEFORE the throw, because the
            // throw is what discards the verdict; a refused bypass with no log leaves the
            // attempt itself invisible, which is the opposite of why the verb is auditable.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association decision = CreateApprovalDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);
            SetupAccessBrokerToRefuse(AccessDenialReason.BypassNotPermitted);

            var logCallOrder = new List<string>();

            this.loggingBrokerMock.Setup(broker =>
                broker.LogWarningAsync(It.IsAny<string>()))
                    .Callback<string>(message => logCallOrder.Add($"warning:{message}"))
                    .Returns(ValueTask.CompletedTask);

            this.loggingBrokerMock.Setup(broker =>
                broker.LogErrorAsync(It.IsAny<Exception>()))
                    .Callback<Exception>(_ => logCallOrder.Add("error"))
                    .Returns(ValueTask.CompletedTask);

            decision.IsApprovedByBypass = true;
            decision.ApprovedByBypassReason = GetRandomString();

            // when
            ValueTask<Association> bypassApproveAssociationTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<AssociationValidationException>(
                bypassApproveAssociationTask.AsTask);

            // then: the warning lands first, and the error the throw produces second
            logCallOrder.Should().HaveCount(2);
            logCallOrder[0].Should().StartWith("warning:");
            logCallOrder[1].Should().Be("error");

            // the log is the one place the row, the reason and the explanation belong
            logCallOrder[0].Should().Contain(storageAssociation.Id.ToString());
            logCallOrder[0].Should().Contain(nameof(AccessDenialReason.BypassNotPermitted));
            logCallOrder[0].Should().Contain("refused");
        }

        [Fact]
        public async Task ShouldNotLeakTheAccessExplanationToTheCallerOnBypassApproveDenialAsync()
        {
            // given: the verdict's Explanation is composed from resolved policy values — which
            // role a bypass needs, which block fired — and the denial reason names the rule.
            // Exception messages and their Data surface outward through a public event address
            // (§14.5 rule 2), so neither may appear in anything thrown. A refused bypass is the
            // sharpest case: the refusal itself tells a prober the route exists.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association decision = CreateApprovalDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);
            SetupAccessBrokerToRefuse(AccessDenialReason.BypassNotPermitted);

            decision.IsApprovedByBypass = true;
            decision.ApprovedByBypassReason = GetRandomString();

            // when
            ValueTask<Association> bypassApproveAssociationTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    bypassApproveAssociationTask.AsTask);

            // then: wording of the service's own, naming no policy — and identical to the
            // ordinary approve's, so the message does not even disclose that a bypass was tried
            actualAssociationValidationException.InnerException.Message.Should().Be(
                "The current user is not allowed to approve this content item association.");

            string thrownText =
                FlattenExceptionText(actualAssociationValidationException);

            // the explanation the refusing verdict carried
            thrownText.Should().NotContain("refused");

            // and the name of the rule that fired
            thrownText.Should().NotContain(
                nameof(AccessDenialReason.BypassNotPermitted));

            actualAssociationValidationException.Data.Count.Should().Be(0);
            actualAssociationValidationException.InnerException.Data.Count.Should().Be(0);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldThrowValidationExceptionOnBypassApproveIfTheReasonIsBlankAsync(
            string blankReason)
        {
            // given: a bypass is only tolerable because it leaves a record, and an unexplained
            // one records nothing worth reading. Refused before storage is touched, so an
            // unexplained bypass never even locates the row it was aimed at.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association decision = CreateApprovalDecision(Guid.NewGuid());

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.ApprovedByBypassReason),
                values: "Bypass reason is required when bypassing.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationException);

            decision.IsApprovedByBypass = true;
            decision.ApprovedByBypassReason = blankReason;

            // when
            ValueTask<Association> bypassApproveAssociationTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    bypassApproveAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnBypassApproveIfTheReasonExceedsMaxLengthAsync()
        {
            // given: the column this lands in is nvarchar(500). Without the bound the same
            // payload comes back from SQL Server as a "contact support" dependency failure
            // naming no field at all — and it comes back AFTER the approve was decided and the
            // fact published, so the row and its audience disagree about what happened.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association decision = CreateApprovalDecision(Guid.NewGuid());
            string overlongReason = GetRandomStringWithLengthOf(501);

            // the draw has to actually exceed the bound, or this test asserts nothing
            overlongReason.Length.Should().Be(501);

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.ApprovedByBypassReason),
                values: "Text exceed max length of 500 characters");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationException);

            decision.IsApprovedByBypass = true;
            decision.ApprovedByBypassReason = overlongReason;

            // when
            ValueTask<Association> bypassApproveAssociationTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    bypassApproveAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAskTheAccessBrokerForABypassOnBypassApproveAsync()
        {
            // given: the three members that ARE the bypass. Everything else in the query matches
            // the ordinary approve's, and is built from STORAGE for the same reason — a
            // caller-supplied author or endpoint content type would be self-certification, and a
            // bypass is the last place to accept one.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association storageAssociation = CreateApprovableStorageAssociation();
            storageAssociation.CreatedBy = $"stored-{Guid.NewGuid()}";
            storageAssociation.EntityAType = EntityType.ContentItem;
            storageAssociation.EntityAContentType = ContentType.Testimony;
            storageAssociation.EntityBType = EntityType.Tag;
            storageAssociation.EntityBContentType = null;

            Association decision = CreateApprovalDecision(storageAssociation.Id);
            decision.CreatedBy = $"caller-{Guid.NewGuid()}";

            string bypassReason = $"argument-{Guid.NewGuid()}";

            // the service copies the approval fields onto the stored instance, so the values the
            // query should have carried are snapshotted before the act
            Guid expectedEntityId = storageAssociation.Id;
            string expectedCreatedBy = storageAssociation.CreatedBy;
            decimal? expectedConfidenceScore = storageAssociation.ConfidenceScore;

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureBypassApprovalDecisionQueryAsync(
                    storageAssociation,
                    decision,
                    bypassReason);

            // then
            actualQuery.Should().NotBeNull();

            // the bypass is DECLARED. Asking the ordinary question and then writing the waiver
            // anyway would record a bypass the decision never granted.
            actualQuery.IsBypassRequested.Should().BeTrue();

            // and the reason travels with it — the client refuses a bypass that carries none
            actualQuery.BypassReason.Should().Be(bypassReason);

            // fixed to Approve: a bypass exists to let something through, and there is nothing
            // to waive in refusing
            actualQuery.Decision.Should().Be(ApprovalDecision.Approve);

            // the association's OWN type, never an endpoint's, and its policy tier is
            // (Association, null)
            actualQuery.EntityType.Should().Be(EntityType.Association);
            actualQuery.EntityId.Should().Be(expectedEntityId);
            actualQuery.ContentType.Should().BeNull();

            actualQuery.EntityCreatedBy.Should().Be(expectedCreatedBy);
            actualQuery.EntityCreatedBy.Should().NotBe(decision.CreatedBy);
            actualQuery.ConfidenceScore.Should().Be(expectedConfidenceScore);

            actualQuery.RoleSubjects.Should().HaveCount(2);
            actualQuery.RoleSubjects[0].EntityType.Should().Be(nameof(EntityType.ContentItem));
            actualQuery.RoleSubjects[0].ContentType.Should().Be(nameof(ContentType.Testimony));
            actualQuery.RoleSubjects[1].EntityType.Should().Be(nameof(EntityType.Tag));
            actualQuery.RoleSubjects[1].ContentType.Should().BeNull();
        }

        [Theory]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldThrowValidationExceptionOnBypassApproveIfStatusIsNotAnApprovalAsync(
            ApprovalStatus notAnApproval)
        {
            // given: NARROWER than the transition itself, which admits all three targets.
            // Rejected is the row that matters here, and it is refused: there is no such thing
            // as a bypass-reject. A rejection withholds approval rather than granting it, so
            // nothing is being waived, DoNotAllowBypassingSettings does not gate it and
            // IsApprovedByBypass stays false (§9.7.5) — and rejecting is already unconditional
            // through the same verb, so nothing is lost by closing this door. Re-opening to
            // Submitted is refused for the same reason: it decides nothing, so it waives nothing.
            //
            // Admitting one would go wrong three ways at once: the row would be stamped
            // IsApprovedByBypass on a REJECTION, the access decision would be taken out for
            // Decision = Approve, and the fact published would be Approved — telling every
            // subscriber the opposite of what happened.
            //
            // Draft and Dismissed are absent because they are refused one rule earlier, as
            // targets the transition does not accept at all.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association decision = CreateApprovalDecision(Guid.NewGuid());
            decision.ApprovalStatus = notAnApproval;

            // so the bypass rule is the ONLY one that can fire — otherwise a green result here
            // could be coming from the published-without-approval rule instead
            decision.IsPublished = false;
            decision.PublishDate = null;

            decision.IsApprovedByBypass = true;
            decision.ApprovedByBypassReason = GetRandomString();

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.IsApprovedByBypass),
                values: "Bypass requires an approved content item association.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationException);

            // when
            ValueTask<Association> bypassApproveAssociationTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    bypassApproveAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            // refused before the row was even located: nothing was written, nothing was
            // announced, and no bypass decision was taken out on the policy's behalf
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnBypassApproveIfCallerIsNotInThePublisherTierAsync()
        {
            // given: a caller holding a review role and nothing more. A bypass widens WHICH
            // conditions may be waived, never who is standing at the door — HR-3 keeps the
            // decision out of reviewers' hands whichever route it takes.
            //
            // The access broker is deliberately left on the fixture's PERMISSIVE default, because
            // it is the only other thing in this operation that could refuse: with it permitting,
            // a refusal can have come from nothing but the row-local tier gate. And that gate runs
            // FIRST — an unauthorised caller costs one role comparison instead of four table reads
            // — which is what the VerifyNoOtherCalls on the broker below pins, and what
            // distinguishes this gate from the client-side one.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association decision = CreateApprovalDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is not allowed to approve " +
                        "this content item association.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            decision.IsApprovedByBypass = true;
            decision.ApprovedByBypassReason = GetRandomString();

            // when
            ValueTask<Association> bypassApproveAssociationTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    bypassApproveAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            // the row IS loaded first — the tier is resolved from the STORED endpoints, never
            // from the caller's copy
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectAssociationByIdAsync(
                        decision.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // and the decision was never taken out. THE assertion of this test: a refusal that
            // arrived only after the broker answered would be the client-side gate doing the
            // work, and deleting the row-local one would then cost nothing.
            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishAssociationAsync(
                        It.IsAny<EventEnvelope<Association>>(),
                        It.IsAny<AssociationEventOperation>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnBypassApproveIfStoredRowIsNotInReviewAsync(
            ApprovalStatus storedStatus)
        {
            // given: only a row actually in review can be decided, and a bypass does not lift
            // that. What a bypass waives are the §8.5 approval CONDITIONS — the threshold, a
            // standing rejection, an unresolved comment — not the requirement that there be a
            // submission to decide on. Bypassing a Draft would skip the submission the whole
            // workflow is built around, and a Dismissed row is not in a round at all.
            //
            // Approved and Rejected are absent because they no longer fail HERE: they are
            // transitionable by an administrator through the override, so a publisher meeting one is
            // refused earlier, at the override gate, and never reaches the access decision this
            // asserts was taken.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association storageAssociation = CreateStorageAssociationInStatus(storedStatus);
            Association decision = CreateApprovalDecision(storageAssociation.Id);
            decision.IsApprovedByBypass = true;
            decision.ApprovedByBypassReason = GetRandomString();

            SetupStorageRead(storageAssociation);
            SetupAccessBrokerToPermitByBypass(AccessDenialReason.ApprovalThresholdNotMet);

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken));

            // the decision was permitted and the row was still refused — the precondition sits
            // AFTER the access gate and is the only thing left that can stop this
            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            // and no Approved fact went out for a row that was never approvable
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishAssociationAsync(
                        It.IsAny<EventEnvelope<Association>>(),
                        It.IsAny<AssociationEventOperation>()),
                Times.Never);
        }

        // Runs a permitted bypass-approve end to end and hands back the query the service gave
        // the access broker. Permitted rather than refused because the whole operation should
        // run: the query is built before the verdict is read, so this is the query a real bypass
        // sends.
        private async ValueTask<ApprovalDecisionQuery> CaptureBypassApprovalDecisionQueryAsync(
            Association storageAssociation,
            Association decision,
            string bypassReason)
        {
            ApprovalDecisionQuery actualQuery = null;

            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalAsync(
                    It.IsAny<ApprovalDecisionQuery>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ApprovalDecisionQuery, CancellationToken>(
                            (approvalDecisionQuery, _) => actualQuery = approvalDecisionQuery)
                        .ReturnsAsync(new AccessVerdict
                        {
                            IsPermitted = true,
                            DenialReason = AccessDenialReason.None,
                            IsBypassUsed = true,
                            BypassedBlockReason = AccessDenialReason.ApprovalThresholdNotMet,
                            Explanation = "permitted by bypass",
                        });

            SetupStorageRead(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<AssociationEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            decision.IsApprovedByBypass = true;
            decision.ApprovedByBypassReason = bypassReason;

            await this.associationService.TransitionAssociationApprovalAsync(
                decision,
                TestContext.Current.CancellationToken);

            return actualQuery;
        }
    }
}
