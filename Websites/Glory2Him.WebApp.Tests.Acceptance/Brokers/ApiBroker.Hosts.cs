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

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        /// <summary>
        /// The page size every queryable collection read will actually serve at, taken off the
        /// booted host's action descriptors — the same instances the request pipeline uses.
        ///
        /// <para>Deliberately narrow. The host's whole container would answer this and much else
        /// besides, and this broker is otherwise closed: requests go over HTTP, with one named
        /// storage seam for state no endpoint can arrange. Handing every test an
        /// <c>IServiceProvider</c> would invite assertions against internals that belong behind
        /// the API, so what leaves here is the one fact a caller needs.</para>
        /// </summary>
        internal IReadOnlyList<int> GetQueryableCollectionPageSizes() =>
            webApplicationFactory.Services
                .GetRequiredService<IActionDescriptorCollectionProvider>()
                .ActionDescriptors.Items
                .SelectMany(actionDescriptor => actionDescriptor.FilterDescriptors)
                .Select(filterDescriptor => filterDescriptor.Filter)
                .OfType<EnableQueryAttribute>()
                .Select(enableQueryAttribute => enableQueryAttribute.PageSize)
                .ToList();
    }
}
