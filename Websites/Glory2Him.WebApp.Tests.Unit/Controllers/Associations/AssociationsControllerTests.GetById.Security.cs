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
using Microsoft.AspNetCore.OData.Query;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        /// <summary>
        /// Criterion 2. The route is open to every caller, because which rows a caller is answered
        /// is decided beneath it, and every other caller is answered not-found rather than refused
        /// (<c>AssociationsController.AssociationReads.md</c> §2). It has no query surface: it
        /// answers one row. A route that grew an <c>[Authorize]</c> would stop answering
        /// signed-out readers, and one that grew an <c>[EnableQuery]</c> would keep answering, so
        /// neither is visible to a behavioural test alone.
        /// </summary>
        [Fact]
        public void ShouldServeAnAssociationByIdAnonymouslyWithNoQuerySurface()
        {
            // given
            string expectedRouteTemplate = "{associationId}";

            MethodInfo getAssociationByIdMethod = typeof(AssociationsController)
                .GetMethod(nameof(AssociationsController.GetAssociationByIdAsync));

            // when
            HttpGetAttribute actualHttpGetAttribute =
                getAssociationByIdMethod.GetCustomAttribute<HttpGetAttribute>(inherit: true);

            AllowAnonymousAttribute actualAllowAnonymousAttribute =
                getAssociationByIdMethod.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true);

            EnableQueryAttribute actualEnableQueryAttribute =
                getAssociationByIdMethod.GetCustomAttribute<EnableQueryAttribute>(inherit: true);

            // then
            actualHttpGetAttribute.Should().NotBeNull();
            actualHttpGetAttribute.Template.Should().Be(expectedRouteTemplate);
            actualAllowAnonymousAttribute.Should().NotBeNull();
            actualEnableQueryAttribute.Should().BeNull();
        }
    }
}
