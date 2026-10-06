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
using FluentAssertions;
using Glory2Him.Core.Models.Orchestrations.Associations;

namespace Glory2Him.Core.Tests.Unit.Models.Orchestrations.Associations
{
    public class AssociationSuggestionStatusTests
    {
        [Fact]
        public void ShouldKeepEverySuggestionStatusOnItsWireNumber()
        {
            // given: the wire carries the number, not the name — the host registers no
            // JsonStringEnumConverter, and the React app mirrors these values (#731) — so a new
            // member is appended and none moves (Likes.md, Risks). Each member is named here as a
            // string rather than through the enum, so a member that is missing, renamed or
            // renumbered reds this test rather than a reader's card.
            var expectedWireNumbers = new Dictionary<string, int>
            {
                ["Created"] = 0,
                ["AlreadyPending"] = 1,
                ["AlreadyApproved"] = 2,
                ["OverlapsExisting"] = 3,
                ["Restored"] = 4,
                ["Repointed"] = 5,
            };

            // when
            Dictionary<string, int> actualWireNumbers =
                Enum.GetValues<AssociationSuggestionStatus>()
                    .ToDictionary(
                        suggestionStatus => suggestionStatus.ToString(),
                        suggestionStatus => (int)suggestionStatus);

            // then
            actualWireNumbers.Should().BeEquivalentTo(expectedWireNumbers);
        }
    }
}
