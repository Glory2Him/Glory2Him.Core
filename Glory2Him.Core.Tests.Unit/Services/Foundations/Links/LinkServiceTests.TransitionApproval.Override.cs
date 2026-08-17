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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    /// <summary>
    /// The three things the widened transition verb added: the <c>Admin</c> override out of a
    /// terminal state, the system identity as a second admissible actor, and the bypass pair
    /// carried as a request and written from the verdict.
    /// </summary>
    public partial class LinkServiceTests
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

            Link storageLink = CreateTerminalStorageLink(terminalStatus);
            Link inputLink = CreateReopenDecision(storageLink.Id);

            SetupLinkStorageRead(storageLink);

            var unauthorizedLinkException =
                new UnauthorizedLinkException(
                    message: "The current user is not allowed to transition this link.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkException);

            // when
            ValueTask<Link> transitionTask =
                this.linkService.TransitionLinkApprovalAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(transitionTask.AsTask);

            // then: refused row-local, before the cross-entity decision is asked
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnTransitionIfANonPublisherOverridesATerminalRowAsync(
            string[] roles)
        {
            // given: the owner and the Reviewer are refused the override too, and by the SAME
            // gate — it runs before the publisher-tier check, so the message names the override
            // rather than the approve.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            Link storageLink = CreateTerminalStorageLink(ApprovalStatus.Approved);
            Link inputLink = CreateReopenDecision(storageLink.Id);

            SetupLinkStorageRead(storageLink);

            var unauthorizedLinkException =
                new UnauthorizedLinkException(
                    message: "The current user is not allowed to transition this link.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkException);

            // when
            ValueTask<Link> transitionTask =
                this.linkService.TransitionLinkApprovalAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(transitionTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
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
            // published, so this is also where the unpublish-on-the-way-out rule is proved:
            // a re-opened row must not stay publicly visible while it waits for a second verdict.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin);

            Link storageLink = CreateTerminalStorageLink(terminalStatus);
            Link inputLink = CreateReopenDecision(storageLink.Id);

            // when
            Link savedLink = await CaptureSavedLinkOnTransitionAsync(
                storageLink: storageLink,
                inputLink: inputLink);

            // then
            savedLink.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            savedLink.IsPublished.Should().BeFalse();
            savedLink.PublishDate.Should().BeNull();

            // the fact follows the decision: re-opening a round is what the Submitted address
            // already means, and the approval workflow keys on it
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Submitted),
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

            Link storageLink = CreateTerminalStorageLink(ApprovalStatus.Approved);
            Link inputLink = CreateRejectionDecision(storageLink.Id);

            SetupAccessBrokerToPermit();

            // when
            Link savedLink = await CaptureSavedLinkOnTransitionAsync(
                storageLink: storageLink,
                inputLink: inputLink);

            // then
            savedLink.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
            savedLink.IsPublished.Should().BeFalse();
            savedLink.PublishDate.Should().BeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Rejected),
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

            Link storageLink = CreateApprovableStorageLink();
            Link inputLink = CreateApprovalDecision(storageLink.Id);

            // when
            Link savedLink = await CaptureSavedLinkOnTransitionAsync(
                storageLink: storageLink,
                inputLink: inputLink);

            // then
            savedLink.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            // it stands in for the publisher tier and nothing else: it requests no bypass and is
            // granted none
            savedLink.IsApprovedByBypass.Should().BeFalse();
            savedLink.ApprovedByBypassReason.Should().BeNull();

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

            Link storageLink = CreateTerminalStorageLink(ApprovalStatus.Approved);
            Link inputLink = CreateReopenDecision(storageLink.Id);

            // when
            Link savedLink = await CaptureSavedLinkOnTransitionAsync(
                storageLink: storageLink,
                inputLink: inputLink);

            // then
            savedLink.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            savedLink.IsPublished.Should().BeFalse();
            savedLink.PublishDate.Should().BeNull();
        }

        [Fact]
        public async Task ShouldRefuseASystemIdentityClaimedOnAnInboundEnvelopeAsync()
        {
            // given: THE highest-risk case in this change. On the event path the security context
            // is deserialized and unverified (§14.6 rule 4), so a caller who can reach the public
            // Link-Approving address would otherwise declare themselves the workflow and walk past
            // every approval rule in the design by setting one JSON property.
            //
            // Roleless, exactly as the genuine system context is — so the ONLY thing that could
            // authorize this is the claim, and the only thing that can refuse it is where the
            // claim arrived from.
            var requestEnvelope = new EventEnvelope<Link>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = CreateApprovalDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            Link storageLink = CreateApprovableStorageLink();
            requestEnvelope.Content.Id = storageLink.Id;

            SetupLinkStorageRead(storageLink);

            var unauthorizedLinkException =
                new UnauthorizedLinkException(
                    message: "The current user is not allowed to approve this link.");

            // when
            ValueTask<EventEnvelope<Link>?> transitionTask =
                this.linkService.OnApprovingLinkAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(transitionTask.AsTask);

            // then: treated as the ordinary unprivileged caller it is, and refused at the
            // publisher tier it does not hold
            actualException.InnerException.Should()
                .BeEquivalentTo(unauthorizedLinkException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        It.IsAny<LinkEventOperation>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldRefuseAnInboundEnvelopeClaimingSystemIdentityToOverrideATerminalRowAsync()
        {
            // given: the same forged claim aimed at the override — the write the flag would be
            // most valuable for forging, because it re-opens and unpublishes a decided row.
            var requestEnvelope = new EventEnvelope<Link>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = CreateReopenDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            Link storageLink = CreateTerminalStorageLink(ApprovalStatus.Approved);
            requestEnvelope.Content.Id = storageLink.Id;

            SetupLinkStorageRead(storageLink);

            var unauthorizedLinkException =
                new UnauthorizedLinkException(
                    message: "The current user is not allowed to transition this link.");

            // when
            ValueTask<EventEnvelope<Link>?> transitionTask =
                this.linkService.OnApprovingLinkAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(transitionTask.AsTask);

            // then: refused at the override gate, which is where a non-Admin belongs
            actualException.InnerException.Should()
                .BeEquivalentTo(unauthorizedLinkException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldWriteTheBypassFlagFromTheVerdictRatherThanTheRequestAsync()
        {
            // given: the caller ASKS for a bypass and the decision finds nothing to waive. A
            // bypass that turned out to be unnecessary must record no bypass at all — otherwise
            // "what was published without meeting its conditions" answers with rows that met
            // them.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Link storageLink = CreateApprovableStorageLink();

            Link inputLink = CreateBypassApprovalRequest(
                linkId: storageLink.Id,
                bypassReason: GetRandomString());

            SetupAccessBrokerToPermit();

            // when
            Link savedLink = await CaptureSavedLinkOnTransitionAsync(
                storageLink: storageLink,
                inputLink: inputLink);

            // then
            savedLink.IsApprovedByBypass.Should().BeFalse();
            savedLink.ApprovedByBypassReason.Should().BeNull();
        }

        [Fact]
        public async Task ShouldRetainTheBypassReasonOnlyWhenTheVerdictUsedTheBypassAsync()
        {
            // given: the reason's VALUE is necessarily the caller's own words — no decision can
            // say why a human chose to override — but its RETENTION is the decision's call.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Link storageLink = CreateApprovableStorageLink();
            string inputBypassReason = GetRandomString();

            Link inputLink = CreateBypassApprovalRequest(
                linkId: storageLink.Id,
                bypassReason: inputBypassReason);

            SetupAccessBrokerToPermitByBypass();

            // when
            Link savedLink = await CaptureSavedLinkOnTransitionAsync(
                storageLink: storageLink,
                inputLink: inputLink);

            // then
            savedLink.IsApprovedByBypass.Should().BeTrue();
            savedLink.ApprovedByBypassReason.Should().Be(inputBypassReason);
        }

        [Fact]
        public async Task ShouldCarryTheBypassRequestToTheAccessDecisionAsync()
        {
            // given: the request has to reach the decision, or DoNotAllowBypassingSettings has
            // nothing to refuse and the waiver is never actually evaluated.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Link storageLink = CreateApprovableStorageLink();
            string inputBypassReason = GetRandomString();

            Link inputLink = CreateBypassApprovalRequest(
                linkId: storageLink.Id,
                bypassReason: inputBypassReason);

            SetupAccessBrokerToPermitByBypass();

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureApprovalDecisionQueryAsync(
                    storageLink: storageLink,
                    inputLink: inputLink);

            // then
            actualQuery.IsBypassRequested.Should().BeTrue();
            actualQuery.BypassReason.Should().Be(inputBypassReason);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnTransitionIfABypassHasNoReasonAsync()
        {
            // given: a bypass is only tolerable because it leaves a record, and an unexplained
            // one records nothing worth reading. Refused BEFORE any policy is read, so it is
            // refused under every policy — including one that would have permitted the waiver.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin);

            Link inputLink = CreateBypassApprovalRequest(
                linkId: Guid.NewGuid(),
                bypassReason: null);

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.UpsertDataList(
                key: nameof(Link.ApprovedByBypassReason),
                value: "Bypass reason is required when bypassing.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            // when
            ValueTask<Link> transitionTask =
                this.linkService.TransitionLinkApprovalAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(transitionTask.AsTask);

            // then: the row was never even read, let alone a policy resolved against it
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectLinkByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Submitted)]
        public async Task ShouldThrowValidationExceptionOnTransitionIfABypassIsNotAnApprovalAsync(
            ApprovalStatus notAnApproval)
        {
            // given: a waiver waives the APPROVAL conditions. Rejecting withholds approval rather
            // than granting it and re-opening decides nothing, so neither has anything to waive
            // (§9.7.5). Admitting one would stamp IsApprovedByBypass on a rejection.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin);

            Link inputLink = CreateBypassApprovalRequest(
                linkId: Guid.NewGuid(),
                bypassReason: GetRandomString());

            inputLink.ApprovalStatus = notAnApproval;
            inputLink.IsPublished = false;
            inputLink.PublishDate = null;

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.UpsertDataList(
                key: nameof(Link.IsApprovedByBypass),
                value: "Bypass requires an approved link.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            // when
            ValueTask<Link> transitionTask =
                this.linkService.TransitionLinkApprovalAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(transitionTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnTransitionIfTheDecisionRefusesABypassForAnAdminAsync()
        {
            // given: DoNotAllowBypassingSettings closes the route to EVERYONE, Admin included.
            // The setting lives on another entity, so the refusal comes back on the verdict —
            // which is the point of asking rather than deciding locally.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin);

            Link storageLink = CreateApprovableStorageLink();

            Link inputLink = CreateBypassApprovalRequest(
                linkId: storageLink.Id,
                bypassReason: GetRandomString());

            SetupLinkStorageRead(storageLink);
            SetupAccessBrokerToRefuse(AccessDenialReason.BypassNotPermitted);

            var unauthorizedLinkException =
                new UnauthorizedLinkException(
                    message: "The current user is not allowed to approve this link.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkException);

            // when
            ValueTask<Link> transitionTask =
                this.linkService.TransitionLinkApprovalAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(transitionTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
