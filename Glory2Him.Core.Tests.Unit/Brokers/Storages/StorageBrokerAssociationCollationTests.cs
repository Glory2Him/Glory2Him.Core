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
using Glory2Him.Core.Models.Foundations.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Glory2Him.Core.Tests.Unit.Brokers.Storages
{
    /// <summary>
    /// A guard on the collation <c>Association.UserId</c> carries on its own column
    /// (§DOM4.6 rule 3), and on the migration that pins it.
    ///
    /// <para><b>Why only the model and the migration can prove it.</b> A column created with no
    /// collation of its own takes the catalogue's default, and today's catalogues default to the
    /// very collation pinned here, so a catalogue reports <c>SQL_Latin1_General_CP1_CI_AS</c> for
    /// the column before the pin as well as after it. Reading the catalogue cannot tell the pin
    /// from the default; the model and the migration's operations can.</para>
    /// </summary>
    public class StorageBrokerAssociationCollationTests
    {
        private const string CaseInsensitiveCollation = "SQL_Latin1_General_CP1_CI_AS";

        [Fact]
        public void ShouldPinTheAssociationUserIdToACaseInsensitiveCollation()
        {
            // given
            // THE DESIGN-TIME MODEL: the runtime model does not store a column's collation and
            // throws when asked for it.
            IEntityType associationEntityType =
                StorageBrokerModelSource.DesignTimeModel.FindEntityType(typeof(Association));

            // when
            string actualCollation = associationEntityType
                .FindProperty(nameof(Association.UserId))
                .GetCollation();

            // then
            actualCollation.Should().Be(CaseInsensitiveCollation);
        }
    }
}
