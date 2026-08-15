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
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Controllers.Tags;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Tags
{
    public partial class TagsControllerTests
    {
        [Fact]
        public void ApproveShouldHaveRoleAttributeWithRoles()
        {
            // Given
            var controllerType = typeof(TagsController);
            var methodInfo = controllerType.GetMethod("ApproveTagAsync");
            Type attributeType = typeof(AuthorizeAttribute);
            string attributeProperty = "Roles";

            List<string> expectedAttributeValues = new List<string>
            {
                Roles.Admin,
                Roles.Publisher,
                Roles.TagPublisher
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
        [Fact]
        public void ApproveShouldNotAllowAnonymous()
        {
            // Given
            var controllerType = typeof(TagsController);
            var methodInfo = controllerType.GetMethod("ApproveTagAsync");
            Type attributeType = typeof(AllowAnonymousAttribute);

            // When
            var attribute = methodInfo?
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().BeNull();
        }
    }
}
