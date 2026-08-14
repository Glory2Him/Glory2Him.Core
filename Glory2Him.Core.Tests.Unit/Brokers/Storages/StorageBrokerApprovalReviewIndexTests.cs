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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Tests.Unit.Brokers.Storages
{
    /// <summary>
    /// A guard on the one index that stands behind §7.7 rule 1.
    ///
    /// <para>Every other rule in the approval workflow is enforced by code a unit test can call.
    /// This one is enforced by a unique index, so nothing in the suite would notice if its filter
    /// were dropped or narrowed — and <c>has-pending-model-changes</c> would not either, because
    /// that detects a model the migrations do not match, not a model that is wrong.</para>
    ///
    /// <para>The filter is what distinguishes "one <i>active</i> review per reviewer" from "one
    /// review per reviewer, ever". Without it a reviewer whose verdict was dismissed — retained
    /// for audit by design (§9.5) — or withdrawn by a soft delete keeps the
    /// <c>(ApprovalId, ReviewerId)</c> slot forever, and §7.7 rule 7's re-file has nowhere to go.
    /// </para>
    /// </summary>
    public class StorageBrokerApprovalReviewIndexTests
    {
        private const string UniqueIndexName = "UX_ApprovalReviews_ApprovalId_CreatedBy";

        [Fact]
        public void ShouldRestrictTheReviewerUniqueIndexToActiveReviews()
        {
            // given
            IModel model = CreateStorageBrokerModel();

            IIndex reviewerIndex = model
                .FindEntityType(typeof(ApprovalReview))!
                .GetIndexes()
                .Single(index => index.GetDatabaseName() == UniqueIndexName);

            // The literal the filter must carry. Written from the enum rather than hard-coded so
            // that renumbering ApprovalStatus fails this test instead of silently repurposing the
            // filter to exclude whichever member landed on 4.
            string expectedFilter =
                $"[{nameof(ApprovalReview.StatusId)}] <> {(int)ApprovalStatus.Dismissed} "
                    + $"AND [{nameof(ApprovalReview.IsDeleted)}] = 0";

            // when
            string? actualFilter = reviewerIndex.GetFilter();

            // then
            reviewerIndex.IsUnique.Should().BeTrue();
            actualFilter.Should().Be(expectedFilter);

            reviewerIndex.Properties.Select(property => property.Name).Should()
                .Equal(nameof(ApprovalReview.ApprovalId), nameof(ApprovalReview.CreatedBy));
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
