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
using static Glory2Him.WebApp.Tests.Unit.Infrastructure.HostQueryValidation;

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

        // One row per term the allowlist names. Each passes under a bare attribute too: what
        // they pin is that the walk is no narrower than the allowlist.
        [Theory]
        [InlineData("version eq 1")]
        [InlineData("version ne 1")]
        [InlineData("version gt 1")]
        [InlineData("version ge 1")]
        [InlineData("version lt 1")]
        [InlineData("version le 1")]
        [InlineData("kinds has 'Alpha'")]
        [InlineData("name in ('a','b')")]
        [InlineData("version eq 1 and version ne 2")]
        [InlineData("version eq 1 or version eq 2")]
        [InlineData("not (version eq 1)")]
        [InlineData("groupId eq null")]
        [InlineData("groupId ne null")]
        [InlineData("kinds eq 'Beta'")]
        [InlineData("contains(name,'x')")]
        [InlineData("startswith(name,'x')")]
        [InlineData("endswith(name,'x')")]
        [InlineData("contains(tolower(name),'x')")]
        [InlineData("startswith(tolower(name),'x')")]
        [InlineData("endswith(tolower(name),'x')")]
        [InlineData("contains(toupper(name),'X')")]
        [InlineData("startswith(toupper(name),'X')")]
        [InlineData("endswith(toupper(name),'X')")]
        [InlineData("tolower(name) eq 'x'")]
        [InlineData("toupper(name) eq 'X'")]
        [InlineData("contains(fixed,'x')")]
        [InlineData("startswith(fixed,'x')")]
        [InlineData("endswith(fixed,'x')")]
        [InlineData("contains(tolower(fixed),'x')")]
        [InlineData("startswith(tolower(fixed),'x')")]
        [InlineData("endswith(tolower(fixed),'x')")]
        [InlineData("contains(toupper(fixed),'X')")]
        [InlineData("startswith(toupper(fixed),'X')")]
        [InlineData("endswith(toupper(fixed),'X')")]
        [InlineData("tolower(fixed) eq 'x'")]
        [InlineData("toupper(fixed) eq 'X'")]
        [InlineData("version eq 5000000000")]
        [InlineData("version eq 1.5")]
        [InlineData("score eq 1.5")]
        public void ShouldAcceptAFilterHoldingOneAllowlistedTerm(string filter)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$filter=" + filter;

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().NotThrow();
        }

        [Fact]
        public void ShouldAcceptAnOrderByNamingTheEntitysOwnProperties()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$orderby=name,version desc";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().NotThrow();
        }

        // The four options the walk does not read keep OData's own rules and the attribute's
        // own settings, so each still answers as before.
        [Theory]
        [InlineData("$select=name,version")]
        [InlineData("$top=5")]
        [InlineData("$skip=1")]
        [InlineData("$count=true")]
        public void ShouldAcceptEachOtherOptionOfTheSix(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().NotThrow();
        }

        // Only a nested option is refused in $select. A wildcard, with or without the items it
        // subsumes, carries none, so it stays accepted.
        [Theory]
        [InlineData("$select=*")]
        [InlineData("$select=*,name")]
        [InlineData("select=*,labels")]
        public void ShouldAcceptASelectWithAWildcardCarryingNoNestedOption(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().NotThrow();
        }
    }
}
