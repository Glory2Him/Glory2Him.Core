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
        private void SetupTierMembers(params IdentityUser[] identityUsers) =>
            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(identityUsers.ToList());

        private void SetupResolvedIdentityUsers(params IdentityUser[] identityUsers) =>
            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(identityUsers.ToList());

        /// <summary>
        /// The set is the ROUND's, drawn from all three places the panel draws from - the review
        /// rows, the outstanding invitations and the review tier. The id is echoed back beside the
        /// name so a caller joins the answer onto the rows it already holds without depending on
        /// ordering, and the name is composed by the same rule the candidates read uses, which is
        /// what stops two surfaces rendering one person under two names.
        /// </summary>
        [Fact]
        public async Task ShouldNameEverybodyTheRoundInvolvesAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid reviewerId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            Guid candidateId = Guid.NewGuid();

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

            SetupTierMembers(
                CreateIdentityUser(candidateId, preferredName: "Zoe"),
                CreateIdentityUser(reviewerId, preferredName: "Adam"),
                CreateIdentityUser(invitedId, preferredName: "Mary"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: ordered by name, the way the picker renders them
            reviewerDisplayNames.Select(name => (name.UserId, name.DisplayName))
                .Should().ContainInOrder(
                    (reviewerId.ToString(), "Adam"),
                    (invitedId.ToString(), "Mary"),
                    (candidateId.ToString(), "Zoe"));
        }

        /// <summary>
        /// <b>The case the resolver exists for.</b> A reviewer who voted and then lost the role,
        /// or whose account was disabled, is absent from the tier read - which is exactly why the
        /// candidates read could never name them. Their id is still stamped on the review row, so
        /// the round admits them, and a second lookup applying no role filter and no disabled
        /// filter names them.
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

            SetupTierMembers();

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

            SetupTierMembers();

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
        /// One identity read in the ordinary case. The tier read returns whole accounts, so the
        /// same rows that admit a candidate also carry the name; nobody the round involves is
        /// looked up twice, and the second read is spent only on people the tier no longer holds.
        /// </summary>
        [Fact]
        public async Task ShouldNotLookUpAgainAnybodyTheTierReadAlreadyNamedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid reviewerId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();

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

            SetupTierMembers(
                CreateIdentityUser(reviewerId, preferredName: "Adam"),
                CreateIdentityUser(invitedId, preferredName: "Mary"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Should().HaveCount(2);

            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
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

            SetupTierMembers();
            SetupResolvedIdentityUsers(CreateIdentityUser(knownId, preferredName: "Known"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Select(name => name.UserId)
                .Should().BeEquivalentTo(new[] { knownId.ToString() });
        }

        /// <summary>
        /// One person appearing on the round more than once - as a reviewer and as an outstanding
        /// invitation, and as a tier member besides - is one name, not three. The surfaces overlap
        /// by design, so the resolver has to collapse them.
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

            SetupTierMembers(CreateIdentityUser(everywhereId, preferredName: "Everywhere"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Single().DisplayName.Should().Be("Everywhere");
        }

        /// <summary>
        /// <b>The round is the gate as well as the boundary.</b> Nobody outside it is named, so a
        /// holder of any review-tier role can no longer resolve an arbitrary account id - which is
        /// the composition of the tier gate with an entity gate that 16.7.4 asked for and the
        /// unscoped form could not provide.
        /// </summary>
        [Fact]
        public async Task ShouldNameNobodyOutsideTheRoundAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid strangerId = Guid.NewGuid();

            SetupReviewerScope(approvalId: approvalId);
            SetupTierMembers();
            SetupResolvedIdentityUsers(CreateIdentityUser(strangerId, preferredName: "Stranger"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: an empty round names nobody, and the resolution read is never even reached
            reviewerDisplayNames.Should().BeEmpty();

            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
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

        /// <summary>
        /// The shape rule that replaced the batch ceiling. An unusable key names no round, so
        /// there is nobody to resolve and the identity store is never asked.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationOnResolveIfTheEntityKeyIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);

            // when
            ValueTask<IReadOnlyList<ReviewerDisplayName>> resolveTask =
                this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    EntityType.ContentItem,
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    resolveTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

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
