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

        private void SetupReviewerScope(
            Guid approvalId,
            ApprovalStatus approvalStatus = ApprovalStatus.Submitted,
            string entityCreatedBy = "the-entity-owner",
            IReadOnlyList<string> activeReviewerUserIds = null,
            IReadOnlyList<ActiveReviewRequest> activeRequests = null,
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

                            ActiveRequests =
                                activeRequests ?? Array.Empty<ActiveReviewRequest>(),
                        });
        }

        /// <summary>
        /// The three subtractions of 16.7.4 in one pass - the owner, anyone who already reviewed,
        /// and anyone already invited all drop out, leaving only people who can usefully be asked.
        /// </summary>
        [Fact]
        public async Task ShouldRetrieveReviewerCandidatesExcludingOwnerReviewersAndInviteesAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
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

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<IdentityUser>
                        {
                            CreateIdentityUser(ownerId, preferredName: "Owner"),
                            CreateIdentityUser(reviewedId, preferredName: "Reviewed"),
                            CreateIdentityUser(invitedId, preferredName: "Invited"),
                            CreateIdentityUser(freshId, preferredName: "Fresh"),
                        });

            // when
            IReadOnlyList<ReviewerCandidate> candidates =
                await this.approvalOrchestrationService.RetrieveReviewerCandidatesAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then
            candidates.Should().HaveCount(1);
            candidates[0].UserId.Should().Be(freshId.ToString());
            candidates[0].DisplayName.Should().Be("Fresh");
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid approvalId = Guid.NewGuid();
            IEnumerable<string> capturedRoleNames = null;

            SetupReviewerScope(approvalId: approvalId, contentType: "Blog");

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(),
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
                "Reviewer",
                "Publisher",
                "Admin",
                "ContentItem-Reviewer",
                "ContentItem-Publisher",
                "ContentItem-Blog-Reviewer",
                "ContentItem-Blog-Publisher",
            });
        }

        [Fact]
        public async Task ShouldNotComposeContentTypeRolesWhenTheSubjectCarriesNoContentTypeAsync()
        {
            // given: 18.6 rule 5 gives the narrow tier to ContentItem alone
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid approvalId = Guid.NewGuid();
            IEnumerable<string> capturedRoleNames = null;

            SetupReviewerScope(approvalId: approvalId, contentType: null);

            this.identityUserServiceMock.Setup(service =>
                service.RetrieveIdentityUsersInRolesAsync(
                    It.IsAny<IEnumerable<string>>(),
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
