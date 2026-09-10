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

        /// <summary>
        /// Both tiers reaching the expression tree at once, asserted on the ROWS that come back.
        ///
        /// <para>This replaced a test that read <c>ToQueryString()</c> and matched substrings of
        /// the generated SQL — <c>= N'Tag'</c>, <c>[a].[EntityAType] =</c>, and the rest (#486).
        /// Those assertions gated the build on EF's SQL formatting, which changes across provider
        /// upgrades with no change in behaviour. Everything they were reaching for is visible in
        /// the result set instead: a predicate EF cannot translate still throws before any row is
        /// returned, and enum values parameterised as numbers rather than the
        /// <c>HasConversion&lt;string&gt;()</c> names would match nothing, so the reachable rows
        /// below would be missing.</para>
        ///
        /// <para>Five rows, because the narrow tier is FOUR conjuncts over two endpoints and a
        /// row set that leaves any of them unexercised proves less than the SQL strings did.
        /// The B-side branch needs a row whose only route in is <c>EntityBContentType</c> —
        /// canonical ordering decides which endpoint a content item lands on, so the B side is
        /// not a mirror that can be assumed. And the two endpoint-TYPE conjuncts need a row
        /// carrying a content type on endpoints that are not content items, on BOTH sides —
        /// the service refuses to write such a pair, but no check constraint does (see the
        /// note on <c>ResolveReviewableContentTypes</c>), so the column can hold it and the
        /// query is what must not be fooled by it.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnRowsFromBothTiersWhenTheCallerHoldsCoarseAndNarrowRolesAsync()
        {
            // given: a caller holding both a coarse and a narrow scoped role, so both the
            // entity-type set and the content-type set are non-empty and both reach the
            // expression tree
            string actorUserId = Guid.NewGuid().ToString();

            this.broker.ActAs(
                actorUserId,
                Roles.TagReviewers,
                "ContentItem-Testimony-Reviewers");

            // in through the coarse tier, on the B endpoint
            Association coarseReachableAssociation = CreateAssociation(
                entityAType: EntityType.ContentItem,
                entityAContentType: ContentType.Story,
                entityBType: EntityType.Tag,
                isPublished: false,
                createdBy: Guid.NewGuid().ToString());

            // in through the narrow tier, on the A endpoint
            Association narrowEndpointAReachableAssociation = CreateAssociation(
                entityAType: EntityType.ContentItem,
                entityAContentType: ContentType.Testimony,
                entityBType: EntityType.Reaction,
                isPublished: false,
                createdBy: Guid.NewGuid().ToString());

            // in through the narrow tier, on the B endpoint — its ONLY route in, so this dies
            // if the B-side branch stops translating or reads the wrong column
            Association narrowEndpointBReachableAssociation = CreateAssociation(
                entityAType: EntityType.Comment,
                entityAContentType: null,
                entityBType: EntityType.ContentItem,
                isPublished: false,
                createdBy: Guid.NewGuid().ToString(),
                entityBContentType: ContentType.Testimony);

            // the B-side control: right endpoint type, wrong content type
            Association otherContentTypeOnEndpointBAssociation = CreateAssociation(
                entityAType: EntityType.Comment,
                entityAContentType: null,
                entityBType: EntityType.ContentItem,
                isPublished: false,
                createdBy: Guid.NewGuid().ToString(),
                entityBContentType: ContentType.Story);

            // a reviewable content type parked on BOTH endpoints, neither of them a content
            // item. Carrying it on one endpoint only would mutation-test one conjunct and
            // leave the other free: with the B column null, dropping
            // EntityBType == ContentItem changes no answer here, because the null still fails
            // the IS NOT NULL guard. Populated on both, this single row dies if EITHER
            // endpoint-type conjunct goes.
            Association contentTypeOnNonContentItemEndpointsAssociation = CreateAssociation(
                entityAType: EntityType.Comment,
                entityAContentType: ContentType.Testimony,
                entityBType: EntityType.Link,
                isPublished: false,
                createdBy: Guid.NewGuid().ToString(),
                entityBContentType: ContentType.Testimony);

            await SeedAsync(
                coarseReachableAssociation,
                narrowEndpointAReachableAssociation,
                narrowEndpointBReachableAssociation,
                otherContentTypeOnEndpointBAssociation,
                contentTypeOnNonContentItemEndpointsAssociation);

            // when
            IQueryable<Association> query =
                await this.broker.AssociationService.RetrieveAllAssociationsAsync(
                    CancellationToken.None);

            List<Association> actualAssociations =
                await query.ToListAsync(TestContext.Current.CancellationToken);

            // then: both tiers answer on both endpoints, and neither widens to everything
            actualAssociations.Should().Contain(association =>
                association.Id == coarseReachableAssociation.Id);

            actualAssociations.Should().Contain(association =>
                association.Id == narrowEndpointAReachableAssociation.Id);

            actualAssociations.Should().Contain(association =>
                association.Id == narrowEndpointBReachableAssociation.Id);

            actualAssociations.Should().NotContain(association =>
                association.Id == otherContentTypeOnEndpointBAssociation.Id);

            actualAssociations.Should().NotContain(association =>
                association.Id == contentTypeOnNonContentItemEndpointsAssociation.Id);
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

            List<Association> actualAssociations = await query.ToListAsync(TestContext.Current.CancellationToken);

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
            this.broker.ActAs(actorUserId, Roles.TagReviewers);

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

            List<Association> actualAssociations = await query.ToListAsync(TestContext.Current.CancellationToken);

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
            this.broker.ActAs(actorUserId, "ContentItem-Testimony-Reviewers");

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

            List<Association> actualAssociations = await query.ToListAsync(TestContext.Current.CancellationToken);

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

        // entityBContentType defaults to null because most cases only need the A side; the
        // B-side narrow tier is a separate branch of the filter and has to be able to set it
        private static Association CreateAssociation(
            EntityType entityAType,
            ContentType? entityAContentType,
            EntityType entityBType,
            bool isPublished,
            string createdBy,
            ContentType? entityBContentType = null)
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
                EntityBContentType = entityBContentType,
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
