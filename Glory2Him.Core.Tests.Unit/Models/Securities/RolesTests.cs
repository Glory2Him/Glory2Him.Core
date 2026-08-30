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
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Tests.Unit.Models.Securities
{
    public class RolesTests
    {
        // the hand-written constants, keyed by the entity type they belong to. They stay in
        // the catalogue because a constant can appear in an xUnit [InlineData] attribute and
        // a method call cannot — but that means two spellings of the same name exist, and
        // the tests below are what stop them drifting apart.
        private static readonly Dictionary<EntityType, (string ReadOnly, string Reviewer, string Publisher)>
            HandWrittenRoleNames = new()
            {
                [EntityType.ContentItem] =
                    (Roles.ContentItemReadOnly, Roles.ContentItemReviewers, Roles.ContentItemPublishers),
                [EntityType.Tag] =
                    (Roles.TagReadOnly, Roles.TagReviewers, Roles.TagPublishers),
                [EntityType.Reaction] =
                    (Roles.ReactionReadOnly, Roles.ReactionReviewers, Roles.ReactionPublishers),
                [EntityType.Comment] =
                    (Roles.CommentReadOnly, Roles.CommentReviewers, Roles.CommentPublishers),
                [EntityType.BibleReference] =
                    (Roles.BibleReferenceReadOnly, Roles.BibleReferenceReviewers, Roles.BibleReferencePublishers),
                [EntityType.Link] =
                    (Roles.LinkReadOnly, Roles.LinkReviewers, Roles.LinkPublishers),
                [EntityType.Attachment] =
                    (Roles.AttachmentReadOnly, Roles.AttachmentReviewers, Roles.AttachmentPublishers)
            };

        [Fact]
        public void ShouldComposeANonEmptyRoleNameForEveryEntityType()
        {
            // given: authorization now composes role names rather than reading constants, so
            // a member that composes to nothing would silently deny — or, worse, match a
            // name nobody intended
            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                // when
                string readOnly = Roles.ReadOnlyFor(entityType);
                string reviewer = Roles.ReviewersFor(entityType);
                string publisher = Roles.PublishersFor(entityType);

                // then
                readOnly.Should().NotBeNullOrWhiteSpace();
                reviewer.Should().NotBeNullOrWhiteSpace();
                publisher.Should().NotBeNullOrWhiteSpace();

                readOnly.Should().Be($"{entityType}-ReadOnly");
                reviewer.Should().Be($"{entityType}-Reviewers");
                publisher.Should().Be($"{entityType}-Publishers");
            }
        }

        [Fact]
        public void ShouldComposeTheSameNameAsTheHandWrittenConstant()
        {
            // given: every entity type that has hand-written constants must agree with the
            // composed form, or a check written one way silently misses a role granted the
            // other way
            foreach ((EntityType entityType, var expected) in HandWrittenRoleNames)
            {
                // when / then
                Roles.ReadOnlyFor(entityType).Should().Be(expected.ReadOnly);
                Roles.ReviewersFor(entityType).Should().Be(expected.Reviewer);
                Roles.PublishersFor(entityType).Should().Be(expected.Publisher);
            }
        }

        [Fact]
        public void ShouldHaveHandWrittenConstantsForEveryApprovableEntityType()
        {
            // given: Association is the one member with no scoped roles of its own — its
            // authorization derives from its two endpoints (design §14.7, §18.6). Every
            // other member must have constants, and this is what catches the next one added.
            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                if (entityType == EntityType.Association)
                {
                    HandWrittenRoleNames.Should().NotContainKey(entityType,
                        because: "an association has no scoped roles of its own");

                    continue;
                }

                HandWrittenRoleNames.Should().ContainKey(entityType,
                    because: $"{entityType} needs role constants in the catalogue");
            }
        }

        [Theory]
        [InlineData(EntityType.ContentItem, ContentType.Testimony, "ContentItem-Testimony-Reviewers")]
        [InlineData(EntityType.ContentItem, ContentType.Story, "ContentItem-Story-Reviewers")]
        [InlineData(EntityType.ContentItem, ContentType.BlogPost, "ContentItem-BlogPost-Reviewers")]
        public void ShouldComposeTheNarrowReviewerRoleWithTheCapabilityLast(
            EntityType entityType,
            ContentType contentType,
            string expectedRoleName)
        {
            // when
            string actualRoleName = Roles.ReviewersFor(entityType, contentType);

            // then: capability LAST. Three approval services identify a reviewer by suffix
            // match, so a name ending in the content type would not be recognised as a
            // review role at all (design §18.6).
            actualRoleName.Should().Be(expectedRoleName);
            actualRoleName.Should().EndWith(Roles.ReviewersSuffix);
        }

        [Theory]
        [InlineData(EntityType.ContentItem, ContentType.Testimony, "ContentItem-Testimony-Publishers")]
        [InlineData(EntityType.ContentItem, ContentType.Quote, "ContentItem-Quote-Publishers")]
        public void ShouldComposeTheNarrowPublisherRoleWithTheCapabilityLast(
            EntityType entityType,
            ContentType contentType,
            string expectedRoleName)
        {
            // when
            string actualRoleName = Roles.PublishersFor(entityType, contentType);

            // then
            actualRoleName.Should().Be(expectedRoleName);
            actualRoleName.Should().EndWith(Roles.PublishersSuffix);
        }

        [Fact]
        public void ShouldNotComposeANarrowRoleThatCollidesWithACoarseOne()
        {
            // given: the narrow tier must never produce a name the coarse tier also produces,
            // or a content-type-scoped grant would silently widen to the whole entity type
            var coarseRoleNames = new HashSet<string>();

            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                coarseRoleNames.Add(Roles.ReviewersFor(entityType));
                coarseRoleNames.Add(Roles.PublishersFor(entityType));
            }

            foreach (ContentType contentType in Enum.GetValues<ContentType>())
            {
                // when
                string narrowReviewer = Roles.ReviewersFor(EntityType.ContentItem, contentType);
                string narrowPublisher = Roles.PublishersFor(EntityType.ContentItem, contentType);

                // then
                coarseRoleNames.Should().NotContain(narrowReviewer);
                coarseRoleNames.Should().NotContain(narrowPublisher);
            }
        }
    }
}
