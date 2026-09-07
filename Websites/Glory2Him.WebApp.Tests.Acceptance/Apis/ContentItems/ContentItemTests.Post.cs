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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItems;
using RESTFulSense.Exceptions;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItems
{
    public partial class ContentItemApiTests
    {
        [Fact]
        public async Task ShouldPostContentItemAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            ContentItem expectedContentItem = inputContentItem;

            try
            {
                // when
                ContentItem createdContentItem =
                    await this.apiBroker.PostContentItemAsync(inputContentItem);

                ContentItem actualContentItem =
                    await this.apiBroker.GetContentItemByIdAsync(createdContentItem.Id);

                // then
                actualContentItem.Should().BeEquivalentTo(expectedContentItem, options => options
                    .Excluding(property => property.Id)
                    .Excluding(property => property.CreatedBy)
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen)

                    // Derived, not echoed. ContentHash is computed from Content (§3.4.2), and
                    // GroupId and Version are assigned by the add because a new item is version
                    // 1 of its own group (§12.4.1 rule 6, §3.4.1). Asserting them against the
                    // request would be asserting that the service ignored its own rules.
                    .Excluding(property => property.ContentHash)
                    .Excluding(property => property.GroupId)
                    .Excluding(property => property.Version));

                // The derived fields are asserted as DERIVED rather than skipped: a service that
                // silently left them unset would otherwise pass the exclusions above.
                createdContentItem.ContentHash.Should().NotBeNullOrWhiteSpace();
                createdContentItem.GroupId.Should().NotBe(Guid.Empty);
                createdContentItem.Version.Should().Be(1);

                inputContentItem.Id = createdContentItem.Id;
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemByIdAsync(inputContentItem.Id);
            }
        }

        // §3.4.2 rule 6 end to end, and it is the SECOND post that is under test. It must come
        // back looking exactly like the first — 201, a body with an id, a group and version 1,
        // no error and no mention of a duplicate — while creating nothing. Asserted from the
        // wire rather than from the service because the leak this closes was a wire leak: the
        // refusal reached the SPA and it printed the sentence (#392, #412).
        [Fact]
        public async Task ShouldAcknowledgeDuplicateContentOnPostWithoutCreatingItAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem firstContentItem = randomContentItem;

            ContentItem duplicateContentItem = CreateRandomContentItem();
            duplicateContentItem.ContentType = firstContentItem.ContentType;
            duplicateContentItem.Content = firstContentItem.Content;

            ContentItem createdContentItem =
                await this.apiBroker.PostContentItemAsync(firstContentItem);

            // Set as soon as the acknowledgement comes back, so the finally can clean up after a
            // REGRESSION as well as after a pass. If the quiet arm ever starts writing a row, the
            // not-found assertion below fails and this id names a real row carrying the same
            // (ContentType, ContentHash) as the fixture — left behind, it is a live duplicate that
            // makes the FIRST post of every later run acknowledge instead of create, and the
            // failure then reads as something else entirely.
            Guid? acknowledgedContentItemId = null;

            try
            {
                // when
                ContentItem acknowledgedContentItem =
                    await this.apiBroker.PostContentItemAsync(duplicateContentItem);

                acknowledgedContentItemId = acknowledgedContentItem.Id;

                // then: the acknowledgement is shaped as a created item, down to the derived
                // fields — an answer a caller could tell apart names the duplicate as plainly
                // as the message it replaced
                acknowledgedContentItem.Id.Should().NotBe(Guid.Empty);
                acknowledgedContentItem.Id.Should().NotBe(createdContentItem.Id);
                acknowledgedContentItem.GroupId.Should().NotBe(Guid.Empty);
                acknowledgedContentItem.GroupId.Should().NotBe(createdContentItem.GroupId);
                acknowledgedContentItem.Version.Should().Be(1);
                acknowledgedContentItem.ContentHash.Should().Be(createdContentItem.ContentHash);
                acknowledgedContentItem.CreatedBy.Should().NotBeNullOrWhiteSpace();
                acknowledgedContentItem.UpdatedBy.Should().Be(acknowledgedContentItem.CreatedBy);
                acknowledgedContentItem.CreatedWhen.Should().Be(acknowledgedContentItem.UpdatedWhen);

                // and nothing was written: the acknowledged id resolves to nothing at all
                var readAcknowledgedTask =
                    this.apiBroker.GetContentItemByIdAsync(acknowledgedContentItem.Id).AsTask();

                await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => readAcknowledgedTask);

                // and the row that was already there is untouched
                ContentItem actualFirstContentItem =
                    await this.apiBroker.GetContentItemByIdAsync(createdContentItem.Id);

                actualFirstContentItem.Should().BeEquivalentTo(createdContentItem);
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemByIdAsync(createdContentItem.Id);

                if (acknowledgedContentItemId is not null)
                {
                    // Best effort: on a pass there is nothing there to remove, which is the whole
                    // point of the test, so a failure to delete is not itself a failure.
                    try
                    {
                        await this.apiBroker.RemoveCoreContentItemByIdAsync(
                            acknowledgedContentItemId.Value);
                    }
                    catch (Exception)
                    {
                        // no row to remove — the rule held
                    }
                }
            }
        }
    }
}
