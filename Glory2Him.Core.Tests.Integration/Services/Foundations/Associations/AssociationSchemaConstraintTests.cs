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
            Guid contentItemGroupId = Guid.NewGuid();
            Guid tagGroupId = Guid.NewGuid();

            Association firstAssociation = CreatePair(contentItemGroupId, tagGroupId);
            Association duplicateAssociation = CreatePair(contentItemGroupId, tagGroupId);

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
            Guid contentItemGroupId = Guid.NewGuid();
            Guid tagGroupId = Guid.NewGuid();

            Association canonicalAssociation = CreatePair(contentItemGroupId, tagGroupId);

            Association reversedAssociation = CreateAssociation(
                entityAType: EntityType.Tag,
                entityAGroupId: tagGroupId,
                entityBType: EntityType.ContentItem,
                entityBGroupId: contentItemGroupId);

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
            Guid contentItemGroupId = Guid.NewGuid();
            Guid reactionGroupId = Guid.NewGuid();

            Association firstUserAssociation = CreatePair(contentItemGroupId, reactionGroupId);
            firstUserAssociation.UserId = Guid.NewGuid().ToString();

            Association secondUserAssociation = CreatePair(contentItemGroupId, reactionGroupId);
            secondUserAssociation.UserId = Guid.NewGuid().ToString();

            Association sameUserAgainAssociation = CreatePair(contentItemGroupId, reactionGroupId);
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
            Guid contentItemGroupId = Guid.NewGuid();
            Guid tagGroupId = Guid.NewGuid();

            Association firstAssociation = CreatePair(contentItemGroupId, tagGroupId);
            firstAssociation.EntityAKeyId = Guid.NewGuid();

            Association sameMeaningAssociation = CreatePair(contentItemGroupId, tagGroupId);
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
        private static Association CreatePair(Guid contentItemGroupId, Guid otherGroupId) =>
            CreateAssociation(
                entityAType: EntityType.ContentItem,
                entityAGroupId: contentItemGroupId,
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
