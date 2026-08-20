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

using System.Net.Http;
using Glory2Him.Core.Brokers.Storages.Sql;
using Microsoft.Extensions.DependencyInjection;
using RESTFulSense.Clients;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
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
            webApplicationFactory = new TestWebApplicationFactory();
            httpClient = webApplicationFactory.CreateClient();
            apiFactoryClient = new RESTFulApiFactoryClient(httpClient);
            storageBroker = webApplicationFactory.Services.GetService<IStorageBroker>();
        }
    }
}
