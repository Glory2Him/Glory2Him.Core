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
using System.Collections.Generic;
using System.Text;
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
        public async Task ShouldThrowValidationExceptionOnApproveIfTheAccessBrokerRefusesAsync()
        {
            // given: the caller holds the global Publisher role, so the row-local tier check
            // passes and the cross-entity decision is the ONLY thing left that can refuse the
            // approve. Without that the test would pass on the row-local check alone and prove
            // nothing about the wired gate.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association decision = CreateApprovalDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);
            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalThresholdNotMet);

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is not allowed to approve " +
                        "this content item association.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            // when
            ValueTask<Association> approveAssociationTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    approveAssociationTask.AsTask);

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

            // nothing was written
            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            // and nothing was announced. A refused approve that still broadcast Approved would
            // tell every subscriber the row is live.
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
        public async Task ShouldLogTheDenialAsAWarningBeforeThrowingOnApproveAsync()
        {
            // given: §14.5 — the true reason is recorded server-side and the caller is told
            // nothing about the policy. It has to be recorded BEFORE the throw, because the
            // throw is what discards the verdict.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association decision = CreateApprovalDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);
            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalThresholdNotMet);

            var logCallOrder = new List<string>();

            this.loggingBrokerMock.Setup(broker =>
                broker.LogWarningAsync(It.IsAny<string>()))
                    .Callback<string>(message => logCallOrder.Add($"warning:{message}"))
                    .Returns(ValueTask.CompletedTask);

            this.loggingBrokerMock.Setup(broker =>
                broker.LogErrorAsync(It.IsAny<Exception>()))
                    .Callback<Exception>(_ => logCallOrder.Add("error"))
                    .Returns(ValueTask.CompletedTask);

            // when
            ValueTask<Association> approveAssociationTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<AssociationValidationException>(
                approveAssociationTask.AsTask);

            // then: the warning lands first, and the error the throw produces second
            logCallOrder.Should().HaveCount(2);
            logCallOrder[0].Should().StartWith("warning:");
            logCallOrder[1].Should().Be("error");

            // the log is the one place the reason and the explanation belong
            logCallOrder[0].Should().Contain(storageAssociation.Id.ToString());
            logCallOrder[0].Should().Contain(nameof(AccessDenialReason.ApprovalThresholdNotMet));
            logCallOrder[0].Should().Contain("refused");
        }

        [Fact]
        public async Task ShouldNotLeakTheAccessExplanationToTheCallerOnApproveDenialAsync()
        {
            // given: the verdict's Explanation is composed from resolved policy values — how
            // many approvals were required, which block fired — and the denial reason names the
            // rule. Exception messages and their Data surface outward through a public event
            // address (§14.5 rule 2), so neither may appear in anything thrown.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association decision = CreateApprovalDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);
            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalThresholdNotMet);

            // when
            ValueTask<Association> approveAssociationTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    decision,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    approveAssociationTask.AsTask);

            // then: wording of the service's own, naming no policy
            actualAssociationValidationException.InnerException.Message.Should().Be(
                "The current user is not allowed to approve this content item association.");

            string thrownText =
                FlattenExceptionText(actualAssociationValidationException);

            // the explanation the refusing verdict carried
            thrownText.Should().NotContain("refused");

            // and the name of the rule that fired
            thrownText.Should().NotContain(
                nameof(AccessDenialReason.ApprovalThresholdNotMet));

            actualAssociationValidationException.Data.Count.Should().Be(0);
            actualAssociationValidationException.InnerException.Data.Count.Should().Be(0);
        }

        [Fact]
        public async Task ShouldAskTheAccessBrokerAboutTheStoredAssociationOnApproveAsync()
        {
            // given: the caller's copy names a DIFFERENT author from the stored row. That
            // difference is what gives the assertion below its meaning — if the query were
            // built from the caller's copy, a contributor could name somebody else as author
            // and walk straight past the self-approval bar.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();
            storageAssociation.CreatedBy = $"stored-{Guid.NewGuid()}";
            storageAssociation.EntityAType = EntityType.ContentItem;
            storageAssociation.EntityAContentType = ContentType.Testimony;
            storageAssociation.EntityBType = EntityType.Tag;
            storageAssociation.EntityBContentType = null;

            Association decision = CreateApprovalDecision(storageAssociation.Id);
            decision.CreatedBy = $"caller-{Guid.NewGuid()}";

            // the service copies the approval fields onto the stored instance, so the values
            // the query should have carried are snapshotted before the act
            Guid expectedEntityId = storageAssociation.Id;
            string expectedCreatedBy = storageAssociation.CreatedBy;
            decimal? expectedConfidenceScore = storageAssociation.ConfidenceScore;

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureApprovalDecisionQueryAsync(storageAssociation, decision);

            // then
            actualQuery.Should().NotBeNull();

            // the association's OWN type, never an endpoint's
            actualQuery.EntityType.Should().Be(EntityType.Association);
            actualQuery.EntityId.Should().Be(expectedEntityId);

            // an association's policy tier is (Association, null) — an endpoint's content type
            // authorises the caller, it does not key the policy
            actualQuery.ContentType.Should().BeNull();

            actualQuery.EntityCreatedBy.Should().Be(expectedCreatedBy);
            actualQuery.EntityCreatedBy.Should().NotBe(decision.CreatedBy);
            actualQuery.ConfidenceScore.Should().Be(expectedConfidenceScore);

            // BOTH endpoints, because an association is authorised from them rather than from
            // itself and holding a role for either one is enough
            actualQuery.RoleSubjects.Should().HaveCount(2);
            actualQuery.RoleSubjects[0].EntityType.Should().Be(nameof(EntityType.ContentItem));
            actualQuery.RoleSubjects[0].ContentType.Should().Be(nameof(ContentType.Testimony));
            actualQuery.RoleSubjects[1].EntityType.Should().Be(nameof(EntityType.Tag));
            actualQuery.RoleSubjects[1].ContentType.Should().BeNull();

            // bypass is its own operation and this is not it (§12.4.4 rule 11)
            actualQuery.IsBypassRequested.Should().BeFalse();
            actualQuery.BypassReason.Should().BeNull();
        }

        [Theory]
        [InlineData(ApprovalStatus.Approved, ApprovalDecision.Approve)]
        [InlineData(ApprovalStatus.Rejected, ApprovalDecision.Reject)]
        public async Task ShouldTellTheAccessBrokerWhichWayTheApprovalIsMovingOnApproveAsync(
            ApprovalStatus callerStatus,
            ApprovalDecision expectedDecision)
        {
            // given: the two directions are gated differently — rejecting withholds approval
            // rather than granting it, so it satisfies no threshold and waives nothing. Asking
            // one question for both would leave a publisher unable to reject the very row the
            // threshold was failing to approve.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();

            Association decision = callerStatus == ApprovalStatus.Rejected
                ? CreateRejectionDecision(storageAssociation.Id)
                : CreateApprovalDecision(storageAssociation.Id);

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureApprovalDecisionQueryAsync(storageAssociation, decision);

            // then
            actualQuery.Should().NotBeNull();
            actualQuery.Decision.Should().Be(expectedDecision);
        }

        // Runs a permitted approve end to end and hands back the query the service gave the
        // access broker. Permitted rather than refused because the whole operation should run:
        // the query is built before the verdict is read, so this is the query a real approve
        // sends.
        private async ValueTask<ApprovalDecisionQuery> CaptureApprovalDecisionQueryAsync(
            Association storageAssociation,
            Association decision)
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
                            IsBypassUsed = false,
                            BypassedBlockReason = AccessDenialReason.None,
                            Explanation = "permitted",
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

            await this.associationService.TransitionAssociationApprovalAsync(
                decision,
                TestContext.Current.CancellationToken);

            return actualQuery;
        }

        // Everything a caller could read off what was thrown: every message in the chain and
        // every key and value in every Data dictionary. The leak guard asserts against this
        // rather than against the message alone, because Data surfaces outward too.
        private static string FlattenExceptionText(Exception exception)
        {
            var builder = new StringBuilder();

            for (Exception current = exception;
                current is not null;
                current = current.InnerException)
            {
                builder.AppendLine(current.Message);

                foreach (DictionaryEntry entry in current.Data)
                {
                    builder.AppendLine(Convert.ToString(entry.Key));

                    if (entry.Value is IEnumerable<string> values)
                    {
                        builder.AppendLine(string.Join(" ", values));

                        continue;
                    }

                    builder.AppendLine(Convert.ToString(entry.Value));
                }
            }

            return builder.ToString();
        }
    }
}
