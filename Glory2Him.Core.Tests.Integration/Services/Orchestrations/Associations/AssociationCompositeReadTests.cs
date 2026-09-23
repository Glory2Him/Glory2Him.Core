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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Tests.Integration.Brokers;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Tests.Integration.Services.Orchestrations.Associations
{
    /// <summary>
    /// Proves §SEC14.3's composite survives translation to SQL Server — §ARC12.2.1 rule 6's case,
    /// and the one place a test in this solution sits below the exposer.
    ///
    /// <para>The evaluator splices each endpoint entity's own collection read into the association
    /// query as a correlated sub-query, so that the association queryable can be handed upward
    /// still unenumerated. Nothing above the broker can check that: the unit suite runs the same
    /// expression over in-memory arrays, where every shape "translates". Against a real provider
    /// the composition either becomes SQL, throws, or is silently evaluated on the client after
    /// materialising both tables — and the last of those is a production defect no assertion above
    /// this layer can see.</para>
    ///
    /// <para>Proven ONCE, for one endpoint pair. The mechanism is a correlated sub-query over a
    /// second entity's filtered read; repeating it per entity type would buy nothing and cost a
    /// round trip each.</para>
    /// </summary>
    [Collection(AssociationCompositeIntegrationCollection.Name)]
    public sealed class AssociationCompositeReadTests : IDisposable
    {
        private readonly AssociationCompositeQueryBroker broker;
        private readonly List<Association> seededAssociations;
        private readonly List<ContentItem> seededContentItems;
        private readonly List<Tag> seededTags;

        public AssociationCompositeReadTests(AssociationCompositeQueryBroker broker)
        {
            this.broker = broker;
            this.seededAssociations = new List<Association>();
            this.seededContentItems = new List<ContentItem>();
            this.seededTags = new List<Tag>();
        }

        [Fact]
        public async Task ShouldTranslateTheEndpointCompositeToSqlOnRetrieveAllAsync()
        {
            // given: a caller holding the global review roles, so each endpoint entity's own
            // collection read degrades to "every non-deleted row of my table". That is
            // deliberate — what is under test here is the JOIN of those reads into the
            // association query, not any one entity's own posture, which has its own coverage.
            string actorUserId = Guid.NewGuid().ToString();

            this.broker.ActAs(
                actorUserId,
                Roles.Administrators,
                Roles.Reviewers,
                Roles.Publishers);

            ContentItem liveContentItem = CreateContentItem(actorUserId);
            Tag liveTag = CreateTag(actorUserId, isDeleted: false);
            Tag softDeletedTag = CreateTag(actorUserId, isDeleted: true);

            await SeedAsync(liveContentItem);
            await SeedAsync(liveTag, softDeletedTag);

            // both endpoints resolvable — survives
            Association reachableAssociation = CreateAssociation(
                actorUserId,
                contentItemGroupId: liveContentItem.GroupId,
                contentItemKeyId: liveContentItem.Id,
                tagId: liveTag.Id);

            // the A endpoint names a group no row belongs to — §SEC14.3 rule 4 drops it
            Association associationOnAMissingEndpoint = CreateAssociation(
                actorUserId,
                contentItemGroupId: Guid.NewGuid(),
                contentItemKeyId: Guid.NewGuid(),
                tagId: liveTag.Id);

            // the B endpoint exists but is soft-deleted, so the Tag read refuses it to every
            // caller, Administrators included — §SEC14.3 rule 3 / §SEC14.5 rule 3
            Association associationOnASoftDeletedEndpoint = CreateAssociation(
                actorUserId,
                contentItemGroupId: liveContentItem.GroupId,
                contentItemKeyId: liveContentItem.Id,
                tagId: softDeletedTag.Id);

            await SeedAsync(
                reachableAssociation,
                associationOnAMissingEndpoint,
                associationOnASoftDeletedEndpoint);

            // when
            IQueryable<Association> query =
                await this.broker.AssociationOrchestrationService.RetrieveAllAssociationsAsync(
                    CancellationToken.None);

            // an untranslatable composition throws right here, before a single row comes back
            List<Association> actualAssociations =
                await query.ToListAsync(TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().Contain(association =>
                association.Id == reachableAssociation.Id);

            actualAssociations.Should().NotContain(association =>
                association.Id == associationOnAMissingEndpoint.Id);

            actualAssociations.Should().NotContain(association =>
                association.Id == associationOnASoftDeletedEndpoint.Id);
        }

        private static ContentItem CreateContentItem(string actorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new ContentItem
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                Version = 1,

                // set explicitly: every default-constructed ContentItem shares one content type,
                // so leaving it alone would prove nothing about a content-typed endpoint
                ContentType = ContentType.Story,
                Title = "seeded",
                Author = "seeded",
                Content = "seeded",
                ShareabilityBasis = ShareabilityBasis.Owned,
                SharePermission = string.Empty,
                ContentHash = Guid.NewGuid().ToString("N"),
                ApprovalStatus = ApprovalStatus.Draft,
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                DeletedBy = null,
                DeletedWhen = null,
            };
        }

        private static Tag CreateTag(string actorUserId, bool isDeleted)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new Tag
            {
                Id = Guid.NewGuid(),
                // the column caps at 30, so the uniqueness suffix is trimmed rather than
                // carrying a whole guid
                Name = $"seeded-{Guid.NewGuid():N}"[..20],
                ApprovalStatus = ApprovalStatus.Draft,
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                IsDeleted = isDeleted,
                DeletedBy = isDeleted ? actorUserId : null,
                DeletedWhen = isDeleted ? now : null,
            };
        }

        private static Association CreateAssociation(
            string actorUserId,
            Guid contentItemGroupId,
            Guid contentItemKeyId,
            Guid tagId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new Association
            {
                Id = Guid.NewGuid(),

                // 'ContentItem' sorts before 'Tag' ordinally, which is what
                // CK_Association_CanonicalOrder pins — the row could not be written the other
                // way round
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = contentItemKeyId,
                EntityAGroupId = contentItemGroupId,
                EntityAScope = Scope.AllVersions,
                EntityAContentType = ContentType.Story,
                EntityBType = EntityType.Tag,
                EntityBKeyId = tagId,
                EntityBGroupId = tagId,
                EntityBScope = Scope.ThisVersionOnly,
                ApprovalStatus = ApprovalStatus.Draft,
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                DeletedBy = null,
                DeletedWhen = null,
            };
        }

        private async Task SeedAsync(params ContentItem[] contentItems)
        {
            await this.broker.SeedAsync(contentItems);
            this.seededContentItems.AddRange(contentItems);
        }

        private async Task SeedAsync(params Tag[] tags)
        {
            await this.broker.SeedAsync(tags);
            this.seededTags.AddRange(tags);
        }

        private async Task SeedAsync(params Association[] associations)
        {
            await this.broker.SeedAsync(associations);
            this.seededAssociations.AddRange(associations);
        }

        public void Dispose() =>
            this.broker.ClearAsync(
                this.seededAssociations,
                this.seededContentItems,
                this.seededTags)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
    }
}
