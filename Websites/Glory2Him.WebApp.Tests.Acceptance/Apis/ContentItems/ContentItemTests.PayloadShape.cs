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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItems
{
    /// <summary>
    /// THE CONTRACT A CONTENT ITEM TRAVELS ON, held still across a schema change.
    ///
    /// <para><c>/api/ContentItems/Public</c> is <c>[HttpGet("Public")] [EnableQuery]
    /// [AllowAnonymous]</c> and serialises <c>ContentItem</c> directly, so a column mapped as an
    /// ordinary CLR property would become a new public field AND a new anonymous
    /// <c>$orderby</c>/<c>$filter</c> target the day a migration lands — a contract change
    /// nobody asked for, on the one route with no caller check in front of it. The effective
    /// publication moment is therefore an EF SHADOW property: it exists in the catalogue, it is
    /// read by the optimiser, and it is on no contract.</para>
    ///
    /// <para>Both checks are here because comparing a response body cannot detect whether a
    /// name is ACCEPTED AS A QUERY TARGET: a property mapped by mistake could be honoured in
    /// <c>$orderby</c> while never appearing in the payload, and the body comparison would pass
    /// a change this refuses.</para>
    /// </summary>
    public partial class ContentItemApiTests
    {
        // The name the catalogue carries for the effective publication moment. Nothing in the
        // solution names it — it is written here precisely so an attempt to USE it can be made
        // and refused.
        private const string EffectivePublicationMomentName = "EffectivePublishedWhen";

        // Every property a content item carried on the wire at the branch point. Hardcoded
        // rather than derived from the model, because a list derived from the CLR type would
        // grow with the type and pass the very change this refuses.
        private static readonly string[] BranchPointContentItemProperties =
        {
            "id",
            "contentType",
            "title",
            "author",
            "content",
            "shareabilityBasis",
            "sharePermission",
            "contentHash",
            "groupId",
            "version",
            "publishDate",
            "isPublished",
            "approvalStatus",
            "isApprovedByBypass",
            "approvedByBypassReason",
            "isDeleted",
            "createdBy",
            "createdWhen",
            "updatedBy",
            "updatedWhen",
            "deletedBy",
            "deletedWhen",
            "deletionReason"
        };

        /// <summary>
        /// Criterion 10, first half: AN ANONYMOUS CALLER SEES THE SAME OBJECT IT SAW BEFORE.
        /// One property more, one fewer or one renamed all fail.
        /// </summary>
        [Fact]
        public async Task ShouldReturnTheSameContentItemShapeToAnAnonymousCaller_AfterTheMigrationAsync()
        {
            // given
            DateTimeOffset now = DateTimeOffset.UtcNow;

            CoreContentItem arrangedContentItem =
                await this.apiBroker.InsertFeedContentItemAsync(
                    createdWhen: now,
                    publishDate: now);

            try
            {
                this.apiBroker.ActAsAnonymous();

                // when
                HttpResponseMessage response =
                    await this.apiBroker.GetContentItemsResponseAsync("api/contentItems/Public");

                string payload = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(payload);

                List<string> propertyNames = document.RootElement
                    .EnumerateArray()
                    .SelectMany(contentItem =>
                        contentItem.EnumerateObject().Select(property => property.Name))
                    .Distinct()
                    .ToList();

                // then
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                propertyNames.Should().NotBeEmpty(
                    because: "the arranged item is publicly visible, so the read returns it");

                propertyNames.Should().BeEquivalentTo(
                    BranchPointContentItemProperties,
                    because: "an anonymous caller's content item carries exactly the properties "
                        + "it carried at the branch point");
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(arrangedContentItem.Id);
            }
        }

        /// <summary>
        /// Criterion 10, second half: THE EFFECTIVE PUBLICATION MOMENT IS NOT A QUERY TARGET, on
        /// any <c>[EnableQuery]</c> content-item route. One request per route per option, each
        /// of which must be REFUSED rather than silently honoured.
        /// </summary>
        [Theory]
        [InlineData("api/contentItems")]
        [InlineData("api/contentItems/Public")]
        [InlineData("api/contentItems/Groups/{groupId}")]
        public async Task ShouldRejectTheEffectivePublicationMomentAsAQueryTarget_ForAnAnonymousCallerAsync(
            string routeTemplate)
        {
            // given
            string route = routeTemplate.Replace("{groupId}", Guid.NewGuid().ToString());

            this.apiBroker.ActAsAnonymous();

            try
            {
                // when
                HttpResponseMessage orderByResponse =
                    await this.apiBroker.GetContentItemsResponseAsync(
                        $"{route}?$orderby={EffectivePublicationMomentName} desc");

                HttpResponseMessage filterResponse =
                    await this.apiBroker.GetContentItemsResponseAsync(
                        $"{route}?$filter={EffectivePublicationMomentName} ne null");

                // then
                orderByResponse.IsSuccessStatusCode.Should().BeFalse(
                    because: $"{EffectivePublicationMomentName} is a shadow property and is on "
                        + "no query surface, so ordering by it is not a request this route "
                        + "can honour");

                filterResponse.IsSuccessStatusCode.Should().BeFalse(
                    because: $"{EffectivePublicationMomentName} is a shadow property and is on "
                        + "no query surface, so filtering on it is not a request this route "
                        + "can honour");
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
            }
        }
    }
}
