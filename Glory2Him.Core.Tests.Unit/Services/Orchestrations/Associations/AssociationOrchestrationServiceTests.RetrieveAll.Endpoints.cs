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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    /// <summary>
    /// The composite is twelve hand-written terms — six endpoint entity types on each of two
    /// sides — and canonical ordering decides which entity lands on which side, so neither side
    /// is a mirror of the other that can be assumed rather than asserted. Every one of the twelve
    /// is exercised here, each against its OWN entity's collection read, so a term dropped,
    /// inverted or pointed at the wrong queryable reds a case.
    /// </summary>
    public partial class AssociationOrchestrationServiceTests
    {
        public static TheoryData<EntityType, bool> EndpointTypeAndSideCases()
        {
            var cases = new TheoryData<EntityType, bool>();

            EntityType[] resolvableEndpointTypes =
            {
                EntityType.ContentItem,
                EntityType.Link,
                EntityType.Tag,
                EntityType.Reaction,
                EntityType.Comment,
                EntityType.BibleReference,
            };

            foreach (EntityType endpointType in resolvableEndpointTypes)
            {
                cases.Add(endpointType, true);
                cases.Add(endpointType, false);
            }

            return cases;
        }

        [Theory]
        [MemberData(nameof(EndpointTypeAndSideCases))]
        public async Task ShouldResolveEachEndpointTypeThroughItsOwnCollectionReadOnRetrieveAllAsync(
            EntityType endpointType,
            bool endpointIsOnSideA)
        {
            // given: the PARTNER end always resolves, so the end under test is the only thing
            // that can decide either row.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            EntityType partnerType =
                endpointType == EntityType.Tag ? EntityType.Comment : EntityType.Tag;

            var resolvableEndpointId = Guid.NewGuid();
            var resolvableEndpointGroupId = Guid.NewGuid();
            var unresolvableEndpointId = Guid.NewGuid();
            var unresolvableEndpointGroupId = Guid.NewGuid();
            var partnerEndpointId = Guid.NewGuid();

            Association survivingAssociation = BuildTestedEndpointAssociation(
                endpointType,
                resolvableEndpointId,
                resolvableEndpointGroupId,
                partnerType,
                partnerEndpointId,
                endpointIsOnSideA);

            Association droppedAssociation = BuildTestedEndpointAssociation(
                endpointType,
                unresolvableEndpointId,
                unresolvableEndpointGroupId,
                partnerType,
                partnerEndpointId,
                endpointIsOnSideA);

            SetupVisibleAssociations(survivingAssociation, droppedAssociation);

            var visibleEndpoints = new VisibleEndpointSets();
            visibleEndpoints.Add(endpointType, resolvableEndpointId, resolvableEndpointGroupId);
            visibleEndpoints.Add(partnerType, partnerEndpointId, partnerEndpointId);

            SetupEndpointCollectionReads(
                contentItems: visibleEndpoints.ContentItems,
                tags: visibleEndpoints.Tags,
                reactions: visibleEndpoints.Reactions,
                bibleReferences: visibleEndpoints.BibleReferences,
                comments: visibleEndpoints.Comments,
                links: visibleEndpoints.Links);

            // when
            IQueryable<Association> actualQuery =
                await this.associationOrchestrationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            List<Association> actualAssociations = actualQuery.ToList();

            // then
            actualAssociations.Should().ContainSingle()
                .Which.Id.Should().Be(survivingAssociation.Id);
        }

        public static TheoryData<EntityType, Scope, bool> VersionedEndpointScopeCases()
        {
            var cases = new TheoryData<EntityType, Scope, bool>();

            foreach (EntityType versionedType in new[] { EntityType.ContentItem, EntityType.Link })
            {
                foreach (Scope scope in new[] { Scope.AllVersions, Scope.ThisVersionOnly })
                {
                    cases.Add(versionedType, scope, true);
                    cases.Add(versionedType, scope, false);
                }
            }

            return cases;
        }

        [Theory]
        [MemberData(nameof(VersionedEndpointScopeCases))]
        public async Task ShouldMatchAnAllVersionsEndpointAtItsGroupAndAThisVersionOnlyEndpointAtItsRowOnRetrieveAllAsync(
            EntityType versionedType,
            Scope endpointScope,
            bool endpointIsOnSideA)
        {
            // given: the row visible to this caller is a LATER version of the same group than the
            // one the endpoint names. An AllVersions endpoint follows the group (§DOM4.3), so it
            // still resolves; a ThisVersionOnly endpoint names one row and that row is not
            // visible, so it does not. The two answers must differ, and they are what makes the
            // scope terms in the composite load-bearing rather than decorative.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var sharedGroupId = Guid.NewGuid();
            var namedVersionId = Guid.NewGuid();
            var visibleLaterVersionId = Guid.NewGuid();
            var partnerEndpointId = Guid.NewGuid();

            Association association = BuildTestedEndpointAssociation(
                versionedType,
                namedVersionId,
                sharedGroupId,
                EntityType.Tag,
                partnerEndpointId,
                endpointIsOnSideA,
                testedScope: endpointScope);

            SetupVisibleAssociations(association);

            var visibleEndpoints = new VisibleEndpointSets();
            visibleEndpoints.Add(versionedType, visibleLaterVersionId, sharedGroupId);
            visibleEndpoints.Add(EntityType.Tag, partnerEndpointId, partnerEndpointId);

            SetupEndpointCollectionReads(
                contentItems: visibleEndpoints.ContentItems,
                tags: visibleEndpoints.Tags,
                reactions: visibleEndpoints.Reactions,
                bibleReferences: visibleEndpoints.BibleReferences,
                comments: visibleEndpoints.Comments,
                links: visibleEndpoints.Links);

            // when
            IQueryable<Association> actualQuery =
                await this.associationOrchestrationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            List<Association> actualAssociations = actualQuery.ToList();

            // then
            if (endpointScope == Scope.AllVersions)
            {
                actualAssociations.Should().ContainSingle()
                    .Which.Id.Should().Be(association.Id);
            }
            else
            {
                actualAssociations.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task ShouldExcludeAnAssociationWhoseEndpointTypeHasNoCollectionReadOnRetrieveAllAsync()
        {
            // given: Attachment has no foundation service yet, and an association pointing at
            // another association is not a supported shape — so neither can be shown to be
            // visible through anything. §SEC14.5 rule 4 makes that a silent drop rather than an
            // error, so the surviving row is the only answer.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association survivingAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            var associationOnAnUnreadableType = new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = EntityType.Attachment,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = Guid.NewGuid(),
                EntityAScope = Scope.ThisVersionOnly,
                EntityBType = EntityType.Tag,
                EntityBKeyId = tagEndpoint.Id,
                EntityBGroupId = tagEndpoint.Id,
                EntityBScope = Scope.ThisVersionOnly,
            };

            SetupVisibleAssociations(survivingAssociation, associationOnAnUnreadableType);

            SetupEndpointCollectionReads(
                contentItems: new[] { contentItemEndpoint },
                tags: new[] { tagEndpoint });

            // when
            IQueryable<Association> actualQuery =
                await this.associationOrchestrationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            List<Association> actualAssociations = actualQuery.ToList();

            // then
            actualAssociations.Should().ContainSingle()
                .Which.Id.Should().Be(survivingAssociation.Id);
        }

        // ── fixtures ──────────────────────────────────────────────────────────────────

        // Places the endpoint under test on the requested side and the partner on the other,
        // deriving the scope the way the add path does unless the caller pins one: a versioned
        // type keys on its group under AllVersions, a non-versioned one on its own id under
        // ThisVersionOnly.
        private static Association BuildTestedEndpointAssociation(
            EntityType testedType,
            Guid testedKeyId,
            Guid testedGroupId,
            EntityType partnerType,
            Guid partnerId,
            bool testedIsOnSideA,
            Scope? testedScope = null)
        {
            bool isVersioned =
                testedType is EntityType.ContentItem or EntityType.Link;

            Scope resolvedTestedScope =
                testedScope ?? (isVersioned ? Scope.AllVersions : Scope.ThisVersionOnly);

            ContentType? testedContentType =
                testedType == EntityType.ContentItem ? ContentType.Story : null;

            ContentType? partnerContentType =
                partnerType == EntityType.ContentItem ? ContentType.Story : null;

            if (testedIsOnSideA)
            {
                return new Association
                {
                    Id = Guid.NewGuid(),
                    EntityAType = testedType,
                    EntityAKeyId = testedKeyId,
                    EntityAGroupId = testedGroupId,
                    EntityAScope = resolvedTestedScope,
                    EntityAContentType = testedContentType,
                    EntityBType = partnerType,
                    EntityBKeyId = partnerId,
                    EntityBGroupId = partnerId,
                    EntityBScope = Scope.ThisVersionOnly,
                    EntityBContentType = partnerContentType,
                };
            }

            return new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = partnerType,
                EntityAKeyId = partnerId,
                EntityAGroupId = partnerId,
                EntityAScope = Scope.ThisVersionOnly,
                EntityAContentType = partnerContentType,
                EntityBType = testedType,
                EntityBKeyId = testedKeyId,
                EntityBGroupId = testedGroupId,
                EntityBScope = resolvedTestedScope,
                EntityBContentType = testedContentType,
            };
        }

        // One list per endpoint entity, so a case can drop a row into the read that owns it and
        // leave every other read empty — which is what proves a term consults ITS OWN queryable
        // rather than a neighbour's.
        private sealed class VisibleEndpointSets
        {
            public List<ContentItem> ContentItems { get; } = new List<ContentItem>();

            public List<Link> Links { get; } = new List<Link>();

            public List<Tag> Tags { get; } = new List<Tag>();

            public List<Reaction> Reactions { get; } = new List<Reaction>();

            public List<Comment> Comments { get; } = new List<Comment>();

            public List<BibleReference> BibleReferences { get; } = new List<BibleReference>();

            public void Add(EntityType entityType, Guid endpointId, Guid groupId)
            {
                switch (entityType)
                {
                    case EntityType.ContentItem:
                        ContentItems.Add(new ContentItem
                        {
                            Id = endpointId,
                            GroupId = groupId,

                            // set explicitly: every default-constructed ContentItem shares one
                            // content type, so leaving it alone would prove nothing
                            ContentType = ContentType.Story,
                        });

                        break;

                    case EntityType.Link:
                        Links.Add(new Link { Id = endpointId, GroupId = groupId });
                        break;

                    case EntityType.Tag:
                        Tags.Add(new Tag { Id = endpointId });
                        break;

                    case EntityType.Reaction:
                        Reactions.Add(new Reaction { Id = endpointId });
                        break;

                    case EntityType.Comment:
                        Comments.Add(new Comment { Id = endpointId });
                        break;

                    case EntityType.BibleReference:
                        BibleReferences.Add(new BibleReference { Id = endpointId });
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(entityType),
                            entityType,
                            "No endpoint collection read exists for this entity type.");
                }
            }
        }
    }
}
