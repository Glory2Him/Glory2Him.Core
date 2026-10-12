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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Tests.Integration.Brokers;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.Associations
{
    /// <summary>
    /// Proves the migration that pins <c>Association.UserId</c>'s collation (§DOM4.6 rule 3)
    /// against a catalogue that holds rows, in both directions.
    ///
    /// <para>SQL Server refuses to alter a column an index keys or filters on, and
    /// <c>UX_Associations_PersonalPair</c> keys on <c>UserId</c> while
    /// <c>UX_Associations_EditorialPair</c> filters on it. So the migration drops both, alters
    /// the column and recreates both, by hand — and nothing but these tests sees the hand-written
    /// key: the shared fixtures build their catalogues from the model.</para>
    ///
    /// <para>The collation read here fails on a wrong collation and not on a missing one, because
    /// a column with no collation of its own takes the catalogue's default, which today is the
    /// pinned one. The migration's operations are what prove the pin is there
    /// (<c>StorageBrokerAssociationCollationTests</c>).</para>
    /// </summary>
    public sealed class AssociationUserIdCollationMigrationTests
    {
        private const string PreviousMigration = "AddSortOrderToReactions";
        private const string CollationMigration = "PinAssociationUserIdToCaseInsensitiveCollation";
        private const string CaseInsensitiveCollation = "SQL_Latin1_General_CP1_CI_AS";
        private const string EditorialPairIndexName = "UX_Associations_EditorialPair";
        private const string PersonalPairIndexName = "UX_Associations_PersonalPair";

        [Fact]
        public async Task ShouldKeepBothPairIndexesWhenTheUserIdCollationIsPinnedAsync()
        {
            // given: a catalogue as it stood before the pin, holding one reader's live reaction
            // and one live editorial association
            using AssociationMigrationQueryBroker migratedBroker =
                AssociationMigrationQueryBroker.CreateOverAnEmptyDatabase(
                    "_AssociationsCollationPinned");

            await migratedBroker.MigrateToAsync(PreviousMigration);

            Association reactionAssociation = CreateReaction();
            Association editorialAssociation = CreateEditorialPair();

            await migratedBroker.InsertNamingOnlyItsIdentityAsync(
                reactionAssociation, editorialAssociation);

            // when
            await migratedBroker.MigrateToAsync(CollationMigration);

            // then
            int storedCount = await migratedBroker.CountStoredAsync(
                reactionAssociation.Id, editorialAssociation.Id);

            storedCount.Should().Be(2, because: "the migration changes no row");

            string actualCollation =
                await migratedBroker.GetColumnCollationAsync(nameof(Association.UserId));

            actualCollation.Should().Be(CaseInsensitiveCollation);

            await ShouldHaveBothPairIndexesAsync(migratedBroker);
        }

        // No collation is asserted: the catalogue reports its default for the column whether
        // Down takes the pin off or keeps it. The migration's Down operations prove that.
        [Fact]
        public async Task ShouldKeepBothPairIndexesWhenTheUserIdCollationPinIsUndoneAsync()
        {
            // given: a catalogue migrated to the pin, holding one reader's live reaction and one
            // live editorial association
            using AssociationMigrationQueryBroker migratedBroker =
                AssociationMigrationQueryBroker.CreateOverAnEmptyDatabase(
                    "_AssociationsCollationUndone");

            await migratedBroker.MigrateToAsync(PreviousMigration);

            Association reactionAssociation = CreateReaction();
            Association editorialAssociation = CreateEditorialPair();

            await migratedBroker.InsertNamingOnlyItsIdentityAsync(
                reactionAssociation, editorialAssociation);
            await migratedBroker.MigrateToAsync(CollationMigration);

            // when
            await migratedBroker.MigrateToAsync(PreviousMigration);

            // then
            int storedCount = await migratedBroker.CountStoredAsync(
                reactionAssociation.Id, editorialAssociation.Id);

            storedCount.Should().Be(2, because: "undoing the migration changes no row");

            await ShouldHaveBothPairIndexesAsync(migratedBroker);
        }

        // §DOM4.6 rule 2 tables both indexes; the migration writes each key by hand, so each is
        // read back key column by key column, in key order
        private static async Task ShouldHaveBothPairIndexesAsync(
            AssociationMigrationQueryBroker migratedBroker)
        {
            (await migratedBroker.GetIndexIsUniqueAsync(EditorialPairIndexName))
                .Should().BeTrue();

            (await migratedBroker.GetIndexKeyColumnsAsync(EditorialPairIndexName))
                .Should().Equal(
                    nameof(Association.EntityAType),
                    nameof(Association.EntityAEffectiveId),
                    nameof(Association.EntityBType),
                    nameof(Association.EntityBEffectiveId));

            (await migratedBroker.GetIndexFilterTermsAsync(EditorialPairIndexName))
                .Should().BeEquivalentTo(
                    $"[{nameof(Association.IsDeleted)}]=(0)",
                    $"[{nameof(Association.UserId)}] IS NULL");

            (await migratedBroker.GetIndexIsUniqueAsync(PersonalPairIndexName))
                .Should().BeTrue();

            (await migratedBroker.GetIndexKeyColumnsAsync(PersonalPairIndexName))
                .Should().Equal(
                    nameof(Association.EntityAType),
                    nameof(Association.EntityAEffectiveId),
                    nameof(Association.EntityBType),
                    nameof(Association.UserId));

            (await migratedBroker.GetIndexFilterTermsAsync(PersonalPairIndexName))
                .Should().BeEquivalentTo(
                    $"[{nameof(Association.IsDeleted)}]=(0)",
                    $"[{nameof(Association.UserId)}] IS NOT NULL");
        }

        // ContentItem sorts below Tag ordinally, so it belongs on A — the only shape the
        // canonical-order check constraint accepts. No UserId: the row is editorial.
        private static Association CreateEditorialPair() =>
            CreateAssociation(entityBType: EntityType.Tag);

        // A reader's reaction on an item: the UserId is what makes the row personal.
        private static Association CreateReaction()
        {
            Association reactionAssociation =
                CreateAssociation(entityBType: EntityType.Reaction);

            reactionAssociation.UserId = Guid.NewGuid().ToString();

            return reactionAssociation;
        }

        private static Association CreateAssociation(EntityType entityBType)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string actor = Guid.NewGuid().ToString();

            return new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = Guid.NewGuid(),
                EntityAScope = Scope.AllVersions,
                EntityAContentType = null,
                EntityBType = entityBType,
                EntityBKeyId = Guid.NewGuid(),
                EntityBGroupId = Guid.NewGuid(),
                EntityBScope = Scope.AllVersions,
                EntityBContentType = null,
                ApprovalStatus = ApprovalStatus.Draft,
                IsPublished = false,
                IsDeleted = false,
                CreatedBy = actor,
                CreatedWhen = now,
                UpdatedBy = actor,
                UpdatedWhen = now
            };
        }
    }
}
