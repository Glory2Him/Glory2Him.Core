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
using System.Threading.Tasks;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        // The whole response rather than a body: what the cache-policy tests assert is the
        // headers the host put on it, on a success and on a failure alike, so nothing here may
        // throw on a non-success status.
        public async ValueTask<HttpResponseMessage> GetResponseAsync(string relativeUrl) =>
            await this.httpClient.GetAsync(relativeUrl);
    }
}
