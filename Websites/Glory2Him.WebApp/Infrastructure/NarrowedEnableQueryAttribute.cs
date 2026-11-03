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

        public override void ValidateQuery(HttpRequest request, ODataQueryOptions queryOptions)
        {
            base.ValidateQuery(request, queryOptions);
            RefuseOptionsOutsideTheSix(queryOptions.RawValues);

            if (queryOptions.Filter is not null)
            {
                AdmitFilterTerm(queryOptions.Filter.FilterClause.Expression);
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

        private static void AdmitFilterTerm(QueryNode filterTerm) =>
            throw new ODataException(
                $"The $filter holds a term this read does not accept: a '{filterTerm.Kind}' node.");
    }
}
