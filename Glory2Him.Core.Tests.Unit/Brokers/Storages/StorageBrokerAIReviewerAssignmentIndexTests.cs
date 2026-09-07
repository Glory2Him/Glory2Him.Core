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

using System.Linq;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Glory2Him.Core.Tests.Unit.Brokers.Storages
{
    /// <summary>
    /// A guard on the one index that stands behind the one-live-assignment-per-approval
    /// invariant, and the simpler twin of <see cref="StorageBrokerApprovalReviewRequestIndexTests"/>
    /// — <c>AIReviewerAssignment</c> has no per-person dimension, so this index carries the whole
    /// invariant on <c>ApprovalId</c> alone rather than a composite key.
    ///
    /// <para>This index is the SOLE enforcement of "one live assignment per approval" — there is
    /// no service-side duplicate check to fall back on. Nothing else in the suite would notice if
    /// the index were dropped or its filter narrowed, and <c>has-pending-model-changes</c> would
    /// not either: that detects a model the migrations do not match, not a model that is
    /// wrong.</para>
    ///
    /// <para>The filter is what distinguishes "one <i>live</i> assignment" from "one assignment
    /// ever". Removal is a SOFT delete, so the row stays; unfiltered, the <c>ApprovalId</c> slot
    /// would be reserved permanently and a round whose assignment was removed by mistake could
    /// never have Berean assigned to it again.</para>
    /// </summary>
    public class StorageBrokerAIReviewerAssignmentIndexTests
    {
        private const string UniqueIndexName = "UX_AIReviewerAssignments_ApprovalId";

        [Fact]
        public void ShouldRestrictTheAssignmentUniqueIndexToLiveAssignments()
        {
            // given
            IModel model = StorageBrokerModelSource.Model;

            IIndex assignmentIndex = model
                .FindEntityType(typeof(AIReviewerAssignment))!
                .GetIndexes()
                .Single(index => index.GetDatabaseName() == UniqueIndexName);

            string expectedFilter = $"[{nameof(AIReviewerAssignment.IsDeleted)}] = 0";

            // when
            string? actualFilter = assignmentIndex.GetFilter();

            // then
            assignmentIndex.IsUnique.Should().BeTrue();
            actualFilter.Should().Be(expectedFilter);

            assignmentIndex.Properties.Select(property => property.Name).Should()
                .Equal(nameof(AIReviewerAssignment.ApprovalId));
        }
    }
}
