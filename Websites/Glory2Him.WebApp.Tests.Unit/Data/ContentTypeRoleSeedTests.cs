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
using System.Linq;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Data;

namespace Glory2Him.WebApp.Tests.Unit.Data
{
    /// <summary>
    /// Pins the seeded role set to the enums it is composed from.
    ///
    /// <para><b>Why this needs a test at all.</b> SeedData is the only place a role can be
    /// minted - IIdentityBroker assigns and never creates - and a role that is never seeded
    /// fails SILENTLY: the composed name is simply never found among an actor's roles and every
    /// gate falls back to the coarser tier. Nothing throws, no log line appears, and the only
    /// symptom is that a tier never admits anybody. That is exactly how the content-type tier
    /// sat unseedable while the code that reads it was live on both read paths and the write
    /// gates.</para>
    ///
    /// <para>So this asserts the SHAPE of the composition rather than a list of names: a
    /// hand-written list that drifts from the enum fails here, which is the failure mode the
    /// seeding rule in design 18.6 exists to prevent.</para>
    /// </summary>
    public class ContentTypeRoleSeedTests
    {
        /// <summary>
        /// Every ContentType member gets the narrow review pair, Series and Topic included.
        /// They are ContentType members on ContentItem, and 18.6 rule 5 scopes the tier to the
        /// entity type rather than to a chosen subset of its content types.
        /// </summary>
        [Fact]
        public void ShouldSeedTheNarrowReviewTierForEveryContentType()
        {
            // given
            string[] seededRoles = SeedData.CoreRoles;

            // when / then
            foreach (ContentType contentType in Enum.GetValues<ContentType>())
            {
                seededRoles.Should().Contain(
                    Roles.ReviewerFor(EntityType.ContentItem, contentType),
                    because: $"a reviewer must be scopeable to {contentType} alone (18.6 rule 5)");

                seededRoles.Should().Contain(
                    Roles.PublisherFor(EntityType.ContentItem, contentType),
                    because: $"a publisher must be scopeable to {contentType} alone (18.6 rule 5)");
            }
        }

        /// <summary>
        /// ContentItem ONLY. Composing the narrow tier for any other entity type would mint
        /// exactly the roles 14.7 posture A-prime rule 6 exists to refuse - AssociationService
        /// tests the endpoint type as well as the content type so that a
        /// ContentItem-Testimony-Reviewer can never be matched against a Tag endpoint that
        /// happens to carry Testimony.
        /// </summary>
        [Fact]
        public void ShouldNotSeedTheNarrowTierForAnyEntityTypeOtherThanContentItem()
        {
            // given
            string[] seededRoles = SeedData.CoreRoles;

            EntityType[] otherEntityTypes = Enum.GetValues<EntityType>()
                .Where(entityType => entityType != EntityType.ContentItem)
                .ToArray();

            // when / then
            foreach (EntityType entityType in otherEntityTypes)
            {
                foreach (ContentType contentType in Enum.GetValues<ContentType>())
                {
                    seededRoles.Should().NotContain(
                        Roles.ReviewerFor(entityType, contentType),
                        because: "only ContentItem carries a ContentType (18.6 rule 5)");

                    seededRoles.Should().NotContain(
                        Roles.PublisherFor(entityType, contentType),
                        because: "only ContentItem carries a ContentType (18.6 rule 5)");
                }
            }
        }

        /// <summary>
        /// There is deliberately no ReadOnlyFor(EntityType, ContentType). The block tier has no
        /// content-type tier, and seeding one would invent a role nothing issues and nothing
        /// checks - two roles per content type, not three.
        /// </summary>
        [Fact]
        public void ShouldSeedExactlyTwoNarrowRolesPerContentType()
        {
            // given
            string[] seededRoles = SeedData.CoreRoles;
            int contentTypeCount = Enum.GetValues<ContentType>().Length;

            // when
            int narrowRoleCount = seededRoles
                .Count(roleName => roleName.StartsWith(
                    $"{EntityType.ContentItem}-", StringComparison.Ordinal)
                        && roleName.Split('-').Length == 3);

            // then
            narrowRoleCount.Should().Be(contentTypeCount * 2);
        }

        /// <summary>
        /// The seed is idempotent and is applied by name, so a duplicate would be harmless at
        /// runtime - but it would also mean two loops are composing the same tier, which is how
        /// one of them later drifts.
        /// </summary>
        [Fact]
        public void ShouldSeedEveryRoleNameExactlyOnce()
        {
            // given
            string[] seededRoles = SeedData.CoreRoles;

            // when / then
            seededRoles.Should().OnlyHaveUniqueItems();
        }
    }
}
