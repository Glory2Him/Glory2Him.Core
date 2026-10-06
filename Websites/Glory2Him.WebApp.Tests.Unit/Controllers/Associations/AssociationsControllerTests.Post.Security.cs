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

using System.Reflection;
using FluentAssertions;
using Glory2Him.WebApp.Controllers.Associations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Fact]
        public void ShouldRequireAnAuthenticatedCallerOnPost()
        {
            // given
            MethodInfo postMethod = typeof(AssociationsController)
                .GetMethod(nameof(AssociationsController.PostAssociationAsync));

            // when
            HttpPostAttribute actualHttpPostAttribute =
                postMethod.GetCustomAttribute<HttpPostAttribute>(inherit: true);

            AuthorizeAttribute actualAuthorizeAttribute =
                postMethod.GetCustomAttribute<AuthorizeAttribute>(inherit: true);

            AllowAnonymousAttribute actualAllowAnonymousAttribute =
                postMethod.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true);

            // then
            actualHttpPostAttribute.Should().NotBeNull();
            actualAuthorizeAttribute.Should().NotBeNull();
            actualAllowAnonymousAttribute.Should().BeNull();
        }
    }
}
