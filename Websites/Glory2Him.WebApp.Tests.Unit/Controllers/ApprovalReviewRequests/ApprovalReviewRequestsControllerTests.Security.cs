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
using Glory2Him.WebApp.Controllers.ApprovalReviewRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestsControllerTests
    {
        /// <summary>
        /// Authenticated, but with NO fixed role list. Who may withdraw is decided per approval
        /// against the entity's own review tier (§7.9 rule 5), which an attribute cannot express —
        /// a role list here would either lock out a legitimate scoped reviewer or admit somebody
        /// the entity's tier does not cover.
        /// </summary>
        [Fact]
        public void DeleteShouldCarryAuthorizeWithNoFixedRoleList()
        {
            // given
            var controllerType = typeof(ApprovalReviewRequestsController);

            var methodInfo =
                controllerType.GetMethod("DeleteApprovalReviewRequestByIdAsync");

            // when
            var attribute =
                (methodInfo?.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                    .FirstOrDefault()
                ?? controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                    .FirstOrDefault()) as AuthorizeAttribute;

            // then
            attribute.Should().NotBeNull();

            string.IsNullOrWhiteSpace(attribute!.Roles).Should().BeTrue(
                because: "eligibility is per entity and per approval, not per attribute");
        }

        /// <summary>
        /// A withdrawal removes the record of who was asked. An anonymous route to it would let
        /// anybody erase that, so the absence of AllowAnonymous is worth pinning rather than
        /// assuming.
        /// </summary>
        [Fact]
        public void DeleteShouldNotAllowAnonymous()
        {
            // given
            var controllerType = typeof(ApprovalReviewRequestsController);

            var methodInfo =
                controllerType.GetMethod("DeleteApprovalReviewRequestByIdAsync");

            // when
            var attribute = methodInfo?
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
                .FirstOrDefault();

            // then
            attribute.Should().BeNull();
        }

        /// <summary>
        /// The route is keyed on the request row's id today. Pinned because issue #355 proposes
        /// re-keying it on the approval and the person, which is a breaking change to any caller
        /// and should not happen by accident.
        /// </summary>
        [Fact]
        public void DeleteShouldBeKeyedOnTheRequestId()
        {
            // given
            var methodInfo = typeof(ApprovalReviewRequestsController)
                .GetMethod("DeleteApprovalReviewRequestByIdAsync");

            // when
            var attribute = methodInfo?
                .GetCustomAttributes(typeof(HttpDeleteAttribute), inherit: true)
                .FirstOrDefault() as HttpDeleteAttribute;

            // then
            attribute.Should().NotBeNull();
            attribute!.Template.Should().Be("{approvalReviewRequestId}");
        }
    }
}
