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
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        private void SetupResolvedIdentityUsers(params IdentityUser[] identityUsers) =>
            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(identityUsers.ToList());

        /// <summary>
        /// The id is echoed back beside the name, because a caller joins the answer onto rows it
        /// already holds and must not have to depend on ordering to do it. The name itself is
        /// composed by the same rule the candidates read uses - one composer is what stops two
        /// surfaces rendering one person under two names.
        /// </summary>
        [Fact]
        public async Task ShouldResolveDisplayNamesForTheIdsGivenAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid firstUserId = Guid.NewGuid();
            Guid secondUserId = Guid.NewGuid();

            SetupResolvedIdentityUsers(
                CreateIdentityUser(secondUserId, preferredName: "Zoe"),
                CreateIdentityUser(firstUserId, preferredName: "Adam"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    new[] { firstUserId.ToString(), secondUserId.ToString() },
                    TestContext.Current.CancellationToken);

            // then: ordered by name, the way the picker renders them
            reviewerDisplayNames.Select(name => (name.UserId, name.DisplayName))
                .Should().ContainInOrder(
                    (firstUserId.ToString(), "Adam"),
                    (secondUserId.ToString(), "Zoe"));
        }

        /// <summary>
        /// <b>No role filter, and that is the whole point of the read.</b> The candidates read
        /// answers who is in scope for a round, so a reviewer who voted and then lost the role
        /// vanishes from it entirely - which is the case that left the panel with a blank name.
        /// This one resolves ids, so the tier read is never consulted.
        /// </summary>
        [Fact]
        public async Task ShouldResolveNamesWithoutConsultingTheReviewTierAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid departedUserId = Guid.NewGuid();

            IdentityUser departedUser = CreateIdentityUser(
                departedUserId,
                preferredName: "Departed");

            departedUser.IsDisabled = true;
            SetupResolvedIdentityUsers(departedUser);

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    new[] { departedUserId.ToString() },
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Single().DisplayName.Should().Be("Departed");

            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // and: no approval is read either - the resolver is not keyed on a round, which is
            // what lets one call answer reviewers, invited people and candidates together.
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// An id naming no account comes back absent rather than as an error. A caller asking
        /// about somebody whose account has been deleted renders its own fallback for that one
        /// row; failing the call would let one departed account blank a whole panel.
        /// </summary>
        [Fact]
        public async Task ShouldOmitIdsThatNameNoAccountAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid knownUserId = Guid.NewGuid();
            Guid unknownUserId = Guid.NewGuid();
            SetupResolvedIdentityUsers(CreateIdentityUser(knownUserId, preferredName: "Known"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    new[] { knownUserId.ToString(), unknownUserId.ToString() },
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Select(name => name.UserId)
                .Should().BeEquivalentTo(new[] { knownUserId.ToString() });
        }

        /// <summary>
        /// Blank and repeated ids are cleaned up before the read, so a surface may hand over
        /// whatever it is holding - the same person appearing as a reviewer and as an invitation
        /// is one lookup, not two.
        /// </summary>
        [Fact]
        public async Task ShouldTrimAndDeduplicateTheIdsBeforeResolvingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid userId = Guid.NewGuid();
            IEnumerable<string> capturedUserIds = null;

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<IEnumerable<string>, CancellationToken>(
                            (userIds, token) => capturedUserIds = userIds)
                        .ReturnsAsync(new List<IdentityUser>());

            // when
            await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                new[] { $" {userId} ", userId.ToString(), null, "   " },
                TestContext.Current.CancellationToken);

            // then
            capturedUserIds.Should().BeEquivalentTo(new[] { userId.ToString() });
        }

        /// <summary>
        /// 16.7.4's posture, applied rather than re-derived: this is a user-enumeration surface,
        /// so only the requesting tier reaches it. The identity store is never touched for a
        /// caller who is refused.
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
                    new[] { Guid.NewGuid().ToString() },
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    resolveTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<UnauthorizedApprovalOrchestrationException>();

            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The batch is capped, and an oversized one is REFUSED rather than truncated. Truncating
        /// would hand the caller a shorter answer than it asked for and leave it rendering blanks
        /// it could not explain.
        ///
        /// <para>The cap bounds one response, not a caller — what decides how much of the
        /// directory is reachable is the tier gate, not this constant.</para>
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationOnResolveIfTheBatchExceedsTheCapAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            string[] tooManyUserIds = Enumerable.Range(0, 201)
                .Select(_ => Guid.NewGuid().ToString())
                .ToArray();

            // when
            ValueTask<IReadOnlyList<ReviewerDisplayName>> resolveTask =
                this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    tooManyUserIds,
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    resolveTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

            this.identityUserServiceMock.Verify(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The cap counts ACCOUNTS, not spellings. A GUID has several equal string forms - case,
        /// braces, hyphens - so counting the caller's raw text would refuse somebody who asked
        /// about 200 people using 201 spellings of them, which is a request the resolver can
        /// answer perfectly well.
        ///
        /// <para>201 strings go in, naming 200 distinct accounts. It must succeed, and the
        /// identity store must be asked for 200 canonical ids.</para>
        /// </summary>
        [Fact]
        public async Task ShouldCountAccountsRatherThanSpellingsAgainstTheCapAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            List<Guid> distinctUserIds = Enumerable.Range(0, 200)
                .Select(_ => Guid.NewGuid())
                .ToList();

            // 201 strings for 200 accounts: the first id spelled twice, once braced and uppercased.
            var spellings = new List<string>(
                distinctUserIds.Select(userId => userId.ToString()))
                {
                    distinctUserIds[0].ToString("B").ToUpperInvariant(),
                };

            IEnumerable<string> capturedUserIds = null;

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<IEnumerable<string>, CancellationToken>(
                            (userIds, token) => capturedUserIds = userIds)
                        .ReturnsAsync(new List<IdentityUser>());

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    spellings,
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Should().BeEmpty();

            capturedUserIds.Should().BeEquivalentTo(
                distinctUserIds.Select(userId => userId.ToString()));
        }

        /// <summary>
        /// Unparseable ids fall out before the count as well. They can never name an account, so
        /// charging them against a ceiling that exists to bound a query would refuse work nobody
        /// asked for.
        /// </summary>
        [Fact]
        public async Task ShouldNotCountUnusableIdsAgainstTheCapAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid realUserId = Guid.NewGuid();

            var mostlyRubbish = new List<string> { realUserId.ToString() };

            mostlyRubbish.AddRange(
                Enumerable.Range(0, 300).Select(index => $"not-a-guid-{index}"));

            SetupResolvedIdentityUsers(CreateIdentityUser(realUserId, preferredName: "Real"));

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    mostlyRubbish,
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Single().UserId.Should().Be(realUserId.ToString());
        }

        /// <summary>
        /// A non-canonical spelling still reaches the identity store, canonicalised.
        ///
        /// <para><b>The captured argument is the assertion here, not the echo.</b> The echoed
        /// UserId is read off the resolved row, so it is canonical by construction whatever the
        /// caller sent and whatever this method does to the input — asserting on it alone would
        /// pass even if every braced spelling were silently discarded on the way in, which is the
        /// whole failure this guards. What has to be observed is the id handed DOWN.</para>
        /// </summary>
        [Fact]
        public async Task ShouldResolveANonCanonicalSpellingAsTheCanonicalIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Guid userId = Guid.NewGuid();
            IEnumerable<string> capturedUserIds = null;

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersByIdsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<IEnumerable<string>, CancellationToken>(
                            (userIds, token) => capturedUserIds = userIds)
                        .ReturnsAsync(new List<IdentityUser>
                        {
                            CreateIdentityUser(userId, preferredName: "Someone"),
                        });

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    new[] { userId.ToString("B").ToUpperInvariant() },
                    TestContext.Current.CancellationToken);

            // then: the braced, upper-cased spelling was recognised and canonicalised, not dropped
            capturedUserIds.Should().BeEquivalentTo(new[] { userId.ToString() });

            // and: the answer names the account in canonical form
            reviewerDisplayNames.Single().UserId.Should().Be(userId.ToString());
        }

        /// <summary>
        /// Asking about nobody is answered with nobody. A panel holding no ids should not have to
        /// branch around calling, and the foundation fails closed beneath this anyway - an empty
        /// set must never be read as "everybody".
        /// </summary>
        [Fact]
        public async Task ShouldReturnNoNamesWhenNoIdsAreGivenAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            SetupResolvedIdentityUsers();

            // when
            IReadOnlyList<ReviewerDisplayName> reviewerDisplayNames =
                await this.approvalOrchestrationService.RetrieveReviewerDisplayNamesAsync(
                    Array.Empty<string>(),
                    TestContext.Current.CancellationToken);

            // then
            reviewerDisplayNames.Should().BeEmpty();
        }
    }
}
