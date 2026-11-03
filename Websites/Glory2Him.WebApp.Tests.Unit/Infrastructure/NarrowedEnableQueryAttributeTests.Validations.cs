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
        // $apply carries the same terms as $filter, has no validator of its own and is applied
        // first. OData maps every spelling to the same parsed option.
        [Theory]
        [InlineData("$apply=filter(version eq 1)")]
        [InlineData("apply=filter(version eq 1)")]
        [InlineData("$APPLY=filter(version eq 1)")]
        public void ShouldRefuseAnApplyInEverySpelling(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // $compute puts a navigated column in the body, and its validator checks nothing.
        [Theory]
        [InlineData("$compute=version as c")]
        [InlineData("compute=version as c")]
        [InlineData("$Compute=version as c")]
        public void ShouldRefuseAComputeInEverySpelling(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // $search is accepted and ignored today, so nothing a caller sends through it is needed.
        [Theory]
        [InlineData("$search=x")]
        [InlineData("search=x")]
        public void ShouldRefuseASearchInEverySpelling(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // $format is accepted today, and no read needs a caller to choose its format.
        [Theory]
        [InlineData("$format=json")]
        [InlineData("format=json")]
        public void ShouldRefuseAFormatInEverySpelling(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // The six are named by the rule itself, not by the attribute's AllowedQueryOptions,
        // whose default already includes $apply, $compute and $search. So an attribute that
        // allows every option still has every option outside the six refused.
        [Fact]
        public void ShouldRefuseAnOptionOutsideTheSixThatTheAttributeAllows()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute
            {
                AllowedQueryOptions = AllowedQueryOptions.All
            };

            string query = "$deltatoken=abc";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        [Fact]
        public void ShouldRefuseAFilterWithAnArithmeticOperator()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$filter=version mul 2 eq 4";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        [Fact]
        public void ShouldRefuseAnOrderByThatIsArithmetic()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$orderby=1 div version";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // OData parses the first key into OrderByClause.Expression and each later key into
        // ThenBy, so a key after an accepted one is refused as the first would be.
        [Theory]
        [InlineData("$orderby=name,1 div version")]
        [InlineData("orderby=name,1 div version")]
        [InlineData("$orderby=name,tolower(name)")]
        [InlineData("orderby=name,tolower(name)")]
        [InlineData("$orderby=name,parent/title")]
        [InlineData("orderby=name,parent/title")]
        [InlineData("$orderby=name,children/$count")]
        [InlineData("orderby=name,children/$count")]
        public void ShouldRefuseAnOrderByWhoseSecondKeyIsRefused(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }
    }
}
