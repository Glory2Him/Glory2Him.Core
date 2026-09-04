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
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// How the <c>ReadOnly</c> veto reaches the approval SURFACE (design §18.6 rule 2, §16.7.2,
    /// §16.7.4, §7.9). An approval has no role vocabulary of its own — its scope is derived from
    /// the attached entity — so a block in scope stops the holder voting on or deciding it, not
    /// merely writing the content.
    ///
    /// <para>Two of the three surfaces get the answer for free, and are asserted here so that
    /// stays true: the verdict's per-caller <c>CanApprove</c> IS the decision verdict, so a panel
    /// taking it verbatim (§20.6.1) reports the block instead of offering a control the server
    /// would then refuse. The third — the candidate list and the invitation — has to subtract
    /// explicitly, because inviting somebody who cannot vote stalls the round for no reason.
    /// </para>
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldReportTheBlockInTheVerdictWhenTheCallerIsReadOnlyForTheEntityAsync()
        {
            // given: nothing about the approval blocks it — the conditions are met — and the
            // caller is still refused. Without the reason travelling into the reason set, a
            // panel would render "nothing is blocking this" beside a disabled approve button,
            // which is the one outcome guaranteed to look like a bug.
            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted));
            SetupConditions(CreateMetConditions());

            SetupAccessDecisions(
                decisionVerdict: RefusedVerdict(AccessDenialReason.BlockedByReadOnlyRole),
                bypassVerdict: RefusedVerdict(AccessDenialReason.BlockedByReadOnlyRole));

            // when
            ApprovalVerdict actualVerdict =
                await this.approvalOrchestrationService.RetrieveApprovalVerdictAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            actualVerdict.CanApprove.Should().BeFalse();

            // The bypass is closed too: what a bypass waives are the §8.5 conditions, never the
            // veto, so a route that survived it would make the block advisory.
            actualVerdict.IsBypassAllowedForCurrentUser.Should().BeFalse();

            actualVerdict.BlockReasons.Select(reason => reason.Code)
                .Should().ContainSingle()
                .Which.Should().Be(AccessDenialReason.BlockedByReadOnlyRole);

            // Composed in Core, and it says the sanction applies without naming which scope of
            // it fired — no scope of it is appealable through this surface.
            actualVerdict.BlockReasons.Single().Message.Should().Be(
                "Your account is restricted to read-only for this content, so you cannot "
                    + "review or approve it.");
        }

        /// THE ONE SUBTRACTION IS THE AUTHOR, and it is theirs alone. A tier member who did not
        /// write the content is offered however they hold their tier — including an
        /// Administrator, since Administrators are named in the candidate roles alongside
        /// Reviewers and Publishers. Pinned because the live picker read empty for an
        /// administrator looking at their own contribution while a second administrator existed,
        /// and "the author is subtracted" is only the right answer if it subtracts nobody else.
        [Fact]
        public async Task ShouldOfferAnAdministratorWhoDidNotWriteTheContentAsync()
        {
            // given: one administrator wrote the content, another did not. The subtraction is
            // keyed on the ENTITY's author, not on whoever is looking.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Guid authorId = Guid.NewGuid();
            Guid otherAdministratorId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: Guid.NewGuid(),
                entityCreatedBy: authorId.ToString(),
                contentType: nameof(ContentType.Quote));

            SetupTierMembers(
                CreateIdentityUser(authorId, preferredName: "The Author"),
                CreateIdentityUser(otherAdministratorId, preferredName: "The Other Admin"));

            // when
            IReadOnlyList<ReviewerCandidate> candidates =
                await this.approvalOrchestrationService.RetrieveReviewerCandidatesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: the author is gone and the other administrator is offered
            candidates.Select(candidate => candidate.UserId)
                .Should().ContainSingle()
                .Which.Should().Be(otherAdministratorId.ToString());
        }

        [Fact]
        public async Task ShouldExcludeBlockedUsersFromTheReviewerCandidatesAsync()
        {
            // given: a tier member who also holds a ReadOnly covering this entity. They cannot
            // cast a vote at all, so offering them is offering a click that always fails — the
            // same reasoning the entity's owner is subtracted on, and unlike the answered and
            // invited people who are deliberately left in, this is not a state a moderator can
            // clear by asking again.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid blockedId = Guid.NewGuid();
            Guid freshId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: Guid.NewGuid(),
                entityCreatedBy: Guid.NewGuid().ToString(),
                contentType: nameof(ContentType.Quote));

            SetupTierMembers(
                CreateIdentityUser(blockedId, preferredName: "Blocked"),
                CreateIdentityUser(freshId, preferredName: "Fresh"));

            SetupBlockedUsers(CreateIdentityUser(blockedId, preferredName: "Blocked"));

            // when
            IReadOnlyList<ReviewerCandidate> candidates =
                await this.approvalOrchestrationService.RetrieveReviewerCandidatesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            candidates.Select(candidate => candidate.UserId)
                .Should().BeEquivalentTo(new[] { freshId.ToString() });
        }

        [Fact]
        public async Task ShouldComposeTheBlockRoleNamesFromTheApprovalSubjectsAsync()
        {
            // given: the veto's names are composed the same way the tier's are, and read the
            // other way round — the global block, the entity-scoped one, and the
            // content-type-scoped one where the subject carries a content type. Asserted on the
            // names handed to the identity store, because that set IS the veto at runtime.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            IEnumerable<string> capturedRoleNames = null;

            SetupReviewerScope(
                approvalId: Guid.NewGuid(),
                contentType: nameof(ContentType.Quote));

            SetupTierMembers();

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.Is<IEnumerable<string>>(roleNames =>
                        roleNames.Contains(Roles.ReadOnly)),
                    It.IsAny<CancellationToken>()))
                        .Callback<IEnumerable<string>, CancellationToken>(
                            (roleNames, token) => capturedRoleNames = roleNames)
                        .ReturnsAsync(new List<IdentityUser>());

            // when
            await this.approvalOrchestrationService.RetrieveReviewerCandidatesAsync(
                EntityType.ContentItem,
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

            // then
            capturedRoleNames.Should().BeEquivalentTo(new[]
            {
                "ReadOnly",
                "ContentItem-ReadOnly",
                "ContentItem-Quote-ReadOnly",
            });
        }

        [Fact]
        public async Task ShouldNotComposeAContentTypeBlockWhenTheSubjectCarriesNoContentTypeAsync()
        {
            // given: only ContentItem carries a content type (§18.6 rule 5), so a subject
            // without one costs that tier rather than composing a name nothing seeds.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            IEnumerable<string> capturedRoleNames = null;

            SetupReviewerScope(approvalId: Guid.NewGuid(), contentType: null);
            SetupTierMembers();

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.Is<IEnumerable<string>>(roleNames =>
                        roleNames.Contains(Roles.ReadOnly)),
                    It.IsAny<CancellationToken>()))
                        .Callback<IEnumerable<string>, CancellationToken>(
                            (roleNames, token) => capturedRoleNames = roleNames)
                        .ReturnsAsync(new List<IdentityUser>());

            // when
            await this.approvalOrchestrationService.RetrieveReviewerCandidatesAsync(
                EntityType.ContentItem,
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

            // then
            capturedRoleNames.Should().BeEquivalentTo(new[]
            {
                "ReadOnly",
                "ContentItem-ReadOnly",
            });
        }

        [Fact]
        public async Task ShouldComposeEveryScopedBlockNameWhenTheSubjectIsUnresolvedAsync()
        {
            // given: an approval outliving the content item it hangs off. The gatherer could
            // not read the entity, so the subject carries no content type and says so with
            // IsEntityUnresolved. IAccessClient fails CLOSED on that and refuses the vote — so
            // this read has to fail closed too, or it offers a candidate whose vote the server
            // then refuses, which is the unanswerable invitation §7.9 rule 3 forbids.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            IEnumerable<string> capturedRoleNames = null;

            SetupUnresolvedReviewerScope(approvalId: Guid.NewGuid());
            SetupTierMembers();

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.Is<IEnumerable<string>>(roleNames =>
                        roleNames.Contains(Roles.ReadOnly)),
                    It.IsAny<CancellationToken>()))
                        .Callback<IEnumerable<string>, CancellationToken>(
                            (roleNames, token) => capturedRoleNames = roleNames)
                        .ReturnsAsync(new List<IdentityUser>());

            // when
            await this.approvalOrchestrationService.RetrieveReviewerCandidatesAsync(
                EntityType.ContentItem,
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

            // then: the subject NAMES its entity type — only the content type is undecidable —
            // so the fail-closed set is that type's own block names and no others. A sanction on
            // another entity type stays silent, exactly as IAccessClient decides it.
            var expectedRoleNames = new List<string>
            {
                Roles.ReadOnly,
                Roles.ReadOnlyFor(EntityType.ContentItem),
            };

            foreach (ContentType contentType in Enum.GetValues<ContentType>())
            {
                expectedRoleNames.Add(Roles.ReadOnlyFor(EntityType.ContentItem, contentType));
            }

            capturedRoleNames.Should().BeEquivalentTo(expectedRoleNames);

            capturedRoleNames.Should().NotContain(Roles.ReadOnlyFor(EntityType.Tag));
        }

        [Fact]
        public async Task ShouldComposeEveryScopedBlockNameWhenTheSubjectCannotBeNamedAsync()
        {
            // given: the approval hangs off an association the gatherer could not read, so it
            // can name neither endpoint and reports a blank entity type. Which entity types are
            // even in play is what could not be established, so every scoped block goes on the
            // list — the arm IAccessClient mirrors by matching any scoped ReadOnly.
            //
            // The assertion that matters is a block name for an entity type the subject NEVER
            // NAMED. Without it this case is satisfied by the named-subject arm too, which is
            // exactly how the arm went untested: the only fixture setting the flag also named
            // ContentItem, so the test pointing at this branch never reached it and the branch
            // could be deleted with the whole suite green.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            IEnumerable<string> capturedRoleNames = null;

            SetupUnnameableReviewerScope(approvalId: Guid.NewGuid());
            SetupTierMembers();

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.Is<IEnumerable<string>>(roleNames =>
                        roleNames.Contains(Roles.ReadOnly)),
                    It.IsAny<CancellationToken>()))
                        .Callback<IEnumerable<string>, CancellationToken>(
                            (roleNames, token) => capturedRoleNames = roleNames)
                        .ReturnsAsync(new List<IdentityUser>());

            // when
            await this.approvalOrchestrationService.RetrieveReviewerCandidatesAsync(
                EntityType.Association,
                Guid.NewGuid(),
                TestContext.Current.CancellationToken);

            // then
            var expectedRoleNames = new List<string> { Roles.ReadOnly };

            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                expectedRoleNames.Add(Roles.ReadOnlyFor(entityType));
            }

            foreach (ContentType contentType in Enum.GetValues<ContentType>())
            {
                expectedRoleNames.Add(Roles.ReadOnlyFor(EntityType.ContentItem, contentType));
            }

            capturedRoleNames.Should().BeEquivalentTo(expectedRoleNames);

            // The one assertion the named-subject arm cannot satisfy.
            capturedRoleNames.Should().Contain(Roles.ReadOnlyFor(EntityType.Tag));
        }

        [Fact]
        public async Task ShouldExcludeANarrowlyBlockedUserFromCandidatesWhenTheSubjectIsUnresolvedAsync()
        {
            // given: the behaviour the composition above exists for. Before it, this user was
            // offered as a candidate and could be invited, and only their VOTE was refused.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid blockedId = Guid.NewGuid();
            Guid freshId = Guid.NewGuid();

            SetupUnresolvedReviewerScope(approvalId: Guid.NewGuid());

            SetupTierMembers(
                CreateIdentityUser(blockedId, preferredName: "Blocked"),
                CreateIdentityUser(freshId, preferredName: "Fresh"));

            // The stub answers ONLY the fail-closed composition. A blanket SetupBlockedUsers
            // would answer whatever names were asked, so this case would pass with the
            // IsEntityUnresolved branch reverted and assert nothing it is named for.
            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.Is<IEnumerable<string>>(roleNames =>
                        roleNames.Contains(
                            Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Quote))),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<IdentityUser>
                        {
                            CreateIdentityUser(blockedId, preferredName: "Blocked"),
                        });

            // when
            IReadOnlyList<ReviewerCandidate> candidates =
                await this.approvalOrchestrationService.RetrieveReviewerCandidatesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            candidates.Select(candidate => candidate.UserId)
                .Should().BeEquivalentTo(new[] { freshId.ToString() });
        }

        [Fact]
        public async Task ShouldThrowOnRequestIfTheInvitedUserIsBlockedForTheEntityAsync()
        {
            // given: somebody can hold a grant and a block together, so being IN the tier is not
            // the same as being able to vote. Refused rather than dissolved like a duplicate:
            // rule 4's idempotence covers invitations that are redundant, not ones that can
            // never be answered.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid blockedId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: Guid.NewGuid(),
                entityCreatedBy: Guid.NewGuid().ToString(),
                contentType: nameof(ContentType.Quote));

            SetupTierMembers(CreateIdentityUser(blockedId, preferredName: "Blocked"));
            SetupBlockedUsers(CreateIdentityUser(blockedId, preferredName: "Blocked"));

            // when
            ValueTask<ApprovalReviewRequest> requestTask =
                this.approvalOrchestrationService.RequestApprovalReviewAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    blockedId.ToString(),
                    TestContext.Current.CancellationToken);

            ApprovalOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalOrchestrationValidationException>(
                    requestTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeOfType<InvalidApprovalOrchestrationException>();

            actualException.InnerException!.Message.Should().Be(
                $"User {blockedId} is restricted to read-only for this "
                    + "entity and cannot review it.");

            this.approvalReviewRequestServiceMock.Verify(service =>
                service.AddApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
