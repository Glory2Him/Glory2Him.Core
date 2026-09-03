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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
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
        private static IdentityUser CreateIdentityUser(Guid userId, string preferredName = null) =>
            new IdentityUser
            {
                Id = userId,
                PreferredName = preferredName,
                Name = "Given",
                Surname = "Family",
                UserName = "someone",
            };

        // The review tier's membership, for the two operations that still ask for it - the
        // candidates read and the invitation's rule 3 eligibility check. It lives here rather
        // than beside either because both are covered from this file's fixtures, and because a
        // stub spelled out at every call site is longer than the assertion it exists to enable.
        //
        // A test whose SUBJECT is the role names asked for cannot use this: it needs a Callback
        // to capture the argument, and a helper that swallowed it would hide the very thing under
        // test.
        //
        // THE MATCHER IS NOT It.IsAny, and that is load-bearing. Both operations now ask the
        // identity store TWICE - once for the review tier, once for the ReadOnly veto (18.6
        // rule 2) - and both calls land on this one method. The global ReadOnly is the one name
        // that only ever appears in the veto's list, so it is what tells them apart. Stubbing
        // "the tier read" with It.IsAny would answer the veto read with the same people and
        // subtract every candidate it had just offered.
        private void SetupTierMembers(params IdentityUser[] identityUsers) =>
            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.Is<IEnumerable<string>>(roleNames =>
                        !roleNames.Contains(Roles.ReadOnly)),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(identityUsers.ToList());

        // Nobody, by default - set in the constructor so every test starts unblocked, which is
        // what makes a subtraction visible as the veto's doing rather than the tier's.
        private void SetupBlockedUsers(params IdentityUser[] blockedUsers) =>
            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.Is<IEnumerable<string>>(roleNames =>
                        roleNames.Contains(Roles.ReadOnly)),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(blockedUsers.ToList());

        // The scope as it comes back when the entity behind the approval could not be read -
        // the subject names its entity type but carries no content type, and says so with the
        // flag. An ABSENT content type and an UNKNOWN one must not be treated alike.
        private void SetupUnresolvedReviewerScope(Guid approvalId)
        {
            SetupReviewerScope(approvalId: approvalId, contentType: null);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewerScope
                        {
                            ApprovalId = approvalId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                            EntityCreatedBy = Guid.NewGuid().ToString(),

                            RoleSubjects = new[]
                            {
                                new RoleSubject
                                {
                                    EntityType = nameof(EntityType.ContentItem),
                                    ContentType = null,
                                    IsEntityUnresolved = true,
                                }
                            },

                            ActiveReviewerUserIds = Array.Empty<string>(),
                            RecordedReviewerUserIds = Array.Empty<string>(),
                            ActiveRequests = Array.Empty<ActiveReviewRequest>(),
                        });
        }

        private void SetupReviewerScope(
            Guid approvalId,
            ApprovalStatus approvalStatus = ApprovalStatus.Submitted,
            string entityCreatedBy = "the-entity-owner",
            IReadOnlyList<string> activeReviewerUserIds = null,
            IReadOnlyList<ActiveReviewRequest> activeRequests = null,
            IReadOnlyList<string> recordedReviewerUserIds = null,
            string contentType = null)
        {
            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalEntityMatch
                        {
                            Id = approvalId,
                            ApprovalStatus = approvalStatus,
                            IsDeleted = false,
                        });

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewerScope
                        {
                            ApprovalId = approvalId,
                            ApprovalStatus = approvalStatus,
                            EntityCreatedBy = entityCreatedBy,

                            RoleSubjects = new[]
                            {
                                new RoleSubject
                                {
                                    EntityType = nameof(EntityType.ContentItem),
                                    ContentType = contentType,
                                }
                            },

                            ActiveReviewerUserIds =
                                activeReviewerUserIds ?? Array.Empty<string>(),

                            // Defaulted to the active set, because every standing review is a
                            // recorded one - the broker builds this field from the same rows with
                            // nothing subtracted. A test that cares about the DIFFERENCE - a
                            // dismissed or withdrawn verdict, which is the case the resolver
                            // exists for - passes it explicitly.
                            RecordedReviewerUserIds =
                                recordedReviewerUserIds
                                    ?? activeReviewerUserIds
                                    ?? Array.Empty<string>(),

                            ActiveRequests =
                                activeRequests ?? Array.Empty<ActiveReviewRequest>(),
                        });
        }

        /// <summary>
        /// TWO subtractions, and only two: the entity's own author, and anyone a <c>ReadOnly</c>
        /// in this entity's scope covers (§18.6 rule 2). Rule 3 refuses an invitation aimed at
        /// either outright, so listing them would offer a click that always fails. This case
        /// holds the block set empty and pins the owner half; the veto half is pinned in
        /// ApprovalOrchestrationServiceTests.ReadOnlyVeto.
        ///
        /// <para>Everyone else in the tier stays, INCLUDING people who have already answered and
        /// people already invited. The read answers "who belongs to this round", not "who is not
        /// yet dealt with": a moderation surface renders an answered person inert and an invited
        /// one under its own heading, so somebody searching for a name finds them and learns
        /// their state. A surface cannot show a person it was never sent, and subtracting them
        /// here made the whole ticked-and-inert branch unreachable in production.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRetrieveReviewerCandidatesExcludingOnlyTheEntityOwnerAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();
            Guid ownerId = Guid.NewGuid();
            Guid reviewedId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            Guid freshId = Guid.NewGuid();

            SetupReviewerScope(
                approvalId: approvalId,
                entityCreatedBy: ownerId.ToString(),
                activeReviewerUserIds: new[] { reviewedId.ToString() },
                activeRequests: new[]
                {
                    new ActiveReviewRequest
                    {
                        Id = Guid.NewGuid(),
                        RequestedUserId = invitedId.ToString(),
                    }
                });

            SetupTierMembers(
                CreateIdentityUser(ownerId, preferredName: "Owner"),
                CreateIdentityUser(reviewedId, preferredName: "Reviewed"),
                CreateIdentityUser(invitedId, preferredName: "Invited"),
                CreateIdentityUser(freshId, preferredName: "Fresh"));

            // when
            IReadOnlyList<ReviewerCandidate> candidates =
                await this.approvalOrchestrationService.RetrieveReviewerCandidatesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            candidates.Select(candidate => candidate.UserId).Should().BeEquivalentTo(new[]
            {
                freshId.ToString(),
                reviewedId.ToString(),
                invitedId.ToString(),
            });

            candidates.Select(candidate => candidate.UserId)
                .Should().NotContain(ownerId.ToString());
        }

        /// <summary>
        /// 18.6 composed in one place. The global tier, the entity-scoped pair, and - because this
        /// subject carries a content type - the narrow pair too. Asserted on the names handed to
        /// the identity store, because that set IS the tier definition at runtime.
        /// </summary>
        [Fact]
        public async Task ShouldComposeTheReviewTierRoleNamesFromTheApprovalSubjectsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            IEnumerable<string> capturedRoleNames = null;

            SetupReviewerScope(approvalId: approvalId, contentType: "Blog");

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.Is<IEnumerable<string>>(roleNames =>
                        !roleNames.Contains(Roles.ReadOnly)),
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
                "Reviewers",
                "Publishers",
                "Administrators",
                "ContentItem-Reviewers",
                "ContentItem-Publishers",
                "ContentItem-Blog-Reviewers",
                "ContentItem-Blog-Publishers",
            });
        }

        [Fact]
        public async Task ShouldNotComposeContentTypeRolesWhenTheSubjectCarriesNoContentTypeAsync()
        {
            // given: 18.6 rule 5 gives the narrow tier to ContentItem alone
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            IEnumerable<string> capturedRoleNames = null;

            SetupReviewerScope(approvalId: approvalId, contentType: null);

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.Is<IEnumerable<string>>(roleNames =>
                        !roleNames.Contains(Roles.ReadOnly)),
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
            capturedRoleNames.Should().NotContain(roleName => roleName.Contains("-Blog-"));
            capturedRoleNames.Should().HaveCount(5);
        }
    }
}
