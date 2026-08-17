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
        public async Task ShouldRefuseASystemIdentityClaimedOnAnInboundEnvelopeAsync()
        {
            // given: THE highest-risk case in this change. On the event path the security context
            // is deserialized and unverified (§14.6 rule 4), so a caller who can reach the public
            // Association-Approving address would otherwise declare themselves the workflow and
            // walk past every approval rule in the design by setting one JSON property.
            //
            // Roleless, exactly as the genuine system context is — so the ONLY thing that could
            // authorize this is the claim, and the only thing that can refuse it is where the
            // claim arrived from.
            Association storageAssociation = CreateApprovableStorageAssociation();

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = CreateApprovalDecision(storageAssociation.Id),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupStorageRead(storageAssociation);

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is not allowed to approve " +
                        "this content item association.");

            // when
            ValueTask<EventEnvelope<Association>?> transitionTask =
                this.associationService.OnApprovingAssociationAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    transitionTask.AsTask);

            // then: treated as the ordinary unprivileged caller it is, and refused at the
            // publisher tier it does not hold
            actualException.InnerException.Should()
                .BeEquivalentTo(unauthorizedAssociationException);

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

        [Fact]
        public async Task ShouldRefuseAnInboundEnvelopeClaimingSystemIdentityToOverrideATerminalRowAsync()
        {
            // given: the same forged claim aimed at the override — the write the flag would be
            // most valuable for forging, because it re-opens and unpublishes a decided row.
            Association storageAssociation =
                CreateTerminalStorageAssociation(ApprovalStatus.Approved);

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = CreateReopenDecision(storageAssociation.Id),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupStorageRead(storageAssociation);

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is not allowed to transition " +
                        "this content item association.");

            // when
            ValueTask<EventEnvelope<Association>?> transitionTask =
                this.associationService.OnApprovingAssociationAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    transitionTask.AsTask);

            // then: refused at the override gate, which is where a non-Admin belongs
            actualException.InnerException.Should()
                .BeEquivalentTo(unauthorizedAssociationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
