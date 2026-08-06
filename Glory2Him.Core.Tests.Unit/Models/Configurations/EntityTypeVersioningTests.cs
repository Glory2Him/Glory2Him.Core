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
    public class EntityTypeVersioningTests
    {
        [Fact]
        public void ShouldDeclareAPublicationModelForEveryEntityType()
        {
            // given: design §7.5.1 rule 2 — adding an entity type without declaring its
            // publication model is an incomplete change, and a missing row is a hard error
            // rather than a default. This is the test that turns "incomplete" into "red".
            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                // when
                Action declaringPublicationModel = () =>
                    EntityTypeVersioning.IsVersioned(entityType);

                // then
                declaringPublicationModel.Should().NotThrow(
                    because: $"{entityType} must declare whether it is versioned");
            }
        }

        [Theory]
        [InlineData(EntityType.ContentItem, true)]
        [InlineData(EntityType.Link, true)]
        [InlineData(EntityType.Attachment, true)]
        [InlineData(EntityType.BibleReference, false)]
        [InlineData(EntityType.Tag, false)]
        [InlineData(EntityType.Reaction, false)]
        [InlineData(EntityType.Comment, false)]
        [InlineData(EntityType.Association, false)]
        public void ShouldMatchThePublicationModelTable(
            EntityType entityType,
            bool expectedIsVersioned)
        {
            // when
            bool actualIsVersioned = EntityTypeVersioning.IsVersioned(entityType);

            // then: pinned against the design §7.5.1 table rather than derived, because the
            // whole point of the lookup is that runtime shape is not a stable discriminator
            actualIsVersioned.Should().Be(expectedIsVersioned);
        }

        [Theory]
        [InlineData(EntityType.ContentItem, Scope.AllVersions)]
        [InlineData(EntityType.Link, Scope.AllVersions)]
        [InlineData(EntityType.Attachment, Scope.AllVersions)]
        [InlineData(EntityType.BibleReference, Scope.ThisVersionOnly)]
        [InlineData(EntityType.Tag, Scope.ThisVersionOnly)]
        [InlineData(EntityType.Reaction, Scope.ThisVersionOnly)]
        [InlineData(EntityType.Comment, Scope.ThisVersionOnly)]
        [InlineData(EntityType.Association, Scope.ThisVersionOnly)]
        public void ShouldResolveDefaultScopeFromThePublicationModel(
            EntityType entityType,
            Scope expectedScope)
        {
            // when
            Scope actualScope = EntityTypeVersioning.DefaultScopeFor(entityType);

            // then
            actualScope.Should().Be(expectedScope);
        }

        [Fact]
        public void ShouldThrowWhenAnEntityTypeHasNoDeclaredPublicationModel()
        {
            // given: an out-of-range member stands in for a type someone added to the enum
            // and forgot to declare here
            var undeclaredEntityType = (EntityType)int.MaxValue;

            // when
            Action askingForThePublicationModel = () =>
                EntityTypeVersioning.IsVersioned(undeclaredEntityType);

            // then: loudly, rather than silently picking a branch of the approval workflow
            askingForThePublicationModel.Should().Throw<NotSupportedException>();
        }
    }
}
