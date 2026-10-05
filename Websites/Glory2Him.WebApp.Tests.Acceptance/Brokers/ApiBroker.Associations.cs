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
using System.Net.Http.Json;
using System.Threading.Tasks;
using Glory2Him.WebApp.Tests.Acceptance.Models.Associations;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string associationsRelativeUrl = "api/associations";

        // The raw response, because the status code is the assertion: the upsert answers 201 on
        // one outcome and 200 on the rest, and a typed post reports neither — it hands back the
        // body on any success and throws on any failure.
        public async ValueTask<HttpResponseMessage> PostAssociationAsync(Association association) =>
            await this.httpClient.PostAsJsonAsync(associationsRelativeUrl, association);
    }
}
