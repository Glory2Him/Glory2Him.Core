// ───────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ───────────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Foundations.Attachments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Tests.Unit.Brokers.Storages
{
    /// <summary>
    /// A guard on the index that holds §3.4.1's at-most-one-published-row invariant for an
    /// attachment group — the row §5.6.4 rule 2 makes the only one ever surfaced publicly.
    ///
    /// <para>Nothing else in the suite would notice if the <c>IsDeleted</c> term were dropped: index
    /// predicates are invisible to ordinary tests, and <c>has-pending-model-changes</c> detects a
    /// model the migrations do not match, not a model that is wrong.</para>
    ///
    /// <para>The term is what distinguishes "one <i>live</i> published version per group" from "one
    /// published version per group, ever". Without it a soft-deleted published row keeps holding its
    /// group's slot — invisible to every read (§10.4), yet still colliding — so approving any later
    /// version of that group fails at the database naming a row the caller cannot see, and the group
    /// can never publish again. §9.7.6 rule 1's unpublish-on-remove mandate is the other half; this
    /// is the defence in depth for any row that reaches the state another way.</para>
    /// </summary>
    public class StorageBrokerAttachmentIndexTests
    {
        private const string UniqueIndexName = "UX_Attachments_GroupId_IsPublished";

        [Fact]
        public void ShouldRestrictThePublishedSlotUniqueIndexToLiveRows()
        {
            // given
            IModel model = CreateStorageBrokerModel();

            IIndex publishedSlotIndex = model
                .FindEntityType(typeof(Attachment))!
                .GetIndexes()
                .Single(index => index.GetDatabaseName() == UniqueIndexName);

            string expectedFilter =
                $"[{nameof(Attachment.IsPublished)}] = 1 "
                    + $"AND [{nameof(Attachment.IsDeleted)}] = 0";

            // when
            string? actualFilter = publishedSlotIndex.GetFilter();

            // then
            publishedSlotIndex.IsUnique.Should().BeTrue();
            actualFilter.Should().Be(expectedFilter);

            publishedSlotIndex.Properties.Select(property => property.Name).Should()
                .Equal(nameof(Attachment.GroupId), nameof(Attachment.IsPublished));
        }

        private static IModel CreateStorageBrokerModel()
        {
            // A connection string is required for OnConfiguring to complete, but no connection is
            // opened: EF builds the model lazily from the configuration alone.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Glory2HimConnectionString"] =
                        "Server=(local);Database=ModelOnly;Integrated Security=true;",
                })
                .Build();

            using var storageBroker = new StorageBroker(configuration);

            return storageBroker.Model;
        }
    }
}
