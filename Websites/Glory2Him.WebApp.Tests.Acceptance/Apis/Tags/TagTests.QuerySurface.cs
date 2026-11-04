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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Tags
{
    /// <summary>
    /// The query-surface probes on <c>GET api/Tags</c> (GHSA-3m43-v266-m6qf, Fix task C). The
    /// probe's term raises on a row it matches, so before the fix the answer told a caller
    /// whether a row it may not see exists. Now it is refused at validation, the same for every
    /// caller and whether or not any row matches.
    /// </summary>
    public partial class TagApiTests
    {
        private const string ProbeRest = "eq 5";

        public static TheoryData<string, string> CallersAndCarryingOptions()
        {
            var theoryData = new TheoryData<string, string>();

            foreach (string caller in QuerySurfaceProbes.Callers)
            {
                foreach (string carryingOption in QuerySurfaceProbes.CarryingOptions)
                {
                    theoryData.Add(caller, carryingOption);
                }
            }

            return theoryData;
        }

        private static string ProbeLeftOperand(string tagName) =>
            $"1 div indexof(name,'{tagName}')";

        /// <summary>
        /// C4. A probe matching another user's draft tag is refused, whoever asks and whichever
        /// option carries it.
        /// </summary>
        [Theory]
        [MemberData(nameof(CallersAndCarryingOptions))]
        public async Task ShouldRefuseAProbeMatchingAnotherUsersDraftTagAsync(
            string caller,
            string carryingOption)
        {
            // given
            CoreTag draftTag =
                await this.apiBroker.InsertDraftTagAsync(authorUserId: Guid.NewGuid().ToString());

            string queryString = QuerySurfaceProbes.Carry(
                carryingOption,
                ProbeLeftOperand(draftTag.Name),
                ProbeRest);

            try
            {
                QuerySurfaceProbes.ActAs(this.apiBroker, caller);

                // when
                HttpResponseMessage response = await this.apiBroker.GetTagsResponseAsync(queryString);

                // then
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreTagByIdAsync(draftTag.Id);
            }
        }
    }
}
