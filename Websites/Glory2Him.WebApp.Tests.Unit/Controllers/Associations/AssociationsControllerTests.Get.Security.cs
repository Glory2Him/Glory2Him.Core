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
        /// is decided beneath it (<c>AssociationsController.AssociationReads.md</c> §1 rule 3),
        /// and its query surface is <c>[EnableQuery]</c>'s with the host's page size
        /// (rule 2). The attribute is read here as declared, before
        /// <c>ODataPageSizeConvention</c> runs: a page size of the action's own reads back as
        /// anything but <c>0</c>, and the convention leaves it, so the host's would never reach
        /// this route.
        /// </summary>
        [Fact]
        public void ShouldServeAssociationsAnonymouslyWithAQuerySurface()
        {
            // given
            int expectedPageSize = 0;

            MethodInfo getMethod = typeof(AssociationsController)
                .GetMethod(nameof(AssociationsController.Get));

            // when
            HttpGetAttribute actualHttpGetAttribute =
                getMethod.GetCustomAttribute<HttpGetAttribute>(inherit: true);

            EnableQueryAttribute actualEnableQueryAttribute =
                getMethod.GetCustomAttribute<EnableQueryAttribute>(inherit: true);

            AllowAnonymousAttribute actualAllowAnonymousAttribute =
                getMethod.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true);

            // then
            actualHttpGetAttribute.Should().NotBeNull();
            actualHttpGetAttribute.Template.Should().BeNull();
            actualEnableQueryAttribute.Should().NotBeNull();
            actualEnableQueryAttribute.PageSize.Should().Be(expectedPageSize);
            actualAllowAnonymousAttribute.Should().NotBeNull();
        }
    }
}
