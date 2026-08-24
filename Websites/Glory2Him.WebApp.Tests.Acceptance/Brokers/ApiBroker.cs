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
using System.Net.Http;
using Glory2Him.Core.Brokers.Storages.Sql;
using Microsoft.Extensions.DependencyInjection;
using RESTFulSense.Clients;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker : IDisposable
    {
        private readonly TestWebApplicationFactory webApplicationFactory;
        private readonly HttpClient httpClient;
        private readonly IRESTFulApiFactoryClient apiFactoryClient;

        // The host's own storage broker, for arranging and tearing down state that no endpoint
        // can produce — an approval round, for instance, which the approve decision reads but
        // which only the approval orchestration would normally create. Real rows in the real
        // database, written through the same broker the request will read back through, so this
        // arranges the system rather than mocking it.
        internal readonly IStorageBroker storageBroker;

        public ApiBroker()
        {
            // This fixture owns the run: xUnit builds a collection fixture once, and every test
            // class in this suite is in that one collection. So the databases are cleared here,
            // before the host is built, and dropped again in Dispose — the same shape the
            // integration suite uses, and the reason neither needs a static flag or a
            // ProcessExit hook (which does not fire under the test host anyway).
            //
            // Allowed to throw. A stale catalogue from a previous run that reused this process id
            // would leave Core rows and registered EventHighway listeners behind, and a silent
            // failure here buys a green run against state the source no longer has.
            AcceptanceDatabaseBroker.DropDatabases(isBestEffort: false);

            webApplicationFactory = new TestWebApplicationFactory();
            httpClient = webApplicationFactory.CreateClient();
            apiFactoryClient = new RESTFulApiFactoryClient(httpClient);
            storageBroker = webApplicationFactory.Services.GetService<IStorageBroker>();
        }

        public void Dispose()
        {
            // The host goes down FIRST. Dropping under a live host would race the substrate's own
            // background work, and Identity and Core both hold open contexts until it stops.
            httpClient?.Dispose();
            webApplicationFactory?.Dispose();

            // Best effort on the way out: an orphaned per-process catalogue is a nuisance, and the
            // next run with the same process id clears it, but throwing from teardown would mask
            // the run's real result.
            AcceptanceDatabaseBroker.DropDatabases(isBestEffort: true);

            GC.SuppressFinalize(this);
        }
    }
}
