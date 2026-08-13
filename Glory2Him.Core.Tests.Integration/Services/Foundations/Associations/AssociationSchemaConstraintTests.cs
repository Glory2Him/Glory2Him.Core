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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.Associations;
using Glory2Him.Core.Tests.Integration.Brokers;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.Associations
{
    /// <summary>
    /// Proves the Association table defends its own shape.
    ///
    /// <para>These rules are all enforced by the service as well, and the unit suite already
    /// covers that. The point here is that the service is not the only way in: a foundation
    /// service is reachable through a public event address, and migrations, backfills and
    /// direct SQL bypass it entirely. A constraint that exists only in C# is not a constraint.
    /// So every insert below goes through the storage broker, deliberately skipping the
    /// service, and asserts on what the DATABASE does.</para>
    /// </summary>
    [Collection(AssociationIntegrationCollection.Name)]
    public sealed class AssociationSchemaConstraintTests : IDisposable
    {
        private readonly AssociationQueryBroker broker;
        private readonly List<Association> seededAssociations;

        public AssociationSchemaConstraintTests(AssociationQueryBroker broker)
        {
            this.broker = broker;
            this.seededAssociations = new List<Association>();
        }

        [Fact]
        public async Task ShouldRejectADuplicatePairAsync()
        {
            // given: canonical ordering puts ContentItem on A, since "ContentItem" sorts below
            // "Tag" ordinally
            Guid groupId = Guid.NewGuid();
            Guid tagGroupId = Guid.NewGuid();

            Association firstAssociation = CreatePair(groupId, tagGroupId);
            Association duplicateAssociation = CreatePair(groupId, tagGroupId);

            // when
            Exception firstOutcome = await SeedAsync(firstAssociation);
            Exception duplicateOutcome = await this.broker.TryInsertAsync(duplicateAssociation);

            // then
            firstOutcome.Should().BeNull(because: "the first of a pair is always allowed");

            duplicateOutcome.Should().BeOfType<DuplicateKeyWithUniqueIndexException>(
                because: "UX_Associations_Pair rejects the second");

            duplicateOutcome.Message.Should().Contain("UX_Associations_Pair");
        }

        [Fact]
        public async Task ShouldRejectTheSamePairWrittenInReverseAsync()
        {
            // given: the same two entities, endpoints swapped. This is the case the unique
            // index alone cannot catch — reversed, the pair is a different key — which is why
            // canonical ordering is enforced by its own constraint rather than left to the
            // service that normally applies it.
            Guid groupId = Guid.NewGuid();
            Guid tagGroupId = Guid.NewGuid();

            Association canonicalAssociation = CreatePair(groupId, tagGroupId);

            Association reversedAssociation = CreateAssociation(
                entityAType: EntityType.Tag,
                entityAGroupId: tagGroupId,
                entityBType: EntityType.ContentItem,
                entityBGroupId: groupId);

            // when
            Exception canonicalOutcome = await SeedAsync(canonicalAssociation);
            Exception reversedOutcome = await this.broker.TryInsertAsync(reversedAssociation);

            // then
            canonicalOutcome.Should().BeNull();

            reversedOutcome.Should().BeOfType<ForeignKeyConstraintConflictException>(
                because: "SQL Server reports a CHECK violation under error 547, which is the "
                    + "number EFxceptions maps to this type");

            reversedOutcome.Message.Should().Contain("CK_Association_CanonicalOrder");
        }

        [Fact]
        public async Task ShouldAllowTwoUsersToPairTheSameEntitiesAsync()
        {
            // given: the reaction case. Reaction is a lookup row, so every "Amen" on a passage
            // is byte-identical apart from who made it — without UserId in the key the second
            // one would be a duplicate. UserId sits LAST in the index and stays nullable, so
            // one index carries both meanings: set, it is one per user; null, it is one
            // globally.
            Guid groupId = Guid.NewGuid();
            Guid reactionGroupId = Guid.NewGuid();

            Association firstUserAssociation = CreatePair(groupId, reactionGroupId);
            firstUserAssociation.UserId = Guid.NewGuid().ToString();

            Association secondUserAssociation = CreatePair(groupId, reactionGroupId);
            secondUserAssociation.UserId = Guid.NewGuid().ToString();

            Association sameUserAgainAssociation = CreatePair(groupId, reactionGroupId);
            sameUserAgainAssociation.UserId = firstUserAssociation.UserId;

            // when
            Exception firstUserOutcome = await SeedAsync(firstUserAssociation);
            Exception secondUserOutcome = await SeedAsync(secondUserAssociation);

            Exception sameUserAgainOutcome =
                await this.broker.TryInsertAsync(sameUserAgainAssociation);

            // then
            firstUserOutcome.Should().BeNull();

            secondUserOutcome.Should().BeNull(
                because: "a different user reacting to the same item is a different row");

            sameUserAgainOutcome.Should().BeOfType<DuplicateKeyWithUniqueIndexException>(
                because: "the SAME user reacting twice is still one row");
        }

        [Fact]
        public async Task ShouldRejectAPairWhoseEndpointsShareAGroupAsync()
        {
            // given: an entity associated with itself
            Guid sharedGroupId = Guid.NewGuid();

            Association selfAssociation = CreateAssociation(
                entityAType: EntityType.ContentItem,
                entityAGroupId: sharedGroupId,
                entityBType: EntityType.Tag,
                entityBGroupId: sharedGroupId);

            // when
            Exception outcome = await this.broker.TryInsertAsync(selfAssociation);

            // then
            outcome.Should().BeOfType<ForeignKeyConstraintConflictException>();
            outcome.Message.Should().Contain("CK_Association_NotSameGroup");
        }

        [Fact]
        public async Task ShouldCollapseTwoAllVersionsRowsDifferingOnlyByKeyIdAsync()
        {
            // given: this is the whole reason the effective id is a computed column rather
            // than a query-time expression. Both rows are AllVersions over the same group, so
            // they MEAN the same thing, but their KeyIds differ — over the raw columns they are
            // two distinct rows and any uniqueness built on KeyId would let both through.
            Guid groupId = Guid.NewGuid();
            Guid tagGroupId = Guid.NewGuid();

            Association firstAssociation = CreatePair(groupId, tagGroupId);
            firstAssociation.EntityAKeyId = Guid.NewGuid();

            Association sameMeaningAssociation = CreatePair(groupId, tagGroupId);
            sameMeaningAssociation.EntityAKeyId = Guid.NewGuid();

            // when
            Exception firstOutcome = await SeedAsync(firstAssociation);

            Exception sameMeaningOutcome =
                await this.broker.TryInsertAsync(sameMeaningAssociation);

            // then
            firstAssociation.EntityAKeyId.Should().NotBe(sameMeaningAssociation.EntityAKeyId,
                because: "the two rows really do differ on the raw column");

            firstOutcome.Should().BeNull();

            sameMeaningOutcome.Should().BeOfType<DuplicateKeyWithUniqueIndexException>(
                because: "AllVersions resolves both to the group id, so they collapse to one key");
        }

        [Fact]
        public async Task ShouldAllowThePairAgainOnceTheOriginalIsSoftDeletedAsync()
        {
            // given: the index is filtered on IsDeleted = 0, and that filter is the whole
            // reason "remove a tag, then add it back" works. Without it, every pair a user
            // ever removed would be permanently unrepeatable — the soft-deleted row would go
            // on occupying the key forever.
            Guid groupId = Guid.NewGuid();
            Guid tagGroupId = Guid.NewGuid();

            Association originalAssociation = CreatePair(groupId, tagGroupId);
            Exception originalOutcome = await SeedAsync(originalAssociation);

            // the pair is taken while the row is live
            Association blockedAssociation = CreatePair(groupId, tagGroupId);
            Exception blockedOutcome = await this.broker.TryInsertAsync(blockedAssociation);

            // when: the original is soft-removed, which is what Remove does — the row stays
            await this.broker.SoftDeleteAsync(originalAssociation);

            Association readdedAssociation = CreatePair(groupId, tagGroupId);
            Exception readdedOutcome = await SeedAsync(readdedAssociation);

            // then
            originalOutcome.Should().BeNull();

            blockedOutcome.Should().BeOfType<DuplicateKeyWithUniqueIndexException>(
                because: "while the original is live the pair is taken");

            readdedOutcome.Should().BeNull(
                because: "the filter excludes the soft-deleted row, so the pair is free again");
        }

        [Fact]
        public async Task ShouldDeployTheCanonicalOrderConstraintWithAnOrdinalCollationAsync()
        {
            // given: CompareEndpoints orders endpoints with string.CompareOrdinal. The
            // constraint has to match, which is why it applies COLLATE Latin1_General_BIN2 to
            // the expression; the database default here is case-insensitive and non-ordinal.
            //
            // No behavioural test can prove this today. The two collations order all eight
            // current EntityType names identically — I checked every pair — so a row that one
            // accepts and the other rejects does not exist to write. The clause only starts
            // mattering when a name is ADDED that distinguishes them, and by then the mistake
            // would already be shipped.
            //
            // So this asserts structurally, against the DEPLOYED object rather than the
            // configuration that produced it: whatever else changes, the constraint that
            // actually exists in the database must still compare ordinally.

            // when
            string constraintDefinition =
                await this.broker.GetCheckConstraintDefinitionAsync(
                    "CK_Association_CanonicalOrder");

            // then
            constraintDefinition.Should().NotBeNull(
                because: "the canonical-order constraint must exist in the deployed schema");

            constraintDefinition.Should().Contain(
                "Latin1_General_BIN2",
                because: "an ordinal collation is what keeps the constraint in agreement with "
                    + "CompareEndpoints; the database default is case-insensitive");
        }

        [Fact]
        public async Task ShouldAcceptExactlyTheEndpointOrderingsTheServiceCallsCanonicalAsync()
        {
            // given: the invariant that actually matters — for EVERY combination of endpoint
            // types, the database must accept a row if and only if the production comparator
            // considers it canonical. If the two ever disagree, either the service writes rows
            // the database rejects, or the database accepts rows the service would have
            // swapped, and the pairing stops being unique.
            //
            // This calls the real CompareEndpoints rather than restating its logic, so it
            // compares the shipped comparator against the shipped constraint.
            EntityType[] entityTypes = Enum.GetValues<EntityType>();

            entityTypes.Should().HaveCountGreaterThan(1);

            // when / then
            foreach (EntityType firstType in entityTypes)
            {
                foreach (EntityType secondType in entityTypes)
                {
                    // two distinct groups, ordered so the same-type case exercises the
                    // group-id tiebreak rather than tripping CK_Association_NotSameGroup
                    Guid firstGroupId = Guid.NewGuid();
                    Guid secondGroupId = Guid.NewGuid();

                    Association candidate = CreateAssociation(
                        entityAType: firstType,
                        entityAGroupId: firstGroupId,
                        entityBType: secondType,
                        entityBGroupId: secondGroupId);

                    bool serviceCallsItCanonical =
                        AssociationService.CompareEndpoints(
                            firstType: firstType,
                            firstGroupId: firstGroupId,
                            secondType: secondType,
                            secondGroupId: secondGroupId) < 0;

                    Exception outcome = await SeedAsync(candidate);

                    (outcome is null).Should().Be(
                        serviceCallsItCanonical,
                        because: $"{firstType} -> {secondType} is "
                            + (serviceCallsItCanonical ? "canonical" : "NOT canonical")
                            + " according to CompareEndpoints, so the database must "
                            + (serviceCallsItCanonical ? "accept" : "reject") + " it");
                }
            }
        }

        private async ValueTask<Exception> SeedAsync(Association association)
        {
            Exception outcome = await this.broker.TryInsertAsync(association);

            if (outcome is null)
            {
                this.seededAssociations.Add(association);
            }

            return outcome;
        }

        // ContentItem sorts below Tag and Reaction ordinally, so it belongs on A — the shape
        // the service's canonical ordering produces, and the only shape the check constraint
        // accepts.
        private static Association CreatePair(Guid groupId, Guid otherGroupId) =>
            CreateAssociation(
                entityAType: EntityType.ContentItem,
                entityAGroupId: groupId,
                entityBType: EntityType.Tag,
                entityBGroupId: otherGroupId);

        private static Association CreateAssociation(
            EntityType entityAType,
            Guid entityAGroupId,
            EntityType entityBType,
            Guid entityBGroupId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string actor = Guid.NewGuid().ToString();

            return new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = entityAType,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = entityAGroupId,
                EntityAScope = Scope.AllVersions,
                EntityAContentType = null,
                EntityBType = entityBType,
                EntityBKeyId = Guid.NewGuid(),
                EntityBGroupId = entityBGroupId,
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

        public void Dispose() =>
            this.broker.ClearAsync(this.seededAssociations).AsTask().GetAwaiter().GetResult();
    }
}
