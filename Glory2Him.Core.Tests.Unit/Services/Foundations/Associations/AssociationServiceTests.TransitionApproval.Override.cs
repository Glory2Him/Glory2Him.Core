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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    /// <summary>
    /// The two things the widened transition verb added beyond folding the bypass in: the
    /// <c>Admin</c> override out of a terminal state, and the system identity as a second
    /// admissible actor.
    /// </summary>
    public partial class AssociationServiceTests
    {
        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldThrowUnauthorizedOnTransitionIfAPublisherOverridesATerminalRowAsync(
            ApprovalStatus terminalStatus)
        {
            // given: the publisher tier decides a SUBMITTED row. Moving one back out of a
            // terminal state is an override, and a state a Publisher could edit out of would not
            // be terminal at all (§3.4 rules 7 and 16, §8.6 HR-4).
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateTerminalStorageAssociation(terminalStatus);
            Association inputAssociation = CreateReopenDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is not allowed to transition " +
                        "this content item association.");

            // when
            ValueTask<Association> transitionTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    transitionTask.AsTask);

            // then: refused row-local, before the cross-entity decision is asked
            actualException.InnerException.Should()
                .BeEquivalentTo(unauthorizedAssociationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowUnauthorizedOnTransitionIfANonAdminOverridesATerminalRowAsync(
            string[] roles)
        {
            // given: the owner and the Reviewer are refused the override too, and by the SAME
            // gate — it runs before the publisher-tier check, so the message names the override
            // rather than the approve.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            Association storageAssociation =
                CreateTerminalStorageAssociation(ApprovalStatus.Approved);

            Association inputAssociation = CreateReopenDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is not allowed to transition " +
                        "this content item association.");

            // when
            ValueTask<Association> transitionTask =
                this.associationService.TransitionAssociationApprovalAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    transitionTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeEquivalentTo(unauthorizedAssociationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldReopenATerminalRowAndUnpublishItAsAdminAsync(
            ApprovalStatus terminalStatus)
        {
            // given: the one route out of a terminal state (§8.6 HR-4). An approved row is
            // published, so this is also where the unpublish-on-the-way-out rule is proved: a
            // re-opened row must not stay publicly visible while it waits for a second verdict.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin);

            Association storageAssociation = CreateTerminalStorageAssociation(terminalStatus);
            Association inputAssociation = CreateReopenDecision(storageAssociation.Id);

            // when
            Association savedAssociation = await CaptureSavedAssociationOnApproveAsync(
                storageAssociation,
                inputAssociation);

            // then
            savedAssociation.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            savedAssociation.IsPublished.Should().BeFalse();
            savedAssociation.PublishDate.Should().BeNull();

            // the fact follows the decision: re-opening a round is what the Submitted address
            // means, and it is a fact with no request address behind it because an association
            // has no submit verb
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishAssociationAsync(
                        It.IsAny<EventEnvelope<Association>>(),
                        AssociationEventOperation.Submitted),
                Times.Once);

            // re-opening decides nothing, so there is no approval decision to ask for
            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldUnpublishWhenAnAdminOverridesAnApprovedRowToRejectedAsync()
        {
            // given: the same unpublish rule on the other override target. Nothing republishes
            // whatever this demoted — the group simply has no public row until something is
            // approved again.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin);

            Association storageAssociation =
                CreateTerminalStorageAssociation(ApprovalStatus.Approved);

            Association inputAssociation = CreateRejectionDecision(storageAssociation.Id);

            SetupAccessBrokerToPermit();

            // when
            Association savedAssociation = await CaptureSavedAssociationOnApproveAsync(
                storageAssociation,
                inputAssociation);

            // then
            savedAssociation.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
            savedAssociation.IsPublished.Should().BeFalse();
            savedAssociation.PublishDate.Should().BeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishAssociationAsync(
                        It.IsAny<EventEnvelope<Association>>(),
                        AssociationEventOperation.Rejected),
                Times.Once);
        }

        [Fact]
        public async Task ShouldPermitTheTransitionForASystemIdentityAsync()
        {
            // given: the workflow's own writes have no human permitted to make them — §8.6
            // regardless-rule 1 bars the very reviewer whose review fires an automatic approval.
            // The context is ROLELESS, so the flag is the whole of its authority and this cannot
            // pass by accident.
            this.ambientSecurityContext = CreateSystemSecurityContext();

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association inputAssociation = CreateApprovalDecision(storageAssociation.Id);

            // when
            Association savedAssociation = await CaptureSavedAssociationOnApproveAsync(
                storageAssociation,
                inputAssociation);

            // then
            savedAssociation.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            // it stands in for the publisher tier and nothing else: it requests no bypass and is
            // granted none
            savedAssociation.IsApprovedByBypass.Should().BeFalse();
            savedAssociation.ApprovedByBypassReason.Should().BeNull();

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldPermitASystemIdentityToOverrideATerminalRowAsync()
        {
            // given: the previously published sibling a newly approved version demotes is itself
            // Approved, so no Publisher may touch it and no human is available to. The override
            // is open to the workflow for exactly that write.
            this.ambientSecurityContext = CreateSystemSecurityContext();

            Association storageAssociation =
                CreateTerminalStorageAssociation(ApprovalStatus.Approved);

            Association inputAssociation = CreateReopenDecision(storageAssociation.Id);

            // when
            Association savedAssociation = await CaptureSavedAssociationOnApproveAsync(
                storageAssociation,
                inputAssociation);

            // then
            savedAssociation.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            savedAssociation.IsPublished.Should().BeFalse();
            savedAssociation.PublishDate.Should().BeNull();
        }

        [Fact]
        public async Task ShouldHonourAVerifiedSystemIdentityOnAnInboundEnvelopeAsync()
        {
            // given: the approval workflow syncing its decision onto the entity (§16.7.1). It
            // holds NO roles — exactly as the genuine system context does — so the claim is the
            // only thing that can authorize this write, and the row it approves is one no human
            // present is permitted to decide.
            //
            // What makes the claim believable is not this service: it is the signature verified
            // on the way in, which is refused unless the envelope was signed with the workflow's
            // own key. That binding is proven against the REAL broker in
            // EnvelopeIntegrityBrokerTests — it CANNOT be proven here, because this suite mocks
            // VerifyAsync to true.
            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = CreateApprovalDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            Association storageAssociation = CreateApprovableStorageAssociation();
            requestEnvelope.Content.Id = storageAssociation.Id;

            // when
            Association savedAssociation = await CaptureSavedAssociationOnEventTransitionAsync(
                storageAssociation: storageAssociation,
                requestEnvelope: requestEnvelope);

            // then
            savedAssociation.Should().NotBeNull();
            savedAssociation.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            // the workflow asked for no waiver, so none is recorded
            savedAssociation.IsApprovedByBypass.Should().BeFalse();
            savedAssociation.ApprovedByBypassReason.Should().BeNull();
        }

        [Fact]
        public async Task ShouldHonourAVerifiedSystemIdentityToOverrideATerminalRowAsync()
        {
            // given: the override is the write the workflow most needs and the one a forgery
            // would most want — it re-opens and unpublishes a decided row. Admitted here only
            // because the envelope was verified; the sibling demotion that follows a new
            // version's approval is exactly this write, against a row that is itself Approved
            // and therefore untouchable by any Publisher.
            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = CreateReopenDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            Association storageAssociation =
                CreateTerminalStorageAssociation(ApprovalStatus.Approved);

            requestEnvelope.Content.Id = storageAssociation.Id;

            // when
            Association savedAssociation = await CaptureSavedAssociationOnEventTransitionAsync(
                storageAssociation: storageAssociation,
                requestEnvelope: requestEnvelope);

            // then
            savedAssociation.Should().NotBeNull();
            savedAssociation.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            // publication is DERIVED — a re-opened row cannot stay publicly visible while it
            // waits for a second verdict
            savedAssociation.IsPublished.Should().BeFalse();
            savedAssociation.PublishDate.Should().BeNull();
        }

        [Fact]
        public async Task ShouldCarryTheBypassPairFromTheWorkflowCommandRatherThanErasingItAsync()
        {
            // given: a human bypass-approved this item and was authorised for it on the Approval
            // row; the workflow is now syncing that decision onto the entity. The waiver has
            // already happened — the sync is a messenger, not a second decision.
            //
            // Deriving "no bypass used" here, as an ordinary system-identity write does, would
            // write IsApprovedByBypass = false onto the entity while the Approval row records
            // true: the two records diverge (§9.8) and the evidence §9.7.1 rule 3 exists to keep
            // is erased by the very act of storing it.
            Association bypassDecision = CreateBypassApprovalDecision(Guid.NewGuid());

            // captured BEFORE the act, because the service writes onto the row it is handed and
            // reading it back afterwards would compare the result with itself
            string expectedBypassReason = bypassDecision.ApprovedByBypassReason;

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = bypassDecision,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            Association storageAssociation = CreateApprovableStorageAssociation();
            requestEnvelope.Content.Id = storageAssociation.Id;

            // when
            Association savedAssociation = await CaptureSavedAssociationOnEventTransitionAsync(
                storageAssociation: storageAssociation,
                requestEnvelope: requestEnvelope);

            // then
            savedAssociation.Should().NotBeNull();
            savedAssociation.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            savedAssociation.IsApprovedByBypass.Should().BeTrue();
            savedAssociation.ApprovedByBypassReason.Should().Be(expectedBypassReason);
            expectedBypassReason.Should().NotBeNullOrWhiteSpace();
        }
    }
}
