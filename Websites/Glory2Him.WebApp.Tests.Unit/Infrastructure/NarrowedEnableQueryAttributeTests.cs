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
using Glory2Him.WebApp.Tests.Unit.Infrastructure.QueryModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure
{
    /// <summary>
    /// Validates query options the way <see cref="EnableQueryAttribute"/> validates them on a
    /// queryable read: the attribute's own model for the element type, a query context over it,
    /// the options parsed from the request, then <see cref="EnableQueryAttribute.ValidateQuery"/>.
    /// Each request carries the host's query configuration, so the base validation accepts what
    /// it accepts on the host and nothing here passes for want of it.
    /// </summary>
    public partial class NarrowedEnableQueryAttributeTests
    {
        private static readonly IServiceProvider hostQueryServices = CreateHostQueryServices();
        private static readonly ControllerActionDescriptor queryableReadDescriptor = new();

        // Program.cs configures OData this way. Without it the base validation refuses $filter,
        // $orderby, $select and $top outright, and a refusal test would pass before any walk
        // existed.
        private static IServiceProvider CreateHostQueryServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddControllers()
                .AddOData(options => options
                    .Select()
                    .Filter()
                    .OrderBy()
                    .Count()
                    .SetMaxTop(null));

            return services.BuildServiceProvider();
        }

        private static void ValidateQuery(EnableQueryAttribute enableQueryAttribute, string query)
        {
            HttpRequest request = CreateQueryableReadRequest(query);

            ODataQueryOptions queryOptions =
                CreateQueryOptions(enableQueryAttribute, request);

            enableQueryAttribute.ValidateQuery(request, queryOptions);
        }

        private static HttpRequest CreateQueryableReadRequest(string query)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = hostQueryServices
            };

            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");
            httpContext.Request.Path = "/api/QueryItems";
            httpContext.Request.QueryString = new QueryString("?" + query);

            return httpContext.Request;
        }

        private static ODataQueryOptions CreateQueryOptions(
            EnableQueryAttribute enableQueryAttribute,
            HttpRequest request)
        {
            IEdmModel model = enableQueryAttribute.GetModel(
                elementClrType: typeof(QueryItem),
                request,
                actionDescriptor: queryableReadDescriptor);

            var queryContext = new ODataQueryContext(
                model,
                elementClrType: typeof(QueryItem),
                path: request.ODataFeature().Path);

            return new ODataQueryOptions(queryContext, request);
        }
    }
}
