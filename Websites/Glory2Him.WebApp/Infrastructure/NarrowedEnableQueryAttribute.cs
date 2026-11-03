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
            BinaryOperatorKind.And,
            BinaryOperatorKind.Or
        };

        public override void ValidateQuery(HttpRequest request, ODataQueryOptions queryOptions)
        {
            base.ValidateQuery(request, queryOptions);
            RefuseOptionsOutsideTheSix(queryOptions.RawValues);

            if (queryOptions.Filter is not null)
            {
                var ownEntityType = (IEdmStructuredType)queryOptions.Context.ElementType;
                AdmitFilterTerm(queryOptions.Filter.FilterClause.Expression, ownEntityType);
            }
        }

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

        private static bool IsAdmittedFunctionCall(
            SingleValueFunctionCallNode functionCallNode,
            IEdmStructuredType ownEntityType)
        {
            QueryNode[] parameters = functionCallNode.Parameters.ToArray();

            return functionCallNode.Source is null
                && functionCallNode.Name == "contains"
                && parameters.Length == 2
                && IsCaseFoldedStringColumn(parameters[0], ownEntityType)
                && IsStringLiteral(parameters[1]);
        }

        private static bool IsCaseFoldedStringColumn(QueryNode parameter, IEdmStructuredType ownEntityType)
        {
            return parameter is SingleValueFunctionCallNode caseFunctionCallNode
                && caseFunctionCallNode.Source is null
                && caseFunctionCallNode.Name == "tolower"
                && caseFunctionCallNode.Parameters.Count() == 1
                && IsStringColumn(caseFunctionCallNode.Parameters.Single(), ownEntityType);
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
