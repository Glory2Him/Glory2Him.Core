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
using FluentAssertions;
using Glory2Him.WebApp.Infrastructure;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure
{
    public partial class NarrowedEnableQueryAttributeTests
    {
        // The narrowed attribute narrows and never widens: every setting it carries still
        // refuses what it refused before, because the base validation still runs.
        [Fact]
        public void ShouldRefuseAFilterTheAttributesOwnSettingsLeaveOut()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute
            {
                AllowedQueryOptions = AllowedQueryOptions.Supported & ~AllowedQueryOptions.Filter
            };

            string query = "$filter=" + ReactSearchShapedFilter;

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // The search the React app sends today must stay accepted, conversions and all.
        [Fact]
        public void ShouldAcceptTheSearchFilterShapeTheReactAppSends()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$filter=" + ReactSearchShapedFilter;

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().NotThrow();
        }
    }
}
