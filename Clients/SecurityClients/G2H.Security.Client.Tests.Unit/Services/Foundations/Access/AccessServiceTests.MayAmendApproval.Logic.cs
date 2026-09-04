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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Securities;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Access
{
    public partial class AccessServiceTests
    {
        [Fact]
        public async Task ShouldRefuseAmendingAnApprovalWhenTheActorIsNotAuthenticatedAsync()
        {
            // given
            AccessActor unauthenticatedActor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.Reviewers },
                isAuthenticated: false);

            AmendApprovalRequest amendApprovalRequest =
                CreateRandomAmendApprovalRequest(actor: unauthenticatedActor);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldRefuseAmendingAnApprovalWhenTheActorUserIdIsBlankAsync(
            string? invalidUserId)
        {
            // given
            var actorWithoutUserId = new AccessActor
            {
                UserId = invalidUserId!,
                Roles = new List<string> { RoleNames.Reviewers },
                IsAuthenticated = true,
            };

            AmendApprovalRequest amendApprovalRequest =
                CreateRandomAmendApprovalRequest(actor: actorWithoutUserId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        /// <summary>
        /// The submitter of an approval may amend their own, holding no role at all — §14.7
        /// posture D rule 3 admits "resubmission by the submitter", and
        /// <c>ModifyApprovalAsync</c> is the only verb that moves an approval Draft↔Submitted.
        ///
        /// <para>This is why the owner branch lives inside this decision rather than being left
        /// to the caller to OR in: two throwing gates compose to an AND, which deletes it.</para>
        /// </summary>
        [Fact]
        public async Task ShouldPermitAmendingAnApprovalWhenTheActorIsItsSubmitterAsync()
        {
            // given: no roles whatsoever
            AccessActor submitter = CreateRandomAccessActor(roles: new List<string>());

            AmendApprovalRequest amendApprovalRequest = CreateRandomAmendApprovalRequest(
                actor: submitter,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = "Testimony" },
                },
                entityCreatedBy: submitter.UserId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        /// <summary>
        /// A blank submitter must never match a blank actor id. The actor gate catches that first,
        /// but the owner comparison is blank-safe in its own right so the order cannot become
        /// load-bearing by accident.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldRefuseAmendingAnApprovalWhenTheSubmitterIsBlankAsync(
            string? blankSubmitter)
        {
            // given
            AccessActor actorWithoutRoles = CreateRandomAccessActor(roles: new List<string>());

            AmendApprovalRequest amendApprovalRequest = CreateRandomAmendApprovalRequest(
                actor: actorWithoutRoles,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = null },
                },
                entityCreatedBy: blankSubmitter!);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotInReviewTier);
        }

        [Fact]
        public async Task ShouldRefuseAmendingAnApprovalWhenTheActorHoldsNoReviewTierRoleAsync()
        {
            // given
            string entityType = GetRandomString();

            // An UNROLED actor, not a ReadOnly one. ReadOnly is now the veto and is answered
            // before any tier is asked (18.6 rule 2), so an actor holding it would be refused
            // for the wrong reason and this test would stop proving anything about the tier.
            AccessActor untieredActor = CreateRandomAccessActor(
                roles: new List<string>());

            AmendApprovalRequest amendApprovalRequest = CreateRandomAmendApprovalRequest(
                actor: untieredActor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = entityType, ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotInReviewTier);
        }

        /// <summary>
        /// The REVIEW tier, not the publisher tier — §14.7 posture D rule 3 has reviewers move an
        /// approval's status through this path, so a plain reviewer must clear it. The publisher
        /// tier widens into the review tier, so the publisher spellings clear it too.
        /// </summary>
        [Theory]
        [InlineData(RoleNames.Reviewers)]
        [InlineData(RoleNames.Publishers)]
        [InlineData(RoleNames.Administrators)]
        public async Task ShouldPermitAmendingAnApprovalForEachGlobalReviewTierRoleAsync(
            string globalRole)
        {
            // given
            AccessActor globalActor = CreateRandomAccessActor(
                roles: new List<string> { globalRole });

            AmendApprovalRequest amendApprovalRequest = CreateRandomAmendApprovalRequest(
                actor: globalActor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
        }

        /// <summary>
        /// Either endpoint is enough (§14.7 posture A′ rule 2). Both ends and both scoped
        /// spellings, so nothing passes by position and the publisher-widens-into-reviewer rule
        /// is exercised at the endpoint tier too.
        /// </summary>
        [Theory]
        [InlineData("ContentItem", false)]
        [InlineData("BibleReference", false)]
        [InlineData("ContentItem", true)]
        [InlineData("BibleReference", true)]
        public async Task ShouldPermitAmendingAnApprovalWhenScopedToEitherEndpointAsync(
            string scopedEndpoint,
            bool asPublisher)
        {
            // given
            string scopedRole = asPublisher
                ? RoleNames.PublishersFor(scopedEndpoint)
                : RoleNames.ReviewersFor(scopedEndpoint);

            AccessActor endpointActor = CreateRandomAccessActor(
                roles: new List<string> { scopedRole });

            AmendApprovalRequest amendApprovalRequest = CreateRandomAmendApprovalRequest(
                actor: endpointActor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = "Testimony" },
                    new RoleSubject { EntityType = "BibleReference", ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
        }

        /// <summary>
        /// #190's headline case at the amend surface: a role scoped to an entity type that is
        /// neither endpoint clears nothing.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseAmendingAnApprovalWhenScopedToNeitherEndpointAsync()
        {
            // given
            AccessActor unrelatedActor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.ReviewersFor("Tag") });

            AmendApprovalRequest amendApprovalRequest = CreateRandomAmendApprovalRequest(
                actor: unrelatedActor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = "Testimony" },
                    new RoleSubject { EntityType = "BibleReference", ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotInReviewTier);
        }

        /// <summary>
        /// The narrow tier widens into the coarse one (§18.6 rule 4).
        /// </summary>
        [Fact]
        public async Task ShouldPermitAmendingAnApprovalWhenScopedToTheEndpointContentTypeAsync()
        {
            // given
            AccessActor narrowActor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.ReviewersFor("ContentItem", "Testimony") });

            AmendApprovalRequest amendApprovalRequest = CreateRandomAmendApprovalRequest(
                actor: narrowActor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = "Testimony" },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
        }
    }
}
