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
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Glory2Him.WebApp.Infrastructure
{
    /// <summary>
    /// An <see cref="EnableQueryAttribute"/> that accepts only the query a caller cannot use to
    /// learn about rows it may not see (GHSA-3m43-v266-m6qf).
    ///
    /// <para><b>Why the posture beneath the attribute is not enough.</b> A read's visibility
    /// filter is composed into the same SQL as the caller's options, and SQL Server does not
    /// promise to evaluate the filter's terms first. A caller term that can raise on a stored
    /// value can therefore raise on a row the caller may not see, and the answer then differs
    /// from the one it would get if that row did not exist. A term that reaches through a
    /// navigation tests rows the posture never filters at all.</para>
    ///
    /// <para><b>What it accepts.</b> <c>$filter</c>, <c>$orderby</c>, <c>$select</c>,
    /// <c>$top</c>, <c>$skip</c> and <c>$count</c>, and no other option, whatever the
    /// attribute's own <see cref="EnableQueryAttribute.AllowedQueryOptions"/> allow. The rule
    /// reads the options OData parsed, never the query string's keys, because OData maps every
    /// spelling of an option, with or without its <c>$</c> and in any letter case, to the same
    /// parsed option. <c>$filter</c> and <c>$orderby</c> are then walked, node by node, against
    /// an allowlist of terms that cannot raise on any stored value in reach and that reach only
    /// the read's own entity. A node the allowlist does not name is refused wherever it sits;
    /// nothing is accepted because no rule names it.</para>
    ///
    /// <para><b>How it refuses.</b> Every refusal is an <see cref="ODataException"/>, which the
    /// attribute answers with <c>400</c> while it validates, before the caller's options reach
    /// the query. That answer is the same whatever the table holds and whoever asks, and its
    /// message names only what the caller sent.</para>
    ///
    /// <para><b>It narrows and never widens.</b> The base validation runs first, so every
    /// setting the attribute carries still refuses what it refused before.</para>
    /// </summary>
    public sealed class NarrowedEnableQueryAttribute : EnableQueryAttribute
    {
        private static readonly string[] sixAcceptedOptionNames =
        {
            nameof(ODataRawQueryOptions.Filter),
            nameof(ODataRawQueryOptions.OrderBy),
            nameof(ODataRawQueryOptions.Select),
            nameof(ODataRawQueryOptions.Top),
            nameof(ODataRawQueryOptions.Skip),
            nameof(ODataRawQueryOptions.Count)
        };

        private static readonly PropertyInfo[] optionsOutsideTheSix =
            typeof(ODataRawQueryOptions)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(option => sixAcceptedOptionNames.Contains(option.Name) is false)
                        .ToArray();

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

        private static readonly string[] patternFunctionNames = { "contains", "startswith", "endswith" };
        private static readonly string[] caseFunctionNames = { "tolower", "toupper" };

        public override void ValidateQuery(HttpRequest request, ODataQueryOptions queryOptions)
        {
            base.ValidateQuery(request, queryOptions);
            RefuseOptionsOutsideTheSix(queryOptions.RawValues);

            if (queryOptions.Filter is not null)
            {
                AdmitFilterTerm(
                    filterTerm: queryOptions.Filter.FilterClause.Expression,
                    ownEntityType: GetOwnEntityType(queryOptions));
            }

            // OData parses the first key into the clause's Expression and each later key into
            // its ThenBy, so the whole chain is walked, not the first key alone.
            for (OrderByClause? orderByClause = queryOptions.OrderBy?.OrderByClause;
                orderByClause is not null;
                orderByClause = orderByClause.ThenBy)
            {
                AdmitOrderByKey(
                    orderByKey: orderByClause.Expression,
                    ownEntityType: GetOwnEntityType(queryOptions));
            }

            if (queryOptions.SelectExpand is not null)
            {
                RefuseSelectItemsCarryingANestedOption(queryOptions.SelectExpand.SelectExpandClause);
            }
        }

        private static IEdmStructuredType GetOwnEntityType(ODataQueryOptions queryOptions) =>
            (IEdmStructuredType)queryOptions.Context.ElementType;

        private static void RefuseOptionsOutsideTheSix(ODataRawQueryOptions rawValues)
        {
            foreach (PropertyInfo option in optionsOutsideTheSix)
            {
                if (option.GetValue(rawValues) is not null)
                {
                    throw new ODataException(
                        $"The query option '${option.Name.ToLowerInvariant()}' is not accepted by this read.");
                }
            }
        }

        // OData applies a $filter nested in a $select item on a collection-valued structural
        // property, so that item would carry a caller term past the walk. Every nested option the
        // parsed clause carries is refused, whatever it holds and whichever property it sits on:
        // any option on a property's item, and the $filter or $search OData keeps on a $count
        // item. The parsed clause reads every letter case of an option's name as the same option.
        private static void RefuseSelectItemsCarryingANestedOption(SelectExpandClause selectExpandClause)
        {
            foreach (SelectItem selectItem in selectExpandClause.SelectedItems)
            {
                bool carriesANestedOption = selectItem switch
                {
                    PathSelectItem pathSelectItem => pathSelectItem.HasOptions,
                    PathCountSelectItem countSelectItem =>
                        countSelectItem.Filter is not null || countSelectItem.Search is not null,
                    _ => false
                };

                if (carriesANestedOption)
                {
                    throw new ODataException(
                        "The $select holds an item this read does not accept: one carrying a nested query option.");
                }
            }
        }

        // The admitted node kinds, each with its condition, walking every child the kind names.
        // A conversion is one OData inserts to bring two sides to a common type or lift a value to
        // its nullable form, so it widens and never narrows; cast() is a function call, not one.
        private static void AdmitFilterTerm(QueryNode filterTerm, IEdmStructuredType ownEntityType)
        {
            switch (filterTerm)
            {
                case ConstantNode:
                    return;

                case ConvertNode convertNode:
                    AdmitFilterTerm(convertNode.Source, ownEntityType);
                    return;

                case BinaryOperatorNode binaryOperatorNode
                    when admittedBinaryOperators.Contains(binaryOperatorNode.OperatorKind):
                    AdmitFilterTerm(binaryOperatorNode.Left, ownEntityType);
                    AdmitFilterTerm(binaryOperatorNode.Right, ownEntityType);
                    return;

                case UnaryOperatorNode unaryOperatorNode
                    when unaryOperatorNode.OperatorKind == UnaryOperatorKind.Not:
                    AdmitFilterTerm(unaryOperatorNode.Operand, ownEntityType);
                    return;

                case InNode inNode when inNode.Right is CollectionConstantNode:
                    AdmitFilterTerm(inNode.Left, ownEntityType);
                    return;

                case SingleValueFunctionCallNode functionCallNode
                    when IsAdmittedFunctionCall(functionCallNode, ownEntityType):
                    return;

                case SingleValuePropertyAccessNode propertyAccessNode
                    when IsOwnProperty(propertyAccessNode, ownEntityType):
                    return;

                default:
                    throw new ODataException(
                        $"The $filter holds a term this read does not accept: a '{filterTerm.Kind}' node.");
            }
        }

        // Each key is exactly one of the entity's own properties, ascending or descending.
        private static void AdmitOrderByKey(QueryNode orderByKey, IEdmStructuredType ownEntityType)
        {
            if (orderByKey is SingleValuePropertyAccessNode propertyAccessNode
                && IsOwnProperty(propertyAccessNode, ownEntityType))
            {
                return;
            }

            throw new ODataException(
                $"The $orderby holds a key this read does not accept: a '{orderByKey.Kind}' node.");
        }

        private static bool IsAdmittedFunctionCall(
            SingleValueFunctionCallNode functionCallNode,
            IEdmStructuredType ownEntityType)
        {
            return IsPatternFunctionCall(functionCallNode, ownEntityType)
                || IsCaseFunctionCall(functionCallNode, ownEntityType);
        }

        // contains, startswith and endswith search a string column, or tolower or toupper of
        // one, for a literal needle. A column needle is refused: EF Core translates it to a
        // CHARINDEX that raises on a stored value over 4000 characters.
        private static bool IsPatternFunctionCall(
            SingleValueFunctionCallNode functionCallNode,
            IEdmStructuredType ownEntityType)
        {
            QueryNode[] parameters = functionCallNode.Parameters.ToArray();

            return functionCallNode.Source is null
                && patternFunctionNames.Contains(functionCallNode.Name)
                && parameters.Length == 2
                && IsStringColumnOrItsCase(parameters[0], ownEntityType)
                && IsStringLiteral(parameters[1]);
        }

        private static bool IsStringColumnOrItsCase(QueryNode parameter, IEdmStructuredType ownEntityType)
        {
            return IsStringColumn(parameter, ownEntityType)
                || parameter is SingleValueFunctionCallNode caseFunctionCallNode
                    && IsCaseFunctionCall(caseFunctionCallNode, ownEntityType);
        }

        private static bool IsCaseFunctionCall(
            SingleValueFunctionCallNode functionCallNode,
            IEdmStructuredType ownEntityType)
        {
            QueryNode[] parameters = functionCallNode.Parameters.ToArray();

            return functionCallNode.Source is null
                && caseFunctionNames.Contains(functionCallNode.Name)
                && parameters.Length == 1
                && IsStringColumn(parameters[0], ownEntityType);
        }

        // A string column is the entity's own string property, or the conversion OData inserts
        // over one. A non-nullable string arrives inside a function wrapped in that conversion,
        // which lifts it to the nullable string the function takes and changes nothing else.
        private static bool IsStringColumn(QueryNode parameter, IEdmStructuredType ownEntityType)
        {
            QueryNode column = parameter is ConvertNode convertNode
                ? convertNode.Source
                : parameter;

            return column is SingleValuePropertyAccessNode propertyAccessNode
                && IsOwnProperty(propertyAccessNode, ownEntityType)
                && propertyAccessNode.TypeReference.IsString();
        }

        private static bool IsStringLiteral(QueryNode parameter) =>
            parameter is ConstantNode constantNode && constantNode.TypeReference?.IsString() is true;

        // A structural property declared on the read's own entity, reached from $it. Lambdas are
        // refused, so no other range variable exists to be a source.
        private static bool IsOwnProperty(
            SingleValuePropertyAccessNode propertyAccessNode,
            IEdmStructuredType ownEntityType)
        {
            return propertyAccessNode.Source is ResourceRangeVariableReferenceNode { Name: "$it" }
                && propertyAccessNode.Property.PropertyKind == EdmPropertyKind.Structural
                && ownEntityType.DeclaredProperties.Contains(propertyAccessNode.Property);
        }
    }
}
