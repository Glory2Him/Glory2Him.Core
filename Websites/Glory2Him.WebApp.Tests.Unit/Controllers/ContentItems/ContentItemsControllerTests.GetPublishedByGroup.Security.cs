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
using Glory2Him.WebApp.Controllers.ContentItems;
using Microsoft.AspNetCore.Authorization;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ContentItems
{
    public partial class ContentItemsControllerTests
    {
        [Fact]
        public void GetPublishedByGroupShouldAllowAnonymous()
        {
            // Given
            var controllerType = typeof(ContentItemsController);
            var methodInfo = controllerType.GetMethod("GetPublishedContentItemByGroupIdAsync");
            Type attributeType = typeof(AllowAnonymousAttribute);

            // When
            var attribute = methodInfo?
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            // Then
            attribute.Should().NotBeNull();
        }

        [Fact]
        public void GetPublishedByGroupShouldNotHaveRoleAttributeWithRoles()
        {
            // Given
            var controllerType = typeof(ContentItemsController);
            var methodInfo = controllerType.GetMethod("GetPublishedContentItemByGroupIdAsync");
            Type attributeType = typeof(AuthorizeAttribute);

            // When
            var methodAttribute = methodInfo?
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            var controllerAttribute = controllerType
                .GetCustomAttributes(attributeType, inherit: true)
                .FirstOrDefault();

            var attribute = methodAttribute ?? controllerAttribute;

            // Then
            attribute.Should().BeNull();
        }
    }
}
