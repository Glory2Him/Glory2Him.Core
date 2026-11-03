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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.UriParser;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure
{
    /// <summary>
    /// The generated rows. Each holds one seed expression in one position of a $filter or as an
    /// $orderby key, and a row is kept only when a bare attribute's validation, on a fresh parse
    /// in the host's query configuration, accepts it: a query the parser or the base validation
    /// refuses cannot fail first. What is left is asserted refused by the narrowed attribute.
    /// Each family is completeness-checked against the enum or list it covers, so a value added
    /// in a later OData version fails here until it is placed.
    /// </summary>
    public partial class NarrowedEnableQueryAttributeTests
    {
        private const string KeyLiteral = "11111111-1111-1111-1111-111111111111";

        private static readonly QueryNodeKind[] admittedNodeKinds =
        {
            QueryNodeKind.Constant,
            QueryNodeKind.CollectionConstant,
            QueryNodeKind.Convert,
            QueryNodeKind.BinaryOperator,
            QueryNodeKind.UnaryOperator,
            QueryNodeKind.In,
            QueryNodeKind.SingleValueFunctionCall,
            QueryNodeKind.SingleValuePropertyAccess,
            QueryNodeKind.ResourceRangeVariableReference
        };

        private static readonly BinaryOperatorKind[] admittedBinaryOperators =
        {
            BinaryOperatorKind.Equal,
            BinaryOperatorKind.NotEqual,
            BinaryOperatorKind.GreaterThan,
            BinaryOperatorKind.GreaterThanOrEqual,
            BinaryOperatorKind.LessThan,
            BinaryOperatorKind.LessThanOrEqual,
            BinaryOperatorKind.And,
            BinaryOperatorKind.Or,
            BinaryOperatorKind.Has
        };

        private static readonly UnaryOperatorKind[] admittedUnaryOperators = { UnaryOperatorKind.Not };

        private static readonly string[] fiveAdmittedFunctionNames =
            { "contains", "startswith", "endswith", "tolower", "toupper" };

        // Every name in Microsoft.OData.Core 8.4.0's internal BuiltInUriFunctions table, which
        // holds 32, and cast and isof, which bind outside it. A test below fails on any other
        // version of Microsoft.OData.Core, so an upgrade has to revisit this list.
        private static readonly string[] pinnedFunctionNames =
        {
            "ceiling", "concat", "contains", "date", "day", "endswith", "floor",
            "fractionalseconds", "geo.distance", "geo.intersects", "geo.length", "hour",
            "indexof", "length", "matchesPattern", "maxdatetime", "mindatetime", "minute",
            "month", "now", "replace", "round", "second", "startswith", "substring", "time",
            "tolower", "totaloffsetminutes", "totalseconds", "toupper", "trim", "year",
            "cast", "isof"
        };

        // One seed per node kind outside the table, and more where a kind reaches a column
        // another way: through the navigation, its required column, a type-cast segment, cast(),
        // $it, and an alias the parser resolves to a navigation.
        private static readonly QuerySeed[] nodeKindSeeds =
        {
            new("SingleNavigationNode", "parent/title", Literal: "'v'"),
            new("SingleNavigationNode, a Guid column", "parent/id", Literal: KeyLiteral),
            new("SingleNavigationNode, the required column", "parent/code", Literal: "'v'"),
            new("SingleResourceCast", "parent/" + ParentTypeName + "/title", Literal: "'v'"),
            new("SingleResourceCast, the required column", "parent/" + ParentTypeName + "/code", Literal: "'v'"),
            new("SingleResourceCast on $it", ItemTypeName + "/name", Literal: "'v'"),
            new("SingleResourceFunctionCall", "cast(parent," + ParentTypeName + ")/title", Literal: "'v'"),
            new("SingleResourceFunctionCall, the required column", "cast(parent," + ParentTypeName + ")/code", Literal: "'v'"),
            new("SingleResourceFunctionCall on $it", "cast($it," + ItemTypeName + ")/name", Literal: "'v'"),
            new("isof over the navigation", "isof(parent," + ParentTypeName + ")", Literal: null),
            new("isof over $it", "isof(" + ItemTypeName + ")", Literal: null),
            new("Count", "children/$count", Literal: "4"),
            new("Count with a nested filter", "children/$count($filter=note eq 'v')", Literal: "4"),
            new("Any", "children/any(x: x/note eq 'v')", Literal: null),
            new("Any without a body", "children/any()", Literal: null),
            new("All", "children/all(x: x/note ne 'v')", Literal: null),
            new("Any reaching $it", "children/any(x: x/note eq $it/name)", Literal: null),
            new("CollectionResourceCast", "children/" + ChildTypeName + "/any(x: x/note eq 'v')", Literal: null),
            new("CollectionResourceCast, counted", "children/" + ChildTypeName + "/$count", Literal: "4"),
            new("$it to the navigation", "$it/parent/title", Literal: "'v'"),
            new("an alias naming the navigation", "@p", Literal: "'v'", Suffix: "&@p=parent/title")
        };

        // A25's seeds for the kinds the $filter table admits, each refused as an $orderby key.
        private static readonly QuerySeed[] admittedNodeKindSeeds =
        {
            new("BinaryOperator, a comparison", "version eq 1", Literal: null),
            new("BinaryOperator, arithmetic", "1 div version", Literal: "4"),
            new("Convert, over arithmetic", "version add 5000000000", Literal: "4"),
            new("Constant, a string", "'abc'", Literal: "'v'"),
            new("Constant, a number", "1", Literal: "4"),
            new("UnaryOperator, negation", "-version", Literal: "4"),
            new("UnaryOperator, not", "not (version eq 1)", Literal: null),
            new("In, with its CollectionConstant", "version in (1)", Literal: null),
            new("SingleValueFunctionCall, a case function", "tolower(name)", Literal: "'v'"),
            new("SingleValueFunctionCall, a pattern function", "contains(name,'a')", Literal: null)
        };

        // The kinds no generated row holds. An entry with a seed is one the parser or the base
        // validation refuses, and the completeness tests assert a bare attribute refuses that
        // seed in every position. An entry without one is a kind the test model cannot hold.
        private static readonly NodeKindWithoutARow[] nodeKindsWithoutARow =
        {
            new(QueryNodeKind.None, Seed: null,
                "The enum's zero value: no node reports it."),
            new(QueryNodeKind.NonResourceRangeVariableReference, Seed: null,
                "A range variable over a collection of primitives or enums; the model holds none."),
            new(QueryNodeKind.CollectionPropertyAccess, Seed: null,
                "A collection of primitives or enums; the model holds none."),
            new(QueryNodeKind.SingleValueOpenPropertyAccess,
                new QuerySeed("an undeclared property", "unknownproperty", Literal: "'v'"),
                "An open type's property; the model's types are closed, so the parser refuses it."),
            new(QueryNodeKind.CollectionOpenPropertyAccess, Seed: null,
                "An open type's collection property; the model's types are closed."),
            new(QueryNodeKind.CollectionFunctionCall, Seed: null,
                "A function returning a collection of primitives; no built-in does and the model declares none."),
            new(QueryNodeKind.CollectionResourceFunctionCall, Seed: null,
                "A function returning entities; no built-in does and the model declares none."),
            new(QueryNodeKind.NamedFunctionParameter, Seed: null,
                "A named parameter of a declared function; the model declares none."),
            new(QueryNodeKind.ParameterAlias, Seed: null,
                "The parser resolves every alias before validation: one naming a term arrives as that "
                    + "term, as the alias seed above shows, and one with no value arrives as null."),
            new(QueryNodeKind.EntitySet,
                new QuerySeed("$root to an entity set", "$root/QueryItem(" + KeyLiteral + ")/name", Literal: "'v'"),
                "Reached only through $root, which the parser does not implement here."),
            new(QueryNodeKind.KeyLookup,
                new QuerySeed("$root to a key", "$root/QueryItem(" + KeyLiteral + ")/name", Literal: "'v'"),
                "Reached only through $root, which the parser does not implement here."),
            new(QueryNodeKind.RootPath,
                new QuerySeed("$root", "$root/QueryItem(" + KeyLiteral + ")/name", Literal: "'v'"),
                "The parser does not implement $root here."),
            new(QueryNodeKind.SearchTerm, Seed: null,
                "Arises only in $search, which a $filter or an $orderby cannot hold."),
            new(QueryNodeKind.CollectionComplexNode, Seed: null,
                "A collection of a complex type; the model holds none."),
            new(QueryNodeKind.SingleComplexNode, Seed: null,
                "A complex property; the model holds none."),
            new(QueryNodeKind.SingleValueCast,
                new QuerySeed("a primitive type-cast segment", "name/Edm.String", Literal: "'v'"),
                "The parser does not implement a type-cast segment on a primitive."),
            new(QueryNodeKind.CollectionPropertyNode, Seed: null,
                "Arises only in $apply's aggregation, which a $filter or an $orderby cannot hold."),
            new(QueryNodeKind.AggregatedCollectionPropertyNode, Seed: null,
                "Arises only in $apply's aggregation, which a $filter or an $orderby cannot hold."),
            new(QueryNodeKind.CustomQueryOption, Seed: null,
                "A query option, never a node of a parsed $filter or $orderby.")
        };

        // One seed for each operator outside the table.
        private static readonly QuerySeed[] operatorSeeds =
        {
            new("Add", "version add 1", Literal: "4"),
            new("Subtract", "version sub 1", Literal: "4"),
            new("Multiply", "version mul 2", Literal: "4"),
            new("Divide", "version div 2", Literal: "4"),
            new("Modulo", "version mod 2", Literal: "4"),
            new("Negate", "-version", Literal: "4")
        };

        // One call to each pinned name outside the five, with arguments it binds to. The geo
        // functions bind with geography literals, and maxdatetime, mindatetime, replace,
        // totaloffsetminutes and totalseconds pass validation even though applying them fails
        // later; so every name gives a kept row.
        private static readonly QuerySeed[] functionSeeds =
        {
            new("ceiling", "ceiling(score)", Literal: "1.5"),
            new("concat", "concat(name,'x')", Literal: "'v'"),
            new("date", "date(2026-01-01T00:00:00Z)", Literal: "2026-01-01"),
            new("day", "day(2026-01-01T00:00:00Z)", Literal: "4"),
            new("floor", "floor(score)", Literal: "1.5"),
            new("fractionalseconds", "fractionalseconds(2026-01-01T00:00:00Z)", Literal: "1.5"),
            new("geo.distance", "geo.distance(geography'POINT(0 0)',geography'POINT(1 1)')", Literal: "1.5"),
            new("geo.intersects", "geo.intersects(geography'POINT(0 0)',geography'POLYGON((0 0,1 0,1 1,0 0))')", Literal: null),
            new("geo.length", "geo.length(geography'LINESTRING(0 0,1 1)')", Literal: "1.5"),
            new("hour", "hour(2026-01-01T00:00:00Z)", Literal: "4"),
            new("indexof", "indexof(name,'x')", Literal: "4"),
            new("length", "length(name)", Literal: "4"),
            new("matchesPattern", "matchesPattern(name,'x')", Literal: null),
            new("maxdatetime", "maxdatetime()", Literal: "2026-01-01T00:00:00Z"),
            new("mindatetime", "mindatetime()", Literal: "2026-01-01T00:00:00Z"),
            new("minute", "minute(2026-01-01T00:00:00Z)", Literal: "4"),
            new("month", "month(2026-01-01T00:00:00Z)", Literal: "4"),
            new("now", "now()", Literal: "2026-01-01T00:00:00Z"),
            new("replace", "replace(name,'x','y')", Literal: "'v'"),
            new("round", "round(score)", Literal: "1.5"),
            new("second", "second(2026-01-01T00:00:00Z)", Literal: "4"),
            new("substring", "substring(name,1)", Literal: "'v'"),
            new("time", "time(2026-01-01T00:00:00Z)", Literal: "12:00:00"),
            new("totaloffsetminutes", "totaloffsetminutes(2026-01-01T00:00:00Z)", Literal: "4"),
            new("totalseconds", "totalseconds(duration'P1D')", Literal: "1.5"),
            new("trim", "trim(name)", Literal: "'v'"),
            new("year", "year(2026-01-01T00:00:00Z)", Literal: "4"),
            new("cast", "cast(version,Edm.Int64)", Literal: "4"),
            new("isof", "isof(version,Edm.Int64)", Literal: null)
        };

        // The pinned names that cannot bind on the test model, each with its reason. On 8.4.0
        // every name outside the five binds, so none is listed.
        private static readonly (string Name, string Reason)[] functionNamesThatCannotBind =
            Array.Empty<(string Name, string Reason)>();

        private static readonly Lazy<KeptRow[]> keptNodeKindFilterRows =
            new(() => KeepRowsABareAttributeAccepts(nodeKindSeeds.SelectMany(GenerateFilterRows)));

        private static readonly Lazy<KeptRow[]> keptNodeKindOrderByRows =
            new(() => KeepRowsABareAttributeAccepts(
                nodeKindSeeds.Concat(admittedNodeKindSeeds).SelectMany(GenerateOrderByRows)));

        private static readonly Lazy<KeptRow[]> keptOperatorFilterRows =
            new(() => KeepRowsABareAttributeAccepts(operatorSeeds.SelectMany(GenerateFilterRows)));

        private static readonly Lazy<KeptRow[]> keptOperatorOrderByRows =
            new(() => KeepRowsABareAttributeAccepts(operatorSeeds.SelectMany(GenerateOrderByRows)));

        private static readonly Lazy<KeptRow[]> keptFunctionFilterRows =
            new(() => KeepRowsABareAttributeAccepts(functionSeeds.SelectMany(GenerateFilterRows)));

        private static readonly Lazy<KeptRow[]> keptFunctionOrderByRows =
            new(() => KeepRowsABareAttributeAccepts(functionSeeds.SelectMany(GenerateOrderByRows)));

        public static TheoryData<string, string> KeptNodeKindFilterRows() =>
            ToTheoryData(keptNodeKindFilterRows.Value);

        public static TheoryData<string, string> KeptNodeKindOrderByRows() =>
            ToTheoryData(keptNodeKindOrderByRows.Value);

        public static TheoryData<string, string> KeptOperatorRows() =>
            ToTheoryData(keptOperatorFilterRows.Value.Concat(keptOperatorOrderByRows.Value));

        public static TheoryData<string, string> KeptFunctionRows() =>
            ToTheoryData(keptFunctionFilterRows.Value.Concat(keptFunctionOrderByRows.Value));

        // A24's positions. P1 is the whole $filter, or the seed beneath a comparison with a
        // literal of its type; P2 the comparison's right operand; P3 each side of and and or;
        // P4 beneath not; P5 beneath an inserted conversion, to Int64 and lifted to nullable;
        // P6 the left of in; P7 the first argument of each pattern function; P8 the argument of
        // each case function.
        private static IEnumerable<GeneratedRow> GenerateFilterRows(QuerySeed seed)
        {
            string comparison = seed.Literal is null
                ? seed.Expression
                : seed.Expression + " eq " + seed.Literal;

            string literal = seed.Literal ?? "true";

            var positions = new (string Position, string Filter)[]
            {
                ("P1", comparison),
                ("P2", literal + " eq " + seed.Expression),
                ("P3 and, left", "(" + comparison + ") and version eq 1"),
                ("P3 and, right", "version eq 1 and (" + comparison + ")"),
                ("P3 or, left", "(" + comparison + ") or version eq 1"),
                ("P3 or, right", "version eq 1 or (" + comparison + ")"),
                ("P4 not", "not (" + comparison + ")"),
                ("P5 to Int64", seed.Expression + " eq 5000000000"),
                ("P5 lifted", "(" + comparison + ") and groupId eq null"),
                ("P6 in", seed.Expression + " in (" + literal + ")"),
                ("P7 contains", "contains(" + seed.Expression + ",'v')"),
                ("P7 startswith", "startswith(" + seed.Expression + ",'v')"),
                ("P7 endswith", "endswith(" + seed.Expression + ",'v')"),
                ("P8 tolower", "tolower(" + seed.Expression + ") eq 'v'"),
                ("P8 toupper", "toupper(" + seed.Expression + ") eq 'V'")
            };

            return positions.Select(position => new GeneratedRow(
                Label: seed.Label + ", " + position.Position,
                Query: "$filter=" + position.Filter + seed.Suffix));
        }

        private static IEnumerable<GeneratedRow> GenerateOrderByRows(QuerySeed seed)
        {
            var keys = new (string Position, string Query)[]
            {
                ("first key", "$orderby=" + seed.Expression),
                ("second key", "$orderby=name," + seed.Expression),
                ("first key, without $", "orderby=" + seed.Expression),
                ("second key, without $", "orderby=name," + seed.Expression)
            };

            return keys.Select(key => new GeneratedRow(
                Label: seed.Label + ", " + key.Position,
                Query: key.Query + seed.Suffix));
        }

        private static KeptRow[] KeepRowsABareAttributeAccepts(IEnumerable<GeneratedRow> rows)
        {
            var keptRows = new List<KeptRow>();

            foreach (GeneratedRow row in rows.DistinctBy(row => row.Query))
            {
                if (TryValidateUnderABareAttribute(row.Query, out QueryNode[] nodes))
                {
                    keptRows.Add(new KeptRow(row.Label, row.Query, nodes));
                }
            }

            return keptRows.ToArray();
        }

        // The base validation runs on a fresh parse before any clause is read: reading a clause
        // first changes ODL's lazy parse state when the parse throws, and a bare attribute would
        // then pass a query it refuses on a fresh parse.
        private static bool TryValidateUnderABareAttribute(string query, out QueryNode[] nodes)
        {
            var enableQueryAttribute = new EnableQueryAttribute();
            HttpRequest request = CreateQueryableReadRequest(query);
            ODataQueryOptions queryOptions = CreateQueryOptions(enableQueryAttribute, request);

            try
            {
                enableQueryAttribute.ValidateQuery(request, queryOptions);
            }
            catch (Exception)
            {
                nodes = Array.Empty<QueryNode>();

                return false;
            }

            var roots = new List<QueryNode>();

            if (queryOptions.Filter is not null)
            {
                roots.Add(queryOptions.Filter.FilterClause.Expression);
            }

            for (OrderByClause orderByClause = queryOptions.OrderBy?.OrderByClause;
                orderByClause is not null;
                orderByClause = orderByClause.ThenBy)
            {
                roots.Add(orderByClause.Expression);
            }

            nodes = roots.SelectMany(CollectNodes).ToArray();

            return true;
        }

        // Every node beneath a root, found through each node's public properties, so a node kind
        // nobody listed here is still found.
        private static IEnumerable<QueryNode> CollectNodes(QueryNode node)
        {
            if (node is null)
            {
                yield break;
            }

            yield return node;

            PropertyInfo[] properties =
                node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties.Where(property => property.GetIndexParameters().Length == 0))
            {
                IEnumerable<QueryNode> children = property.GetValue(node) switch
                {
                    QueryNode child => new[] { child },
                    FilterClause filterClause => new[] { filterClause.Expression },
                    IEnumerable<QueryNode> childNodes => childNodes,
                    _ => Enumerable.Empty<QueryNode>()
                };

                foreach (QueryNode descendant in children.SelectMany(CollectNodes))
                {
                    yield return descendant;
                }
            }
        }

        private static TheoryData<string, string> ToTheoryData(IEnumerable<KeptRow> keptRows)
        {
            var theoryData = new TheoryData<string, string>();

            foreach (KeptRow keptRow in keptRows)
            {
                theoryData.Add(keptRow.Label, keptRow.Query);
            }

            return theoryData;
        }

        private static bool HoldsKind(KeptRow keptRow, QueryNodeKind queryNodeKind) =>
            keptRow.Nodes.Any(node => node.Kind == queryNodeKind);

        private static bool HoldsOperator(KeptRow keptRow, Enum operatorKind)
        {
            return keptRow.Nodes.Any(node => node switch
            {
                BinaryOperatorNode binaryOperatorNode => operatorKind.Equals(binaryOperatorNode.OperatorKind),
                UnaryOperatorNode unaryOperatorNode => operatorKind.Equals(unaryOperatorNode.OperatorKind),
                _ => false
            });
        }

        private static bool CallsFunction(KeptRow keptRow, string functionName)
        {
            return keptRow.Nodes.Any(node =>
                node is SingleValueFunctionCallNode functionCallNode
                    && string.Equals(functionCallNode.Name, functionName, StringComparison.OrdinalIgnoreCase));
        }

        private sealed record QuerySeed(string Label, string Expression, string Literal, string Suffix = "");

        private sealed record GeneratedRow(string Label, string Query);

        private sealed record KeptRow(string Label, string Query, QueryNode[] Nodes);

        private sealed record NodeKindWithoutARow(QueryNodeKind Kind, QuerySeed Seed, string Reason);
    }
}
