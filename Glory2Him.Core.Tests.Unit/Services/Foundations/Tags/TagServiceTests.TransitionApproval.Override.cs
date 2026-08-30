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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    /// <summary>
    /// The three things the widened transition verb added: the <c>Administrators</c> override out of a
    /// terminal state, the system identity as a second admissible actor, and the bypass pair
    /// carried as a request and written from the verdict.
    /// </summary>
    public partial class TagServiceTests
    {
        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldThrowUnauthorizedOnTransitionIfAPublisherOverridesATerminalRowAsync(
            ApprovalStatus terminalStatus)
        {
            // given: the publisher tier decides a SUBMITTED row. Moving one back out of a
            // terminal state is an override, and a state a publisher could edit out of would not
            // be terminal at all (§3.4 rules 7 and 16, §8.6 HR-4).
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Tag storageTag = CreateTerminalStorageTag(terminalStatus);
            Tag inputTag = CreateReopenDecision(storageTag.Id);

            SetupTagStorageRead(storageTag);

            var unauthorizedTagException =
                new UnauthorizedTagException(
                    message: "The current user is not allowed to transition this tag.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> transitionTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(transitionTask.AsTask);

            // then: refused row-local, before the cross-entity decision is asked
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnTransitionIfANonPublisherOverridesATerminalRowAsync(
            string[] roles)
        {
            // given: the owner and the Reviewers are refused the override too, and by the SAME
            // gate — it runs before the publisher-tier check, so the message names the override
            // rather than the approve.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            Tag storageTag = CreateTerminalStorageTag(ApprovalStatus.Approved);
            Tag inputTag = CreateReopenDecision(storageTag.Id);

            SetupTagStorageRead(storageTag);

            var unauthorizedTagException =
                new UnauthorizedTagException(
                    message: "The current user is not allowed to transition this tag.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> transitionTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(transitionTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
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
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Tag storageTag = CreateTerminalStorageTag(terminalStatus);
            Tag inputTag = CreateReopenDecision(storageTag.Id);

            // when
            Tag savedTag = await CaptureSavedTagOnTransitionAsync(
                storageTag: storageTag,
                inputTag: inputTag);

            // then
            savedTag.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            savedTag.IsPublished.Should().BeFalse();
            savedTag.PublishDate.Should().BeNull();

            // the fact follows the decision: re-opening a round is what the Submitted address
            // already means, and the approval workflow keys on it
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Submitted),
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
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Tag storageTag = CreateTerminalStorageTag(ApprovalStatus.Approved);
            Tag inputTag = CreateRejectionDecision(storageTag.Id);

            SetupAccessBrokerToPermit();

            // when
            Tag savedTag = await CaptureSavedTagOnTransitionAsync(
                storageTag: storageTag,
                inputTag: inputTag);

            // then
            savedTag.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
            savedTag.IsPublished.Should().BeFalse();
            savedTag.PublishDate.Should().BeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Rejected),
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

            Tag storageTag = CreateApprovableStorageTag();
            Tag inputTag = CreateApprovalDecision(storageTag.Id);

            // when
            Tag savedTag = await CaptureSavedTagOnTransitionAsync(
                storageTag: storageTag,
                inputTag: inputTag);

            // then
            savedTag.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            // it stands in for the publisher tier and nothing else: it requests no bypass and is
            // granted none
            savedTag.IsApprovedByBypass.Should().BeFalse();
            savedTag.ApprovedByBypassReason.Should().BeNull();

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
            // Approved, so no Publishers may touch it and no human is available to. The override
            // is open to the workflow for exactly that write.
            this.ambientSecurityContext = CreateSystemSecurityContext();

            Tag storageTag = CreateTerminalStorageTag(ApprovalStatus.Approved);
            Tag inputTag = CreateReopenDecision(storageTag.Id);

            // when
            Tag savedTag = await CaptureSavedTagOnTransitionAsync(
                storageTag: storageTag,
                inputTag: inputTag);

            // then
            savedTag.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            savedTag.IsPublished.Should().BeFalse();
            savedTag.PublishDate.Should().BeNull();
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
            // on the way in. Only this system holds the signing key, so a verified
            // envelope is one this system minted — and the security context is inside the
            // signed payload, so the flag cannot be added to a genuine envelope without
            // breaking the HMAC. That binding is proven against the REAL broker in
            // EnvelopeIntegrityBrokerTests — it CANNOT be proven here, because this suite
            // mocks VerifyAsync to true.
            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = CreateApprovalDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            Tag storageTag = CreateApprovableStorageTag();
            requestEnvelope.Content.Id = storageTag.Id;

            // when
            Tag savedTag = await CaptureSavedTagOnEventTransitionAsync(
                storageTag: storageTag,
                requestEnvelope: requestEnvelope);

            // then
            savedTag.Should().NotBeNull();
            savedTag.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            // the workflow asked for no waiver, so none is recorded
            savedTag.IsApprovedByBypass.Should().BeFalse();
            savedTag.ApprovedByBypassReason.Should().BeNull();
        }

        [Fact]
        public async Task ShouldHonourAVerifiedSystemIdentityToOverrideATerminalRowAsync()
        {
            // given: the override is the write the workflow most needs and the one a forgery
            // would most want — it re-opens and unpublishes a decided row. Admitted here only
            // because the envelope was verified; the sibling demotion that follows a new
            // version's approval is exactly this write, against a row that is itself Approved
            // and therefore untouchable by any Publishers.
            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = CreateReopenDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            Tag storageTag = CreateTerminalStorageTag(ApprovalStatus.Approved);
            requestEnvelope.Content.Id = storageTag.Id;

            // when
            Tag savedTag = await CaptureSavedTagOnEventTransitionAsync(
                storageTag: storageTag,
                requestEnvelope: requestEnvelope);

            // then
            savedTag.Should().NotBeNull();
            savedTag.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            // publication is DERIVED — a re-opened row cannot stay publicly visible while it
            // waits for a second verdict
            savedTag.IsPublished.Should().BeFalse();
            savedTag.PublishDate.Should().BeNull();
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
            string bypassReason = GetRandomString();

            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = CreateBypassApprovalRequest(
                    tagId: Guid.NewGuid(),
                    bypassReason: bypassReason),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            Tag storageTag = CreateApprovableStorageTag();
            requestEnvelope.Content.Id = storageTag.Id;

            // when
            Tag savedTag = await CaptureSavedTagOnEventTransitionAsync(
                storageTag: storageTag,
                requestEnvelope: requestEnvelope);

            // then
            savedTag.Should().NotBeNull();
            savedTag.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            savedTag.IsApprovedByBypass.Should().BeTrue();
            savedTag.ApprovedByBypassReason.Should().Be(bypassReason);
        }

        [Fact]
        public async Task ShouldWriteTheBypassFlagFromTheVerdictRatherThanTheRequestAsync()
        {
            // given: the caller ASKS for a bypass and the decision finds nothing to waive. A
            // bypass that turned out to be unnecessary must record no bypass at all — otherwise
            // "what was published without meeting its conditions" answers with rows that met
            // them.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Tag storageTag = CreateApprovableStorageTag();

            Tag inputTag = CreateBypassApprovalRequest(
                tagId: storageTag.Id,
                bypassReason: GetRandomString());

            SetupAccessBrokerToPermit();

            // when
            Tag savedTag = await CaptureSavedTagOnTransitionAsync(
                storageTag: storageTag,
                inputTag: inputTag);

            // then
            savedTag.IsApprovedByBypass.Should().BeFalse();
            savedTag.ApprovedByBypassReason.Should().BeNull();
        }

        [Fact]
        public async Task ShouldRetainTheBypassReasonOnlyWhenTheVerdictUsedTheBypassAsync()
        {
            // given: the reason's VALUE is necessarily the caller's own words — no decision can
            // say why a human chose to override — but its RETENTION is the decision's call.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Tag storageTag = CreateApprovableStorageTag();
            string inputBypassReason = GetRandomString();

            Tag inputTag = CreateBypassApprovalRequest(
                tagId: storageTag.Id,
                bypassReason: inputBypassReason);

            SetupAccessBrokerToPermitByBypass();

            // when
            Tag savedTag = await CaptureSavedTagOnTransitionAsync(
                storageTag: storageTag,
                inputTag: inputTag);

            // then
            savedTag.IsApprovedByBypass.Should().BeTrue();
            savedTag.ApprovedByBypassReason.Should().Be(inputBypassReason);
        }

        [Fact]
        public async Task ShouldCarryTheBypassRequestToTheAccessDecisionAsync()
        {
            // given: the request has to reach the decision, or DoNotAllowBypassingSettings has
            // nothing to refuse and the waiver is never actually evaluated.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Tag storageTag = CreateApprovableStorageTag();
            string inputBypassReason = GetRandomString();

            Tag inputTag = CreateBypassApprovalRequest(
                tagId: storageTag.Id,
                bypassReason: inputBypassReason);

            SetupAccessBrokerToPermitByBypass();

            // when
            ApprovalDecisionQuery actualQuery =
                await CaptureApprovalDecisionQueryAsync(
                    storageTag: storageTag,
                    inputTag: inputTag);

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
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Tag inputTag = CreateBypassApprovalRequest(
                tagId: Guid.NewGuid(),
                bypassReason: null);

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.UpsertDataList(
                key: nameof(Tag.ApprovedByBypassReason),
                value: "Bypass reason is required when bypassing.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            // when
            ValueTask<Tag> transitionTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(transitionTask.AsTask);

            // then: the row was never even read, let alone a policy resolved against it
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
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
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Tag inputTag = CreateBypassApprovalRequest(
                tagId: Guid.NewGuid(),
                bypassReason: GetRandomString());

            inputTag.ApprovalStatus = notAnApproval;
            inputTag.IsPublished = false;
            inputTag.PublishDate = null;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.UpsertDataList(
                key: nameof(Tag.IsApprovedByBypass),
                value: "Bypass requires an approved tag.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            // when
            ValueTask<Tag> transitionTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(transitionTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnTransitionIfTheDecisionRefusesABypassForAnAdminAsync()
        {
            // given: DoNotAllowBypassingSettings closes the route to EVERYONE, Administrators included.
            // The setting lives on another entity, so the refusal comes back on the verdict —
            // which is the point of asking rather than deciding locally.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Tag storageTag = CreateApprovableStorageTag();

            Tag inputTag = CreateBypassApprovalRequest(
                tagId: storageTag.Id,
                bypassReason: GetRandomString());

            SetupTagStorageRead(storageTag);
            SetupAccessBrokerToRefuse(AccessDenialReason.BypassNotPermitted);

            var unauthorizedTagException =
                new UnauthorizedTagException(
                    message: "The current user is not allowed to approve this tag.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> transitionTask =
                this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(transitionTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
