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

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Xunit;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.CachePolicies
{
    /// <summary>
    /// The browser's HTTP cache keeps no reader's API answer (design §UI20.8 rule 3): an /api
    /// answer that sets no cache policy of its own carries <c>no-store</c>, so a page read again
    /// after its reader left is answered by the server for whoever is signed in then.
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public class ApiCachePolicyTests
    {
        private readonly ApiBroker apiBroker;

        public ApiCachePolicyTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;
            this.apiBroker.ActAsSeededAdministrator();
        }

        [Fact]
        public async Task ShouldAnswerAnApiReadWithNoStoreWhenItSetsNoPolicyOfItsOwn()
        {
            // given
            string currentUserUrl = "api/accounts/me";

            // when
            this.apiBroker.ActAsAnonymous();

            using HttpResponseMessage anonymousResponse =
                await this.apiBroker.GetResponseAsync(currentUserUrl);

            this.apiBroker.ActAsContributor();

            using HttpResponseMessage signedInResponse =
                await this.apiBroker.GetResponseAsync(currentUserUrl);

            this.apiBroker.ActAsSeededAdministrator();

            // then
            anonymousResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            anonymousResponse.Headers.CacheControl.Should().NotBeNull();
            anonymousResponse.Headers.CacheControl.NoStore.Should().BeTrue();

            signedInResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            signedInResponse.Headers.CacheControl.Should().NotBeNull();
            signedInResponse.Headers.CacheControl.NoStore.Should().BeTrue();
        }
    }
}
