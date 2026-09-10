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
using Glory2Him.WebApp.Controllers.AIReviewers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.AIReviewers
{
    public partial class AIReviewersControllerTests
    {
        [Fact]
        public void ControllerShouldHaveApiControllerAttribute()
        {
            // Given
            var controllerType = typeof(AIReviewersController);
            Type attributeType = typeof(ApiControllerAttribute);

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().NotBeNull();
        }

        /// <summary>
        /// The template is what makes the resource its own. <c>api/[controller]</c> against
        /// <c>AIReviewersController</c> yields <c>api/AIReviewers</c>, which is the whole point of
        /// the split — these three actions used to hang off <c>api/Approvals/.../AIReviewer</c>,
        /// a sub-resource of the approval round. Pinned as the literal template rather than the
        /// expanded prefix, because that is the form every sibling exposer carries and the form a
        /// rename must keep working.
        /// </summary>
        [Fact]
        public void ControllerShouldHaveRouteAttributeWithApiTemplate()
        {
            // Given
            var controllerType = typeof(AIReviewersController);
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

        /// <summary>
        /// Every action is keyed on the ENTITY, never on the assignment's own id: a moderation
        /// panel knows the item it is showing and has never been handed an <c>AIReviewerAssignment</c>
        /// id. The three verbs therefore share one template, which is also what lets the withdrawal
        /// be idempotent — there is no id in the route that could stop existing.
        /// </summary>
        [Theory]
        [InlineData(nameof(AIReviewersController.GetAIReviewerAsync), typeof(HttpGetAttribute))]
        [InlineData(nameof(AIReviewersController.PostAIReviewerAsync), typeof(HttpPostAttribute))]
        [InlineData(nameof(AIReviewersController.DeleteAIReviewerAsync), typeof(HttpDeleteAttribute))]
        public void ActionShouldBeKeyedOnTheEntity(string actionName, Type verbAttributeType)
        {
            // Given
            MethodInfo methodInfo = typeof(AIReviewersController).GetMethod(actionName);
            string expectedTemplate = "{entityType}/{entityId}";

            // When
            var attribute = methodInfo?
                .GetCustomAttributes(verbAttributeType, inherit: true)
                .FirstOrDefault() as IRouteTemplateProvider;

            // Then
            attribute.Should().NotBeNull();
            attribute.Template.Should().Be(expectedTemplate);
        }

        /// <summary>
        /// §14.7 posture D — this surface names resolved policy and reports what a round's
        /// reviewers are doing, so unlike the tag exposer no action here may opt out of
        /// authentication.
        /// </summary>
        [Fact]
        public void ControllerShouldNotAllowAnonymous()
        {
            // Given
            var controllerType = typeof(AIReviewersController);
            Type attributeType = typeof(AllowAnonymousAttribute);

            // When
            var attribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().BeNull();
        }

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

        /// <summary>
        /// The inventory. A fourth action added to this exposer fails here first, which is what
        /// forces it into the two theories below rather than shipping ungated.
        /// </summary>
        [Fact]
        public void EveryActionShouldBeAccountedForBySecurityTests()
        {
            // Given
            List<string> expectedActions = new List<string>
            {
                nameof(AIReviewersController.GetAIReviewerAsync),
                nameof(AIReviewersController.PostAIReviewerAsync),
                nameof(AIReviewersController.DeleteAIReviewerAsync)
            };

            // When
            List<string> actualActions = GetActions()
                .Select(action => action.Name)
                .ToList();

            // Then
            actualActions.Should().BeEquivalentTo(expectedActions);
        }

        /// <summary>
        /// <b>The empty expected list is the assertion, not a placeholder.</b> §7.9 rule 2 admits
        /// the whole review tier — <c>Administrators</c>, the <c>Publishers</c> tier and the
        /// <c>Reviewers</c> tier — each matched by SUFFIX, so any role ending <c>-Publishers</c>
        /// or <c>-Reviewers</c> qualifies too, including the content-type-scoped tier of §18.6
        /// rule 5. These routes are generic over <c>EntityType</c> as well, so no fixed
        /// <c>Roles = ...</c> list can express the set, and any partial list would lock out the
        /// content-type tier today and every entity type added later. The tier decision therefore
        /// lives in the orchestration alone (§14.6), and this pins the attribute to the coarse
        /// authenticated-only gate so a future fixed list has to be argued for rather than
        /// slipped in.
        /// </summary>
        [Theory]
        [InlineData(nameof(AIReviewersController.GetAIReviewerAsync))]
        [InlineData(nameof(AIReviewersController.PostAIReviewerAsync))]
        [InlineData(nameof(AIReviewersController.DeleteAIReviewerAsync))]
        public void ActionShouldCarryAuthorizeWithNoFixedRoleList(string actionName)
        {
            // Given
            var controllerType = typeof(AIReviewersController);
            MethodInfo methodInfo = controllerType.GetMethod(actionName);
            Type attributeType = typeof(AuthorizeAttribute);
            string attributeProperty = "Roles";

            List<string> expectedAttributeValues = new List<string>
            {
            };

            // When
            var methodAttribute = methodInfo?
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            var controllerAttribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            var attribute = methodAttribute ?? controllerAttribute;

            // Then
            attribute.Should().NotBeNull();

            var actualAttributeValue = attributeType
                .GetProperty(attributeProperty)?
                .GetValue(attribute) as string ?? string.Empty;

            var actualAttributeValues = actualAttributeValue?
                .Split(',')
                .Select(role => role.Trim())
                .Where(role => !string.IsNullOrEmpty(role))
                .ToList();

            actualAttributeValues.Should().BeEquivalentTo(expectedAttributeValues);
        }

        [Theory]
        [InlineData(nameof(AIReviewersController.GetAIReviewerAsync))]
        [InlineData(nameof(AIReviewersController.PostAIReviewerAsync))]
        [InlineData(nameof(AIReviewersController.DeleteAIReviewerAsync))]
        public void ActionShouldNotAllowAnonymous(string actionName)
        {
            // Given
            var controllerType = typeof(AIReviewersController);
            MethodInfo methodInfo = controllerType.GetMethod(actionName);
            Type attributeType = typeof(AllowAnonymousAttribute);

            // When
            var attribute = methodInfo?
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().BeNull();
        }

        private static List<MethodInfo> GetActions() =>
            typeof(AIReviewersController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.IsSpecialName is false)
                .ToList();

        private static bool HasAttribute(MethodInfo method, Type attributeType) =>
            method.GetCustomAttributes(attributeType, inherit: true).Any();
    }
}
