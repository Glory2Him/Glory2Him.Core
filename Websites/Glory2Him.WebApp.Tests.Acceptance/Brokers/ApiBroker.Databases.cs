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
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        /// <summary>
        /// The three production connection-string keys the booted host actually resolved, as
        /// catalogue names — read off the host's own <c>IConfiguration</c>, so this is what the
        /// request pipeline connects to rather than what the test project intended.
        ///
        /// <para>Deliberately narrow, for the reason given on
        /// <see cref="GetQueryableCollectionPageSizes"/>: the catalogue name is the one fact a
        /// caller needs, and handing tests the container would invite assertions against
        /// internals that belong behind the API.</para>
        /// </summary>
        internal IReadOnlyDictionary<string, string> GetResolvedDatabaseNames()
        {
            var configuration =
                webApplicationFactory.Services.GetRequiredService<IConfiguration>();

            string[] productionKeys = new[]
            {
                "Glory2HimConnectionString",
                "EventHighwayConnectionString",
                "Glory2HimSecurityConnection"
            };

            return productionKeys.ToDictionary(
                productionKey => productionKey,
                productionKey => new SqlConnectionStringBuilder(
                    configuration.GetConnectionString(productionKey)).InitialCatalog);
        }
    }
}
