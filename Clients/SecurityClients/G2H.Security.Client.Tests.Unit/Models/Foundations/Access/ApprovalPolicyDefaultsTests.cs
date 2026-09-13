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
using G2H.Security.Client.Models.Foundations.Access;

namespace G2H.Security.Client.Tests.Unit.Models.Foundations.Access
{
    /// <summary>
    /// Pins the static system default directly, rather than only through the composed
    /// <c>AIReviewerPolicyVerdict</c> the AccessService tests exercise. That composition ANDs
    /// <see cref="ApprovalPolicy.IsAIReviewerAutomaticallyRequested"/> with
    /// <see cref="ApprovalPolicy.IsAIReviewerOffered"/> (also false under the system default), so
    /// it stays false no matter what this field is set to and cannot tell "this field is false"
    /// apart from "the offer switch masked whatever this field is" (§8.6.2.1, criterion 7).
    /// </summary>
    public class ApprovalPolicyDefaultsTests
    {
        // §8.6.2.1: reached only when no candidate row resolves at all, so both AI-reviewer
        // switches must fail closed here even though the entity's own CLR initialiser and the
        // column default both take IsAIReviewerAutomaticallyRequested as true (§8.6.2, "Which
        // default wins"). An unseeded environment offers no AI reviewer and asks for none
        // automatically either.
        [Fact]
        public void ShouldFailClosedOnBothAIReviewerSwitchesUnderTheSystemDefault()
        {
            // given
            string entityType = "SomeEntityType";

            // when
            ApprovalPolicy actualSystemDefault =
                ApprovalPolicyDefaults.SystemDefaultFor(entityType, contentType: null);

            // then
            actualSystemDefault.IsAIReviewerOffered.Should().BeFalse();
            actualSystemDefault.IsAIReviewerAutomaticallyRequested.Should().BeFalse();
        }
    }
}
