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
using FluentAssertions;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Tests.Unit.Models.Configurations
{
    public class EntityTypePersonalisationTests
    {
        [Fact]
        public void ShouldDeclareAReactionPersonal()
        {
            // given
            EntityType reactionEntityType = EntityType.Reaction;

            // when
            bool actualIsPersonal = EntityTypePersonalisation.IsPersonal(reactionEntityType);

            // then
            actualIsPersonal.Should().BeTrue();
        }

        [Theory]
        [InlineData(EntityType.ContentItem)]
        [InlineData(EntityType.Tag)]
        [InlineData(EntityType.BibleReference)]
        [InlineData(EntityType.Comment)]
        [InlineData(EntityType.Link)]
        [InlineData(EntityType.Attachment)]
        [InlineData(EntityType.Association)]
        public void ShouldDeclareEveryOtherEntityTypeNotPersonal(EntityType entityType)
        {
            // when
            bool actualIsPersonal = EntityTypePersonalisation.IsPersonal(entityType);

            // then
            actualIsPersonal.Should().BeFalse();
        }

        [Fact]
        public void ShouldDeclareEveryEntityType()
        {
            // given: walking the enum, so a member added without a decision fails here
            // rather than on a request (§DOM4.10 rule 4)
            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                // when
                Action lookingUpPersonalisation = () =>
                    EntityTypePersonalisation.IsPersonal(entityType);

                // then
                lookingUpPersonalisation.Should().NotThrow(
                    because: $"{entityType} must declare whether it is personal");
            }
        }

        [Fact]
        public void ShouldThrowNotSupportedExceptionForAnUndeclaredEntityType()
        {
            // given: an out-of-range value stands in for a member someone added to the enum
            // and forgot to declare here
            var undeclaredEntityType = (EntityType)int.MaxValue;

            // when
            Action lookingUpPersonalisation = () =>
                EntityTypePersonalisation.IsPersonal(undeclaredEntityType);

            // then: loudly, never a false default that files it as editorial
            lookingUpPersonalisation.Should().Throw<NotSupportedException>()
                .Where(exception =>
                    exception.Message.Contains(undeclaredEntityType.ToString())
                    && exception.Message.Contains(nameof(EntityTypePersonalisation)));
        }
    }
}
