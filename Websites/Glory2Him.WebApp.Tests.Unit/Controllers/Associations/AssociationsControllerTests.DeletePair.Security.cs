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
        public void ShouldRequireAnAuthenticatedCallerOnDeletePair()
        {
            // given
            MethodInfo deletePairMethod = typeof(AssociationsController)
                .GetMethod(nameof(AssociationsController.DeleteAssociationPairAsync));

            string expectedRouteTemplate = "Pair";

            // when
            HttpDeleteAttribute actualHttpDeleteAttribute =
                deletePairMethod.GetCustomAttribute<HttpDeleteAttribute>(inherit: true);

            AuthorizeAttribute actualAuthorizeAttribute =
                deletePairMethod.GetCustomAttribute<AuthorizeAttribute>(inherit: true);

            AllowAnonymousAttribute actualAllowAnonymousAttribute =
                deletePairMethod.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true);

            // then
            actualHttpDeleteAttribute.Should().NotBeNull();
            actualHttpDeleteAttribute.Template.Should().Be(expectedRouteTemplate);
            actualAuthorizeAttribute.Should().NotBeNull();
            actualAllowAnonymousAttribute.Should().BeNull();
        }
    }
}
