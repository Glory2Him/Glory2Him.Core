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
        /// Criterion 3. The route is open to every caller because its counts are the same for
        /// every caller (§ARC16.8, <i>Anonymity</i>), and its query surface is closed rather than
        /// allow-listed (§ARC16.8, <i>The route</i>). A route that grew an <c>[Authorize]</c>
        /// would stop answering signed-out readers, and one that grew an <c>[EnableQuery]</c>
        /// would keep answering — so neither is visible to a behavioural test alone.
        /// </summary>
        [Fact]
        public void ShouldServeReactionSummariesAnonymouslyWithNoQuerySurface()
        {
            // given
            string expectedRouteTemplate = "ReactionSummaries";

            MethodInfo getReactionSummariesMethod = typeof(AssociationsController)
                .GetMethod(nameof(AssociationsController.GetReactionSummariesAsync));

            // when
            HttpGetAttribute actualHttpGetAttribute =
                getReactionSummariesMethod.GetCustomAttribute<HttpGetAttribute>(inherit: true);

            AllowAnonymousAttribute actualAllowAnonymousAttribute =
                getReactionSummariesMethod.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true);

            EnableQueryAttribute actualEnableQueryAttribute =
                getReactionSummariesMethod.GetCustomAttribute<EnableQueryAttribute>(inherit: true);

            // then
            actualHttpGetAttribute.Should().NotBeNull();
            actualHttpGetAttribute.Template.Should().Be(expectedRouteTemplate);
            actualAllowAnonymousAttribute.Should().NotBeNull();
            actualEnableQueryAttribute.Should().BeNull();
        }
    }
}
