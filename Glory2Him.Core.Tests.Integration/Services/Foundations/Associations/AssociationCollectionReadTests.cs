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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Tests.Integration.Brokers;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.Associations
{
    /// <summary>
    /// Proves the collection read filter survives translation to SQL Server.
    ///
    /// <para>Design §14.7 A′ rule 5 claims the caller's reviewable sets are resolved in memory
    /// and that <c>Contains</c> over a local collection becomes <c>IN (...)</c> with the
    /// string-converted enum values parameterised. Nothing in the unit suite can check that —
    /// it queries in-memory arrays. If EF cannot translate the predicate it either throws or
    /// silently evaluates on the client, and if it parameterises the numeric enum values
    /// instead of the converted strings the query returns nothing at all. Both failures are
    /// invisible until a real provider is involved.</para>
    /// </summary>
    [Collection(AssociationIntegrationCollection.Name)]
    public sealed class AssociationCollectionReadTests : IDisposable
    {
        private readonly AssociationQueryBroker broker;
        private readonly List<Association> seededAssociations;

        // the fixture is built once for the whole collection and injected — the database is
        // created once, and disposed (and dropped) once, by xUnit rather than by a static
        public AssociationCollectionReadTests(AssociationQueryBroker broker)
        {
            this.broker = broker;
            this.seededAssociations = new List<Association>();
        }

        [Fact]
        public async Task ShouldTranslateTheReviewableSetsToSqlAsync()
        {
            // given: a caller holding both a coarse and a narrow scoped role, so both the
            // entity-type set and the content-type set are non-empty and both reach the
            // expression tree
            this.broker.ActAs(
                actorUserId: Guid.NewGuid().ToString(),
                Roles.TagReviewer,
                "ContentItem-Testimony-Reviewer");

            // when
            IQueryable<Association> query =
                await this.broker.AssociationService.RetrieveAllAssociationsAsync(
                    CancellationToken.None);

            string sql = query.ToQueryString();

            // then: reaching a SQL string at all is the first half of the proof — a predicate
            // EF cannot translate throws here rather than producing one
            sql.Should().NotBeNullOrWhiteSpace();
            sql.Should().Contain("SELECT");

            // The enum columns are mapped HasConversion<string>(), so the parameters SQL sees
            // must be the member NAMES. If EF parameterised the underlying numbers the
            // predicate would silently match nothing.
            //
            // Asserting on the bare words "Tag" and "Testimony" would not show that: both
            // appear in the SELECT projection as part of column names. The assertion has to
            // name the parameter DECLARATION, which only exists if the value was passed as a
            // string.
            sql.Should().Contain("nvarchar(32) = N'Tag'",
                because: "the reviewable entity type is parameterised as its name, not its number");

            sql.Should().Contain("nvarchar(32) = N'Testimony'",
                because: "the reviewable content type is parameterised as its name, not its number");

            // The role predicate itself must be server-side. Asserting on a projected column
            // proves nothing — every column is in the SELECT list regardless — so this looks
            // for the role clauses in the WHERE.
            sql.Should().Contain("[a].[EntityAType] =",
                because: "the endpoint-A role clause is evaluated by the server");

            sql.Should().Contain("[a].[EntityBType] =",
                because: "the endpoint-B role clause is evaluated by the server");

            // EF must emit its own null guard around the nullable enum rather than relying on
            // C# short-circuit ordering, which SQL does not guarantee
            sql.Should().Contain("[a].[EntityAContentType] IS NOT NULL");
            sql.Should().Contain("[a].[EntityBContentType] IS NOT NULL");
        }

        [Fact]
        public async Task ShouldProduceValidSqlWhenTheCallerHasNoScopedRolesAsync()
        {
            // given: both sets resolve empty. An empty IN (...) is invalid SQL, so this is the
            // case most likely to produce something the server rejects at runtime.
            this.broker.ActAs(actorUserId: Guid.NewGuid().ToString());

            Association nonPublicAssociation = CreateAssociation(
                entityAType: EntityType.ContentItem,
                entityAContentType: ContentType.Testimony,
                entityBType: EntityType.Tag,
                isPublished: false,
                createdBy: Guid.NewGuid().ToString());

            await SeedAsync(nonPublicAssociation);

            // when
            IQueryable<Association> query =
                await this.broker.AssociationService.RetrieveAllAssociationsAsync(
                    CancellationToken.None);

            List<Association> actualAssociations = await query.ToListAsync();

            // then: it executes, and degrades to exactly the public-plus-own predicate
            actualAssociations.Should().NotContain(association =>
                association.Id == nonPublicAssociation.Id);
        }

        [Fact]
        public async Task ShouldReturnRowsMatchingTheCoarseTierOnEitherEndpointAsync()
        {
            // given: canonical ordering decides which endpoint lands on A, so the filter has
            // to match on both sides. This seeds one row reachable through the B side and one
            // that is not reachable at all.
            string actorUserId = Guid.NewGuid().ToString();
            this.broker.ActAs(actorUserId, Roles.TagReviewer);

            Association reachableAssociation = CreateAssociation(
                entityAType: EntityType.ContentItem,
                entityAContentType: ContentType.Story,
                entityBType: EntityType.Tag,
                isPublished: false,
                createdBy: Guid.NewGuid().ToString());

            Association unreachableAssociation = CreateAssociation(
                entityAType: EntityType.Comment,
                entityAContentType: null,
                entityBType: EntityType.Link,
                isPublished: false,
                createdBy: Guid.NewGuid().ToString());

            await SeedAsync(reachableAssociation, unreachableAssociation);

            // when
            IQueryable<Association> query =
                await this.broker.AssociationService.RetrieveAllAssociationsAsync(
                    CancellationToken.None);

            List<Association> actualAssociations = await query.ToListAsync();

            // then
            actualAssociations.Should().Contain(association =>
                association.Id == reachableAssociation.Id);

            actualAssociations.Should().NotContain(association =>
                association.Id == unreachableAssociation.Id);
        }

        [Fact]
        public async Task ShouldReturnRowsMatchingTheNarrowTierAndNotOtherContentTypesAsync()
        {
            // given: the narrow tier is the half that depends on a nullable enum being
            // dereferenced inside the expression tree — the construct most likely to fail
            // translation. A reviewer for testimonies must see testimonies and not stories.
            string actorUserId = Guid.NewGuid().ToString();
            this.broker.ActAs(actorUserId, "ContentItem-Testimony-Reviewer");

            Association testimonyAssociation = CreateAssociation(
                entityAType: EntityType.ContentItem,
                entityAContentType: ContentType.Testimony,
                entityBType: EntityType.Reaction,
                isPublished: false,
                createdBy: Guid.NewGuid().ToString());

            Association storyAssociation = CreateAssociation(
                entityAType: EntityType.ContentItem,
                entityAContentType: ContentType.Story,
                entityBType: EntityType.Reaction,
                isPublished: false,
                createdBy: Guid.NewGuid().ToString());

            await SeedAsync(testimonyAssociation, storyAssociation);

            // when
            IQueryable<Association> query =
                await this.broker.AssociationService.RetrieveAllAssociationsAsync(
                    CancellationToken.None);

            List<Association> actualAssociations = await query.ToListAsync();

            // then
            actualAssociations.Should().Contain(association =>
                association.Id == testimonyAssociation.Id);

            actualAssociations.Should().NotContain(association =>
                association.Id == storyAssociation.Id);
        }

        private async ValueTask SeedAsync(params Association[] associations)
        {
            await this.broker.InsertAsync(associations);
            this.seededAssociations.AddRange(associations);
        }

        private static Association CreateAssociation(
            EntityType entityAType,
            ContentType? entityAContentType,
            EntityType entityBType,
            bool isPublished,
            string createdBy)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = entityAType,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = Guid.NewGuid(),
                EntityAScope = Scope.AllVersions,
                EntityAContentType = entityAContentType,
                EntityBType = entityBType,
                EntityBKeyId = Guid.NewGuid(),
                EntityBGroupId = Guid.NewGuid(),
                EntityBScope = Scope.AllVersions,
                EntityBContentType = null,
                ApprovalStatus = ApprovalStatus.Draft,
                IsPublished = isPublished,
                IsDeleted = false,
                CreatedBy = createdBy,
                CreatedWhen = now,
                UpdatedBy = createdBy,
                UpdatedWhen = now
            };
        }

        // each test clears only the rows it seeded; the fixture itself outlives the test and
        // is disposed by xUnit at the end of the collection
        public void Dispose() =>
            this.broker.ClearAsync(this.seededAssociations).AsTask().GetAwaiter().GetResult();
    }
}
