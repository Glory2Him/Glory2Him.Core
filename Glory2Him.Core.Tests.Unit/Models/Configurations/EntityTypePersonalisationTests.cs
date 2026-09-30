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
    }
}
