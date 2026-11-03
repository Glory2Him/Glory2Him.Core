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
using Glory2Him.WebApp.Infrastructure;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData;
using Microsoft.OData.UriParser;

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

        [Fact]
        public void ShouldRefuseAFilterNegatingAColumn()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$filter=-version eq 5";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        [Fact]
        public void ShouldRefuseAFilterCallingAFunctionOutsideTheFive()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$filter=indexof(name,'x') eq 0";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        [Fact]
        public void ShouldRefuseAFilterCallingNow()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$filter=now() eq 2026-01-01T00:00:00Z";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        [Fact]
        public void ShouldRefuseAnOrderByBuiltFromAFunction()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$orderby=tolower(name)";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        [Fact]
        public void ShouldRefuseAFilterThroughASingleValuedNavigation()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$filter=parent/title eq 'v'";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        [Fact]
        public void ShouldRefuseAFilterCountingACollectionValuedNavigation()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$filter=children/$count gt 0";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        [Fact]
        public void ShouldRefuseAnOrderByThroughASingleValuedNavigation()
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$orderby=parent/title";

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // The walk reads the parsed clause, so a refused term is refused whichever spelling
        // carries it.
        [Theory]
        [InlineData("filter=version mul 2 eq 4")]
        [InlineData("$FILTER=version mul 2 eq 4")]
        [InlineData("orderby=1 div version")]
        public void ShouldRefuseARefusedTermInEverySpellingOfItsOption(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // EF Core translates a column needle to a CHARINDEX that raises on a stored value over
        // 4000 characters; a literal needle raises on none.
        [Theory]
        [InlineData("$filter=contains(name,other)")]
        [InlineData("$filter=startswith(name,other)")]
        [InlineData("$filter=endswith(name,other)")]
        [InlineData("$filter=contains(name,tolower(other))")]
        public void ShouldRefuseAPatternFunctionWhoseNeedleIsNotALiteral(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        [Theory]
        [InlineData("$filter=children/any(x: x/note eq 'v')")]
        [InlineData("filter=children/any(x: x/note eq 'v')")]
        [InlineData("$filter=children/all(x: x/note ne 'v')")]
        [InlineData("filter=children/all(x: x/note ne 'v')")]
        public void ShouldRefuseAFilterWithALambdaOverACollectionValuedNavigation(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // Nested rows: a refused term beneath in, beneath an inserted conversion, beneath and,
        // beneath not, and a navigated column inside a function, through a type-cast segment and
        // through cast(). Each fails under a walk that does not recurse into that child.
        [Theory]
        [InlineData("(version mul 2) in (4)")]
        [InlineData("version mul 2 eq 5000000000")]
        [InlineData("(version mul 2 eq 4) and groupId eq null")]
        [InlineData("not (version mul 2 eq 4)")]
        [InlineData("contains(parent/title,'v')")]
        [InlineData("startswith(parent/title,'v')")]
        [InlineData("tolower(parent/title) eq 'v'")]
        [InlineData("parent/" + ParentTypeName + "/title eq 'v'")]
        [InlineData("contains(tolower(parent/code),'v')")]
        [InlineData("cast(parent," + ParentTypeName + ")/title eq 'v'")]
        public void ShouldRefuseEachNestedFilterExample(string filter)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();
            string query = "$filter=" + filter;

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // Later keys a walk of the first key alone would never see: lambdas, a type-cast segment
        // and cast() through the navigation, negation, in and not.
        [Theory]
        [InlineData("$orderby=name,children/any(x: x/note eq 'v')")]
        [InlineData("orderby=name,children/any(x: x/note eq 'v')")]
        [InlineData("$orderby=name,children/all(x: x/note ne 'v')")]
        [InlineData("orderby=name,children/all(x: x/note ne 'v')")]
        [InlineData("$orderby=name,parent/" + ParentTypeName + "/title")]
        [InlineData("orderby=name,parent/" + ParentTypeName + "/title")]
        [InlineData("$orderby=name,cast(parent," + ParentTypeName + ")/title")]
        [InlineData("orderby=name,cast(parent," + ParentTypeName + ")/title")]
        [InlineData("$orderby=name,-version")]
        [InlineData("orderby=name,-version")]
        [InlineData("$orderby=name,version in (1)")]
        [InlineData("orderby=name,version in (1)")]
        [InlineData("$orderby=name,not (version eq 1)")]
        [InlineData("orderby=name,not (version eq 1)")]
        public void ShouldRefuseEachLaterOrderByKeyExample(string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>();
        }

        // Every node kind outside the table, in every $filter position a bare attribute
        // accepts it in. The rows are generated, so a walk that refuses only what a named
        // example names fails here.
        [Theory]
        [MemberData(nameof(KeptNodeKindFilterRows))]
        public void ShouldRefuseEveryNodeKindOutsideTheTableInEveryFilterPosition(string row, string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>(because: row);
        }

        // Every QueryNodeKind is admitted, held by a kept $filter row, or listed with its reason.
        [Fact]
        public void ShouldPlaceEveryNodeKindOutsideTheTableForTheFilterPositions()
        {
            // given
            QueryNodeKind[] kindsOutsideTheTable =
                Enum.GetValues<QueryNodeKind>().Except(admittedNodeKinds).ToArray();

            // when
            QueryNodeKind[] unplacedKinds = kindsOutsideTheTable
                .Where(kind => keptNodeKindFilterRows.Value.Any(row => HoldsKind(row, kind)) is false)
                    .Where(kind => nodeKindsWithoutARow.Any(entry => entry.Kind == kind) is false)
                        .ToArray();

            string[] listedSeedRowsABareAttributeAccepts = nodeKindsWithoutARow
                .Where(entry => entry.Seed is not null)
                    .SelectMany(entry => GenerateFilterRows(entry.Seed))
                        .Where(row => TryValidateUnderABareAttribute(row.Query, out _))
                            .Select(row => row.Query)
                                .ToArray();

            // then
            unplacedKinds.Should().BeEmpty();
            listedSeedRowsABareAttributeAccepts.Should().BeEmpty();
        }

        // Every node kind except an own property and its $it source, as the first key and as
        // the second, in both spellings: a node at the key's root, or beneath one the $filter
        // table admits, as the seeds for the admitted kinds put it.
        [Theory]
        [MemberData(nameof(KeptNodeKindOrderByRows))]
        public void ShouldRefuseEveryNodeKindButAnOwnPropertyAsAnOrderByKey(string row, string query)
        {
            // given
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute();

            // when
            Action validateQueryAction = () =>
                ValidateQuery(narrowedEnableQueryAttribute, query);

            // then
            validateQueryAction.Should().Throw<ODataException>(because: row);
        }

        // Every QueryNodeKind but the own property and its source is held by a kept $orderby
        // row, or listed with its reason.
        [Fact]
        public void ShouldPlaceEveryNodeKindButAnOwnPropertyForTheOrderByKeys()
        {
            // given
            QueryNodeKind[] kindsRefusedAsAKey = Enum.GetValues<QueryNodeKind>()
                .Except(new[] { QueryNodeKind.SingleValuePropertyAccess, QueryNodeKind.ResourceRangeVariableReference })
                    .ToArray();

            // when
            QueryNodeKind[] unplacedKinds = kindsRefusedAsAKey
                .Where(kind => keptNodeKindOrderByRows.Value.Any(row => HoldsKind(row, kind)) is false)
                    .Where(kind => nodeKindsWithoutARow.Any(entry => entry.Kind == kind) is false)
                        .ToArray();

            string[] listedSeedRowsABareAttributeAccepts = nodeKindsWithoutARow
                .Where(entry => entry.Seed is not null)
                    .SelectMany(entry => GenerateOrderByRows(entry.Seed))
                        .Where(row => TryValidateUnderABareAttribute(row.Query, out _))
                            .Select(row => row.Query)
                                .ToArray();

            // then
            unplacedKinds.Should().BeEmpty();
            listedSeedRowsABareAttributeAccepts.Should().BeEmpty();
        }
    }
}
