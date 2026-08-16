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
        public async Task ShouldRefuseDismissingAReviewWhenTheActorIsNotAuthenticatedAsync()
        {
            // given
            AccessActor unauthenticatedActor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.Publisher },
                isAuthenticated: false);

            DismissReviewRequest dismissReviewRequest =
                CreateRandomDismissReviewRequest(actor: unauthenticatedActor);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDismissApprovalReviewAsync(dismissReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldRefuseDismissingAReviewWhenTheActorUserIdIsBlankAsync(
            string? invalidUserId)
        {
            // given
            var actorWithoutUserId = new AccessActor
            {
                UserId = invalidUserId!,
                Roles = new List<string> { RoleNames.Publisher },
                IsAuthenticated = true,
            };

            DismissReviewRequest dismissReviewRequest =
                CreateRandomDismissReviewRequest(actor: actorWithoutUserId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDismissApprovalReviewAsync(dismissReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        /// <summary>
        /// The review tier is not the publisher tier. A reviewer records verdicts; clearing one is
        /// the workflow's act, and §7.7 rule 2 keeps the two apart.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseDismissingAReviewWhenTheActorIsOnlyAReviewerAsync()
        {
            // given
            string entityType = GetRandomString();

            AccessActor reviewerActor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.Reviewer,
                    RoleNames.ReviewerFor(entityType),
                });

            DismissReviewRequest dismissReviewRequest = CreateRandomDismissReviewRequest(
                actor: reviewerActor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = entityType, ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDismissApprovalReviewAsync(dismissReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotInPublisherTier);
        }

        /// <summary>
        /// The defect #190 was written about: a publisher scoped to one entity type must not clear
        /// a verdict on another. The suffix match the foundation applies row-locally cannot tell
        /// these apart — this decision can, because it is handed the approval's own subject.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseDismissingAReviewWhenThePublisherIsScopedToAnotherEntityTypeAsync()
        {
            // given
            AccessActor unrelatedPublisher = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.PublisherFor("Tag") });

            DismissReviewRequest dismissReviewRequest = CreateRandomDismissReviewRequest(
                actor: unrelatedPublisher,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = "Testimony" },
                    new RoleSubject { EntityType = "BibleReference", ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDismissApprovalReviewAsync(dismissReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotInPublisherTier);
        }

        /// <summary>
        /// Either endpoint is enough (§14.7 posture A′ rule 2) — a publisher trusted with one end
        /// of an association may act on the pairing, because the pairing is the thing being
        /// decided. Both ends are exercised so neither is passing by position.
        /// </summary>
        [Theory]
        [InlineData("ContentItem")]
        [InlineData("BibleReference")]
        public async Task ShouldPermitDismissingAReviewWhenThePublisherIsScopedToEitherEndpointAsync(
            string scopedEndpoint)
        {
            // given
            AccessActor endpointPublisher = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.PublisherFor(scopedEndpoint) });

            DismissReviewRequest dismissReviewRequest = CreateRandomDismissReviewRequest(
                actor: endpointPublisher,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = "Testimony" },
                    new RoleSubject { EntityType = "BibleReference", ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDismissApprovalReviewAsync(dismissReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        /// <summary>
        /// The narrow tier widens into the coarse one (§18.6 rule 4), so a publisher scoped to the
        /// endpoint's content type clears a check for that content type.
        /// </summary>
        [Fact]
        public async Task ShouldPermitDismissingAReviewWhenThePublisherIsScopedToTheEndpointContentTypeAsync()
        {
            // given
            AccessActor narrowPublisher = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.PublisherFor("ContentItem", "Testimony") });

            DismissReviewRequest dismissReviewRequest = CreateRandomDismissReviewRequest(
                actor: narrowPublisher,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = "Testimony" },
                    new RoleSubject { EntityType = "BibleReference", ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDismissApprovalReviewAsync(dismissReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
        }

        [Theory]
        [InlineData(RoleNames.Publisher)]
        [InlineData(RoleNames.Admin)]
        public async Task ShouldPermitDismissingAReviewForTheGlobalTiersAsync(string globalRole)
        {
            // given
            AccessActor globalActor = CreateRandomAccessActor(
                roles: new List<string> { globalRole });

            DismissReviewRequest dismissReviewRequest = CreateRandomDismissReviewRequest(
                actor: globalActor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDismissApprovalReviewAsync(dismissReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
        }
    }
}
