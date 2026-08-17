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
using System.Reflection;
using FluentAssertions;
using Glory2Him.WebApp.Controllers.ApprovalReviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalReviews
{
    public partial class ApprovalReviewsControllerTests
    {
        [Fact]
        public void ControllerShouldHaveApiControllerAttribute()
        {
            // Given
            var controllerType = typeof(ApprovalReviewsController);
            Type attributeType = typeof(ApiControllerAttribute);

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().NotBeNull();
        }

        [Fact]
        public void ControllerShouldHaveRouteAttributeWithApiTemplate()
        {
            // Given
            var controllerType = typeof(ApprovalReviewsController);
            Type attributeType = typeof(RouteAttribute);
            string expectedTemplate = "api/[controller]";

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault() as RouteAttribute;

            // Then
            attribute.Should().NotBeNull();
            attribute.Template.Should().Be(expectedTemplate);
        }

        [Fact]
        public void ControllerShouldNotAllowAnonymous()
        {
            // Given
            var controllerType = typeof(ApprovalReviewsController);
            Type attributeType = typeof(AllowAnonymousAttribute);

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().BeNull();
        }

        /// <summary>
        /// Approval reviews are §14.7 posture D — a verdict is never public, so unlike the tag
        /// exposer no action may opt out of authentication. This is the stronger form of the
        /// "exactly one decision" check: every action must carry <c>[Authorize]</c>, and none
        /// may carry <c>[AllowAnonymous]</c> at all.
        /// </summary>
        [Fact]
        public void EveryActionShouldRequireAuthentication()
        {
            // Given
            List<MethodInfo> actions = GetActions();

            // When
            List<string> unauthorizedActions = actions
                .Where(action =>
                    HasAttribute(action, typeof(AuthorizeAttribute)) is false
                        || HasAttribute(action, typeof(AllowAnonymousAttribute)))
                .Select(action => action.Name)
                .ToList();

            // Then
            unauthorizedActions.Should().BeEmpty();
        }

        [Fact]
        public void EveryActionShouldCarryExactlyOneAuthorizationDecision()
        {
            // Given
            List<MethodInfo> actions = GetActions();

            // When
            List<string> undecidedActions = actions
                .Where(action =>
                    HasAttribute(action, typeof(AuthorizeAttribute))
                        == HasAttribute(action, typeof(AllowAnonymousAttribute)))
                .Select(action => action.Name)
                .ToList();

            // Then
            undecidedActions.Should().BeEmpty();
        }

        [Fact]
        public void EveryActionShouldBeAccountedForBySecurityTests()
        {
            // Given
            List<string> expectedActions = new List<string>
            {
                nameof(ApprovalReviewsController.PostApprovalReviewAsync),
                nameof(ApprovalReviewsController.Get),
                nameof(ApprovalReviewsController.GetApprovalReviewByIdAsync),
                nameof(ApprovalReviewsController.PutApprovalReviewAsync),
                nameof(ApprovalReviewsController.DeleteApprovalReviewByIdAsync),
                nameof(ApprovalReviewsController.HardDeleteApprovalReviewByIdAsync),
                nameof(ApprovalReviewsController.DismissApprovalReviewAsync)
            };

            // When
            List<string> actualActions = GetActions()
                .Select(action => action.Name)
                .ToList();

            // Then
            actualActions.Should().BeEquivalentTo(expectedActions);
        }

        private static List<MethodInfo> GetActions() =>
            typeof(ApprovalReviewsController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.IsSpecialName is false)
                .ToList();

        private static bool HasAttribute(MethodInfo method, Type attributeType) =>
            method.GetCustomAttributes(attributeType, inherit: true).Any();
    }
}
