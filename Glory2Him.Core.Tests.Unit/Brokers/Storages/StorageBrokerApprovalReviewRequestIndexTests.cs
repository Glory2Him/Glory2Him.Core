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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Tests.Unit.Brokers.Storages
{
    /// <summary>
    /// A guard on the one index that stands behind §7.9 rule 1, and the twin of
    /// <see cref="StorageBrokerApprovalReviewIndexTests"/>.
    ///
    /// <para>This index is the SOLE enforcement of "one active invitation per person per
    /// approval" — there is no service-side duplicate check to fall back on, because the
    /// orchestration's idempotent dismiss (§7.9 rule 4) reads the row and would still race two
    /// concurrent requests. Nothing else in the suite would notice if the index were dropped or
    /// its filter narrowed, and <c>has-pending-model-changes</c> would not either: that detects a
    /// model the migrations do not match, not a model that is wrong.</para>
    ///
    /// <para>The filter is what distinguishes "one <i>active</i> invitation" from "one invitation
    /// ever". Withdrawal (§7.9 rule 5) and retirement (rule 6) are both SOFT deletes, so the row
    /// stays; unfiltered, the <c>(ApprovalId, RequestedUserId)</c> slot would be reserved
    /// permanently and a person whose invitation was withdrawn by mistake — the exact case rule 5
    /// exists to undo — could never be invited again.</para>
    ///
    /// <para>There is deliberately no status term here, unlike the review index: an invitation
    /// carries no verdict, so deletion is the only state that can retire it.</para>
    /// </summary>
    public class StorageBrokerApprovalReviewRequestIndexTests
    {
        private const string UniqueIndexName = "UX_ApprovalReviewRequests_ApprovalId_RequestedUserId";

        [Fact]
        public void ShouldRestrictTheInvitationUniqueIndexToActiveRequests()
        {
            // given
            IModel model = CreateStorageBrokerModel();

            IIndex invitationIndex = model
                .FindEntityType(typeof(ApprovalReviewRequest))!
                .GetIndexes()
                .Single(index => index.GetDatabaseName() == UniqueIndexName);

            string expectedFilter = $"[{nameof(ApprovalReviewRequest.IsDeleted)}] = 0";

            // when
            string? actualFilter = invitationIndex.GetFilter();

            // then
            invitationIndex.IsUnique.Should().BeTrue();
            actualFilter.Should().Be(expectedFilter);

            // RequestedUserId, never the display name: two accounts can share a display name, and
            // an index on one would refuse a legitimate second invitation and let a duplicate
            // through under a changed name.
            invitationIndex.Properties.Select(property => property.Name).Should()
                .Equal(
                    nameof(ApprovalReviewRequest.ApprovalId),
                    nameof(ApprovalReviewRequest.RequestedUserId));
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
