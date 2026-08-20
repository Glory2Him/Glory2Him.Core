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
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis
{
    /// <summary>
    /// Page size is no longer written on the exposers — it reaches them from configuration
    /// through <c>ODataPageSizeConvention</c>, and this suite raises it so no assertion has to
    /// page. Nothing here fails when that stops working: the collections these tests read are
    /// far smaller than either size, so the suite would keep passing while the mechanism it
    /// depends on quietly did nothing. Hence this test, which reads the size back off the
    /// descriptors that actually serve the requests.
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public class CollectionPagingApiTests
    {
        // Matches the override TestWebApplicationFactory supplies. Deliberately not read from
        // configuration: a test that asks the same source as the code proves nothing.
        private const int ExpectedTestPageSize = 5000;

        private readonly ApiBroker apiBroker;

        public CollectionPagingApiTests(ApiBroker apiBroker) =>
            this.apiBroker = apiBroker;

        [Fact]
        public void ShouldPageEveryQueryableCollectionReadAtTheConfiguredSize()
        {
            // given
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider =
                this.apiBroker.HostServices
                    .GetRequiredService<IActionDescriptorCollectionProvider>();

            // when
            List<EnableQueryAttribute> enableQueryAttributes =
                actionDescriptorCollectionProvider.ActionDescriptors.Items
                    .SelectMany(actionDescriptor => actionDescriptor.FilterDescriptors)
                    .Select(filterDescriptor => filterDescriptor.Filter)
                    .OfType<EnableQueryAttribute>()
                    .ToList();

            // then
            enableQueryAttributes.Should().NotBeEmpty();

            enableQueryAttributes.Should().OnlyContain(enableQueryAttribute =>
                enableQueryAttribute.PageSize == ExpectedTestPageSize);
        }
    }
}
