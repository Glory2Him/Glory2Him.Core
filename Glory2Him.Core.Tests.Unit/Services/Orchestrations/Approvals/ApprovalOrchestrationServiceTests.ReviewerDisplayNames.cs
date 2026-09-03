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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        // Answers only for ids it was actually ASKED about, which is what the real read does and
        // what lets a test prove the resolver never REQUESTED somebody rather than merely never
        // rendering them. A stub that hands back its whole list whatever it was given cannot fail
        // when a source branch is deleted, and this is the only window the resolver's tests have
        // onto the identity store.
        private void SetupResolvedIdentityUsers(params IdentityUser[] identityUsers) =>
            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((IEnumerable<string> userIds, CancellationToken token) =>
                            identityUsers
                                .Where(identityUser => userIds.Contains(
                                    identityUser.Id.ToString(),
                                    StringComparer.Ordinal))
                                .ToList());

        /// <summary>
        /// The set is the ROUND's, and now the round's ALONE - the review rows and the outstanding
        /// invitations, resolved in a single read.
        ///
        /// <para><b>A tier member who took no part is absent.</b> Naming them was the tier read's
        /// only remaining effect once the caller stopped supplying ids to intersect against, and
        /// it duplicated ReviewerCandidates - which the same panel already calls, and which
        /// already returns display names. A Tag-Reviewer can now name the people a tag round
        /// involves and not the whole moderator directory.</para>
        ///
        /// <para>The id is echoed back beside the name so a caller joins the answer onto rows it
        /// already holds without depending on ordering, and the name is composed by the same rule
        /// the candidates read uses - which is what stops two surfaces rendering one person under
        /// two names.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNameEverybodyTheRoundInvolvesAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid reviewerId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            Guid tierMemberId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                activeReviewerUserIds: new[] { reviewerId.ToString() },
                activeRequests: new[]
                {
                    new ActiveReviewRequest
                    {
                        Id = Guid.NewGuid(),
                        RequestedUserId = invitedId.ToString(),
                    }
                });

            // Zoe is resolvable - the store would name her the moment anybody asked. She took no
            // part in this round, so the resolver must never ask: she is exactly the person the
            // retired tier read used to add, and exactly the person ReviewerCandidates answers
            // for.
            SetupResolvedIdentityUsers(
                CreateIdentityUser(tierMemberId, preferredName: "Zoe"),
                CreateIdentityUser(reviewerId, preferredName: "Adam"),
                CreateIdentityUser(invitedId, preferredName: "Mary"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: ordered by name the way the picker renders them, and Zoe is not among them -
            // Equal rather than ContainInOrder, because the absence is half the claim
            reviewerDisplayNames.Select(name => (name.UserId, name.DisplayName))
                .Should().Equal(
                    (reviewerId.ToString(), "Adam"),
                    (invitedId.ToString(), "Mary"));

            // and: ONE identity read, always. VerifyNoOtherCalls is the guard that the tier read
            // has not crept back - it fails on any identity call this Verify did not cover.
            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.identityUserServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// <b>The case the resolver exists for.</b> A reviewer who voted and then lost the role,
        /// or whose account was disabled, is absent from the review tier - which is exactly why
        /// the candidates read could never name them. Their id is still stamped on the review row,
        /// so the round admits them, and the resolution read applies no role filter and no
        /// disabled filter.
        /// </summary>
        [Fact]
        public async Task ShouldNameAReviewerWhoHasSinceLeftTheTierAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            Guid departedId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                activeReviewerUserIds: new[] { departedId.ToString() });

            IdentityUser departedUser = CreateIdentityUser(
                departedId,
                preferredName: "Departed");

            departedUser.IsDisabled = true;
            SetupResolvedIdentityUsers(departedUser);

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Single().DisplayName.Should().Be("Departed");

            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.Is<IEnumerable<string>>(userIds =>
                        userIds.Single() == departedId.ToString()),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.identityUserServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The review rows are taken RECORDED rather than active, and the difference is the whole
        /// reason <c>ApprovalReviewerScope</c> carries both. A dismissed or withdrawn verdict is
        /// still rendered by the panel, so its author still needs a name -
        /// <c>ActiveReviewerUserIds</c> deliberately subtracts exactly those people, which is why
        /// it cannot answer this.
        /// </summary>
        [Fact]
        public async Task ShouldNameTheAuthorOfADismissedReviewAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid dismissedReviewerId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                activeReviewerUserIds: Array.Empty<string>(),
                recordedReviewerUserIds: new[] { dismissedReviewerId.ToString() });

            SetupResolvedIdentityUsers(
                CreateIdentityUser(dismissedReviewerId, preferredName: "Dismissed"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Single().UserId.Should().Be(dismissedReviewerId.ToString());
        }

        /// <summary>
        /// <b>The invitation branch, carrying its own weight.</b> Somebody asked to review but not
        /// yet answering appears on no review row at all, so <c>RecordedReviewerUserIds</c> cannot
        /// reach them - <c>ActiveRequests</c> is the only thing that admits them to the answer, and
        /// the panel's Requested heading is empty without it.
        ///
        /// <para>Nothing proved that while the tier read stood. Every test involving an invited
        /// person also listed them as a tier member, so the loop could have been deleted with the
        /// suite green. This one is built so it cannot be: the round holds no reviews whatsoever,
        /// and the invitee is DISABLED - the roles read filters disabled accounts out, so no tier
        /// read could ever have named them either.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNameSomebodyKnownOnlyFromAnOutstandingInvitationAsync()
        {
            // given: a round with no review rows on it at all - the invitation is the whole of it
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                activeReviewerUserIds: Array.Empty<string>(),
                recordedReviewerUserIds: Array.Empty<string>(),
                activeRequests: new[]
                {
                    new ActiveReviewRequest
                    {
                        Id = Guid.NewGuid(),
                        RequestedUserId = invitedId.ToString(),
                    }
                });

            // Disabled on purpose: this is somebody the tier read could never have produced, so
            // the invitation is doing the admitting and nothing else can be credited for it.
            IdentityUser invitedUser = CreateIdentityUser(invitedId, preferredName: "Invited");
            invitedUser.IsDisabled = true;
            SetupResolvedIdentityUsers(invitedUser);

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Select(name => (name.UserId, name.DisplayName))
                .Should().Equal((invitedId.ToString(), "Invited"));

            // and: the id reached the store because the invitation put it there, and for no other
            // reason - delete that union and this Verify sees an empty set
            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.Is<IEnumerable<string>>(userIds =>
                        userIds.Single() == invitedId.ToString()),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.identityUserServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// An id naming no account comes back absent rather than as an error. A caller rendering
        /// somebody whose account has since been deleted falls back for that one row; failing the
        /// call would let one departed account blank a whole panel.
        /// </summary>
        [Fact]
        public async Task ShouldOmitRoundIdsThatNameNoAccountAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid knownId = Guid.NewGuid();
            Guid deletedId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                activeReviewerUserIds: new[] { knownId.ToString(), deletedId.ToString() });

            SetupResolvedIdentityUsers(CreateIdentityUser(knownId, preferredName: "Known"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: the deleted id WAS asked about - it just named nobody
            reviewerDisplayNames.Select(name => name.UserId)
                .Should().BeEquivalentTo(new[] { knownId.ToString() });

            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.Is<IEnumerable<string>>(userIds =>
                        userIds.Contains(deletedId.ToString())),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// One person appearing on the round twice - as a reviewer and as an outstanding
        /// invitation - is one name, not two. The two sources overlap by design: rule 6 retires an
        /// invitation only once its target answers, so between the vote and the retirement both
        /// hold them, and a failed retirement leaves them there indefinitely.
        ///
        /// <para>The collapse now happens on the way IN rather than on the way out. With the tier
        /// read gone there is no dictionary of already-named people to absorb a duplicate, so the
        /// round's id set is the only thing standing between a repeated id and a repeated row in
        /// the query the identity store is handed - which is why this asserts what was ASKED for,
        /// not just what came back.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNameSomebodyOnceHoweverManySurfacesHoldThemAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid everywhereId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                activeReviewerUserIds: new[] { everywhereId.ToString() },
                activeRequests: new[]
                {
                    new ActiveReviewRequest
                    {
                        Id = Guid.NewGuid(),
                        RequestedUserId = everywhereId.ToString(),
                    }
                });

            SetupResolvedIdentityUsers(
                CreateIdentityUser(everywhereId, preferredName: "Everywhere"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Single().DisplayName.Should().Be("Everywhere");

            // and: the store was asked for them ONCE, not twice
            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.Is<IEnumerable<string>>(userIds =>
                        userIds.Count() == 1
                            && userIds.Single() == everywhereId.ToString()),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// <b>The round is the gate as well as the boundary.</b> Nobody outside it is named, so a
        /// holder of any review-tier role can no longer resolve an arbitrary account id - the
        /// composition of the tier gate with an entity gate that 16.7.4 asked for and the unscoped
        /// form could not provide.
        ///
        /// <para>Removing the tier read is what finally made that true rather than merely claimed.
        /// While it stood, every global Publisher, Reviewer and Administrator came back whatever
        /// the round held, so "only the people this round involves" described the intention and
        /// not the response.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNameNobodyOutsideTheRoundAsync()
        {
            // given: a round that involves nobody, and a store that WOULD name a stranger the
            // moment it was asked for one
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid strangerId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);
            SetupResolvedIdentityUsers(CreateIdentityUser(strangerId, preferredName: "Stranger"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Should().BeEmpty();

            // and: the one read asked for NOBODY. The empty answer is the round's doing, not the
            // store's - an id set the resolver never built cannot leak a name, whoever the store
            // would have been willing to resolve.
            // `== false` rather than the house `is false`: this lambda becomes an expression
            // tree, and a pattern match is illegal in one (CS8122).
            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.Is<IEnumerable<string>>(userIds => userIds.Any() == false),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.identityUserServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// 16.7.4's posture, applied rather than re-derived: this is a user-enumeration surface,
        /// so only the requesting tier reaches it. The identity store is never touched for a
        /// caller who is refused - the gate runs on the envelope before the round is read.
        /// </summary>
        [Theory]
        [MemberData(nameof(NonModerationRoleSets))]
        public async Task ShouldThrowUnauthorizedOnResolveIfCallerIsOutsideTheRequestingTierAsync(
            string[] roles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            // when
            ValueTask<IReadOnlyList<ReviewerDisplayName>> resolveTask =
                this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    resolveTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<UnauthorizedApprovalOrchestrationException>();

            this.identityUserServiceMock.VerifyNoOtherCalls();
        }

        // The two halves of the key, each broken on its own so the failure a case reports is the
        // rule that case exists for. Guid.Empty was the only one the resolver ever asserted; the
        // entityType rule sat beside it unexercised, which is to say it could have been deleted
        // with the suite green.
        public static TheoryData<EntityType, Guid, string> InvalidReviewerDisplayNameKeys() =>
            new TheoryData<EntityType, Guid, string>
            {
                { EntityType.ContentItem, Guid.Empty, "EntityId" },
                { (EntityType)97, Guid.NewGuid(), nameof(EntityType) },
            };

        /// <summary>
        /// The shape rules that replaced the batch ceiling. An unusable key names no round, so
        /// there is nobody to resolve and the identity store is never asked.
        ///
        /// <para>BOTH halves, because both are rules. An integer outside the enum is refused
        /// rather than probed for: no stored approval can carry it, and letting it through would
        /// produce a not-found sentence naming a type that does not exist. Each case asserts the
        /// parameter it expects to be blamed for, so neither can pass on the other's failure - the
        /// mistake a bare BeOfType invites.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(InvalidReviewerDisplayNameKeys))]
        public async Task ShouldThrowValidationOnResolveIfTheEntityKeyIsInvalidAsync(
            EntityType entityType,
            Guid entityId,
            string expectedParameter)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            // when
            ValueTask<IReadOnlyList<ReviewerDisplayName>> resolveTask =
                this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    resolveTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

            // and: the rule that refused is the one this case broke, and it is the ONLY one - the
            // other half of the key was perfectly good
            actualException.InnerException.Data.Keys.Cast<string>()
                .Should().Equal(expectedParameter);

            this.identityUserServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// No approval on the key is a not-found, the same answer the candidates read gives. There
        /// is no round, so there is nobody it names - and an empty list instead would let a
        /// mistyped key look like a round nobody has touched.
        /// </summary>
        [Fact]
        public async Task ShouldThrowNotFoundOnResolveIfNoApprovalOccupiesTheKeyAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalEntityMatch)null);

            // when
            ValueTask<IReadOnlyList<ReviewerDisplayName>> resolveTask =
                this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    resolveTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<NotFoundApprovalOrchestrationException>();

            this.identityUserServiceMock.VerifyNoOtherCalls();
        }
    }
}
