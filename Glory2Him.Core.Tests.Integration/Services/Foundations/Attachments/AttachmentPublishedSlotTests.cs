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
using System.Threading.Tasks;
using EFxceptions.Models.Exceptions;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Attachments;
using Glory2Him.Core.Tests.Integration.Brokers;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.Attachments
{
    /// <summary>
    /// Proves the published slot behaves the way §5.6.4 rule 4 now says it does.
    ///
    /// <para>The unit guard asserts the filter the MODEL declares. It cannot say whether SQL
    /// Server accepted that predicate, nor what the deployed index then does with a row — a
    /// self-consistent but malformed filter passes it and fails at deploy. So these go
    /// through the storage broker and assert on what the DATABASE does.</para>
    /// </summary>
    [Collection(AttachmentSchemaCollection.Name)]
    public sealed class AttachmentPublishedSlotTests : IDisposable
    {
        private readonly AttachmentSchemaQueryBroker broker;
        private readonly List<Attachment> seededAttachments;

        public AttachmentPublishedSlotTests(AttachmentSchemaQueryBroker broker)
        {
            this.broker = broker;
            this.seededAttachments = new List<Attachment>();
        }

        [Fact]
        public async Task ShouldRejectASecondLivePublishedVersionInTheGroupAsync()
        {
            // given
            Guid groupId = Guid.NewGuid();

            Attachment publishedVersion =
                AttachmentSchemaQueryBroker.CreateVersion(groupId, version: 1, isPublished: true);

            Attachment secondPublishedVersion =
                AttachmentSchemaQueryBroker.CreateVersion(groupId, version: 2, isPublished: true);

            // when
            Exception publishedOutcome = await SeedAsync(publishedVersion);
            Exception secondOutcome = await this.broker.TryInsertAsync(secondPublishedVersion);

            // then
            publishedOutcome.Should().BeNull(because: "the first published version is allowed");

            secondOutcome.Should().BeOfType<DuplicateKeyWithUniqueIndexException>(
                because: "a group has one published row at a time (§3.4.1)");

            secondOutcome.Message.Should().Contain("UX_Attachments_GroupId_IsPublished");
        }

        [Fact]
        public async Task ShouldFreeTheGroupSlotOnceThePublishedVersionIsSoftDeletedAsync()
        {
            // given: this is issue #273. Before the IsDeleted term, the tombstone went on
            // holding the slot — invisible to every read, yet still colliding — so no later
            // version of the group could ever be published, and the only route back was a
            // direct database edit.
            Guid groupId = Guid.NewGuid();

            Attachment publishedVersion =
                AttachmentSchemaQueryBroker.CreateVersion(groupId, version: 1, isPublished: true);

            Attachment replacementVersion =
                AttachmentSchemaQueryBroker.CreateVersion(groupId, version: 2, isPublished: true);

            Exception publishedOutcome = await SeedAsync(publishedVersion);

            // the slot is taken while the published row is live
            Exception blockedOutcome = await this.broker.TryInsertAsync(replacementVersion);

            // when: the published version is soft-removed. IsPublished is left alone on
            // purpose — §9.7.6 rule 1 says the remove flow should clear it, and this asserts
            // the index holds even when it did not.
            await this.broker.SoftDeleteAsync(publishedVersion);

            Exception promotedOutcome = await SeedAsync(replacementVersion);

            // then
            publishedOutcome.Should().BeNull();

            blockedOutcome.Should().BeOfType<DuplicateKeyWithUniqueIndexException>(
                because: "while the published version is live the slot is taken");

            promotedOutcome.Should().BeNull(
                because: "the filter excludes the soft-deleted row, so the slot is free again");
        }

        [Fact]
        public async Task ShouldStillAllowUnpublishedVersionsToPileUpInTheGroupAsync()
        {
            // given: the filter must not turn into "one row per group". Draft replacements
            // are the normal case (§5.6.4 rule 1) and several may sit unpublished at once.
            Guid groupId = Guid.NewGuid();

            Attachment firstDraft =
                AttachmentSchemaQueryBroker.CreateVersion(groupId, version: 1, isPublished: false);

            Attachment secondDraft =
                AttachmentSchemaQueryBroker.CreateVersion(groupId, version: 2, isPublished: false);

            // when
            Exception firstOutcome = await SeedAsync(firstDraft);
            Exception secondOutcome = await SeedAsync(secondDraft);

            // then
            firstOutcome.Should().BeNull();

            secondOutcome.Should().BeNull(
                because: "only the published slot is unique, not the group");
        }

        [Theory]
        [InlineData("UX_Attachments_GroupId_IsPublished")]
        [InlineData("IX_ContentItem_IsPublished")]
        [InlineData("UX_Links_GroupId_IsPublished")]
        public async Task ShouldDeployTheLiveOnlyFilterOnEveryPublishedSlotIndexAsync(
            string indexName)
        {
            // given: asserted against the DEPLOYED object rather than the configuration that
            // produced it. The three carried the flag-only filter for as long as they were
            // written out by hand, so what matters is what SQL Server ended up with.
            string definition = await this.broker.GetIndexFilterDefinitionAsync(indexName);

            // when
            string normalizedDefinition = definition?.Replace(" ", string.Empty);

            // then
            normalizedDefinition.Should().NotBeNull(
                because: $"{indexName} should exist and be filtered");

            normalizedDefinition.Should().Contain("[IsPublished]=(1)");
            normalizedDefinition.Should().Contain("[IsDeleted]=(0)");
        }

        private async ValueTask<Exception> SeedAsync(Attachment attachment)
        {
            Exception outcome = await this.broker.TryInsertAsync(attachment);

            if (outcome is null)
            {
                this.seededAttachments.Add(attachment);
            }

            return outcome;
        }

        public void Dispose() =>
            this.broker.ClearAsync(this.seededAttachments).AsTask().GetAwaiter().GetResult();
    }
}
