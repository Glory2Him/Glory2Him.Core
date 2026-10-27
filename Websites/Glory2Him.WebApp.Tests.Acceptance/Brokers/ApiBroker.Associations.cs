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
using System.Net.Http.Json;
using System.Text;
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

        // A hand-built body, posted as RAW JSON so a test can send members the typed request model
        // does not carry: the fields outside the upsert's two-endpoint shape (§ARC16.8.1). The raw
        // response, for the reason PostAssociationAsync gives.
        public async ValueTask<HttpResponseMessage> PostAssociationRawAsync(string json)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await this.httpClient.PostAsync(associationsRelativeUrl, content);
        }

        // The raw response, because the status code is the assertion on the validation arms and
        // a typed read throws on them. The query string is the caller's, whole, because how the
        // ids bind from it — and what the read does with options beside them — is under test.
        public async ValueTask<HttpResponseMessage> GetReactionSummariesAsync(string queryString)
        {
            string url = string.IsNullOrEmpty(queryString)
                ? $"{associationsRelativeUrl}/ReactionSummaries"
                : $"{associationsRelativeUrl}/ReactionSummaries?{queryString}";

            return await this.httpClient.GetAsync(url);
        }

        // The raw response, because the status code is the assertion: the read answers 404 to
        // every caller a row is withheld from, and a typed read throws on it.
        public async ValueTask<HttpResponseMessage> GetAssociationByIdAsync(Guid associationId) =>
            await this.httpClient.GetAsync($"{associationsRelativeUrl}/{associationId}");

        // The raw response, because the status code is the assertion and a typed delete throws on
        // every refusal. The pair travels on the query string, each enum as its number, which is
        // how the React app's broker sends it.
        public async ValueTask<HttpResponseMessage> DeleteAssociationPairAsync(Association association)
        {
            string queryString =
                $"entityAType={(int)association.EntityAType}"
                + $"&entityAKeyId={association.EntityAKeyId}"
                + $"&entityBType={(int)association.EntityBType}"
                + $"&entityBKeyId={association.EntityBKeyId}";

            return await this.httpClient.DeleteAsync($"{associationsRelativeUrl}/Pair?{queryString}");
        }
    }
}
