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
using FluentAssertions;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Foundations.Attachments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Glory2Him.Core.Tests.Unit.Brokers.Storages
{
    /// <summary>
    /// A guard on every published-slot index — §3.4.1's at-most-one-published-row-per-group
    /// invariant, for each entity that carries it.
    ///
    /// <para>Nothing else in the suite would notice if the <c>IsDeleted</c> term were dropped:
    /// index predicates are invisible to ordinary tests, and <c>has-pending-model-changes</c>
    /// detects a model the migrations do not match, not a model that is wrong.</para>
    ///
    /// <para>The term is what distinguishes "one <i>live</i> published version per group" from
    /// "one published version per group, ever". Without it a soft-deleted published row keeps
    /// holding its group's slot — invisible to every read (§10.4), yet still colliding — so
    /// promoting any later version fails at the database naming a row the caller cannot see
    /// (§5.6.4 rule 4).</para>
    ///
    /// <para>The whole set is asserted rather than one entity, because all three were written
    /// by hand and all three drifted to the same wrong shape. The completeness case matters
    /// most: it fails when a NEW versioned entity arrives without the index, which no
    /// per-entity test can catch.</para>
    /// </summary>
    public class StorageBrokerPublishedSlotIndexTests
    {
        private static readonly (Type EntityType, string IndexName)[] ExpectedIndexes =
        {
            (typeof(Attachment), "UX_Attachments_GroupId_IsPublished"),
            (typeof(ContentItem), "IX_ContentItem_IsPublished"),
            (typeof(Link), "UX_Links_GroupId_IsPublished"),
        };

        public static TheoryData<Type, string> PublishedSlotIndexes
        {
            get
            {
                var publishedSlotIndexes = new TheoryData<Type, string>();

                foreach ((Type entityType, string indexName) in ExpectedIndexes)
                {
                    publishedSlotIndexes.Add(entityType, indexName);
                }

                return publishedSlotIndexes;
            }
        }

        [Theory]
        [MemberData(nameof(PublishedSlotIndexes))]
        public void ShouldRestrictEveryPublishedSlotIndexToLiveRows(
            Type entityType,
            string indexName)
        {
            // given
            IIndex publishedSlotIndex = StorageBrokerModelSource.Model
                .FindEntityType(entityType)!
                .GetIndexes()
                .Single(index => index.GetDatabaseName() == indexName);

            string expectedFilter =
                $"[{nameof(IApproval.IsPublished)}] = 1 "
                    + $"AND [{nameof(IAudit.IsDeleted)}] = 0";

            // when
            string? actualFilter = publishedSlotIndex.GetFilter();

            // then
            publishedSlotIndex.IsUnique.Should().BeTrue();
            actualFilter.Should().Be(expectedFilter);

            publishedSlotIndex.Properties.Select(property => property.Name).Should()
                .Equal(new[] { nameof(IVersion.GroupId) }, because:
                    "the filter pins IsPublished, so GroupId alone carries the uniqueness");
        }

        [Fact]
        public void ShouldGiveEveryVersionedApprovableEntityAPublishedSlotIndex()
        {
            // given
            IEnumerable<Type> guardedTypes =
                ExpectedIndexes.Select(expected => expected.EntityType);

            // when
            List<Type> versionedApprovableTypes = StorageBrokerModelSource.Model
                .GetEntityTypes()
                .Select(entityType => entityType.ClrType)
                .Where(clrType =>
                    typeof(IVersion).IsAssignableFrom(clrType)
                        && typeof(IApproval).IsAssignableFrom(clrType)
                        && typeof(IAudit).IsAssignableFrom(clrType))
                .ToList();

            // then
            versionedApprovableTypes.Should().BeEquivalentTo(guardedTypes, because:
                "an entity that versions and publishes needs the slot index, and a new one "
                    + "arriving without a row above would otherwise ship unguarded");
        }
    }
}
