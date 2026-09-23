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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    /// <summary>
    /// The two reads must agree. One evaluator does not by itself give this — the by-id path
    /// resolves its endpoints directly rather than composing, which is a statement about cost and
    /// not a licence for a second predicate — so the agreement is asserted rather than assumed.
    ///
    /// <para>Both tests below run against ONE world: one set of stored associations and one set of
    /// visible endpoints, stubbed once and handed to both paths. A fixture that described the
    /// endpoints twice could let the two paths disagree about what is visible and still pass,
    /// which is the whole failure mode under test.</para>
    ///
    /// <para>The worked case they exist to catch: an <c>AllVersions</c> <c>ContentItem</c> endpoint
    /// whose <c>EntityAKeyId</c> names a version that is no longer visible, in a group that still
    /// has a visible one. §DOM4.6 rule 1 makes the effective id the read predicate and §ARC16.8
    /// rules group level, so it belongs in the list — and it must open.</para>
    /// </summary>
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldOpenByIdEveryAssociationTheCollectionReadReturnsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            CrossPathWorld world = SetupCrossPathWorld();

            // when
            IQueryable<Association> collectionQuery =
                await this.associationOrchestrationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            List<Association> listedAssociations = collectionQuery.ToList();

            // then: the list is not vacuous, and every row in it opens
            listedAssociations.Should().NotBeEmpty();

            foreach (Association listedAssociation in listedAssociations)
            {
                Association openedAssociation =
                    await this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                        listedAssociation.Id,
                        TestContext.Current.CancellationToken);

                openedAssociation.Id.Should().Be(listedAssociation.Id);
            }

            // and the rows the fixture expects in the list are the rows that are in it, so a
            // composite that silently narrowed to nothing could not pass by opening none
            listedAssociations.Select(association => association.Id)
                .Should().BeEquivalentTo(world.ExpectedListedAssociationIds);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnRetrieveByIdForAnAssociationTheCollectionReadDropsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            CrossPathWorld world = SetupCrossPathWorld();

            IQueryable<Association> collectionQuery =
                await this.associationOrchestrationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            HashSet<Guid> listedAssociationIds =
                collectionQuery.Select(association => association.Id).ToHashSet();

            List<Association> droppedAssociations = world.StoredAssociations
                .Where(association => listedAssociationIds.Contains(association.Id) is false)
                .ToList();

            // then: something was dropped, and every dropped row answers not-found when opened
            droppedAssociations.Should().NotBeEmpty();

            foreach (Association droppedAssociation in droppedAssociations)
            {
                ValueTask<Association> retrieveTask =
                    this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                        droppedAssociation.Id,
                        TestContext.Current.CancellationToken);

                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    retrieveTask.AsTask);
            }
        }

        // ── the one world both paths are asked about ──────────────────────────────────

        private sealed class CrossPathWorld
        {
            public List<Association> StoredAssociations { get; } = new List<Association>();

            public List<Guid> ExpectedListedAssociationIds { get; } = new List<Guid>();
        }

        private CrossPathWorld SetupCrossPathWorld()
        {
            var world = new CrossPathWorld();
            var visibleEndpoints = new VisibleEndpointSets();

            // a version group with a visible later version and a superseded one that is not
            var contentItemGroupId = Guid.NewGuid();
            var visibleLaterVersionId = Guid.NewGuid();
            var supersededVersionId = Guid.NewGuid();
            visibleEndpoints.Add(EntityType.ContentItem, visibleLaterVersionId, contentItemGroupId);

            // a group with nothing visible at all
            var invisibleGroupId = Guid.NewGuid();
            var invisibleGroupVersionId = Guid.NewGuid();

            var visibleTagId = Guid.NewGuid();
            visibleEndpoints.Add(EntityType.Tag, visibleTagId, visibleTagId);

            var invisibleTagId = Guid.NewGuid();

            var visibleBibleReferenceId = Guid.NewGuid();
            visibleEndpoints.Add(
                EntityType.BibleReference, visibleBibleReferenceId, visibleBibleReferenceId);

            // a Link version group shaped like the ContentItem one above: one visible version,
            // one superseded, and a second group with nothing visible at all
            var linkGroupId = Guid.NewGuid();
            var visibleLinkVersionId = Guid.NewGuid();
            var supersededLinkVersionId = Guid.NewGuid();
            visibleEndpoints.Add(EntityType.Link, visibleLinkVersionId, linkGroupId);

            var invisibleLinkGroupId = Guid.NewGuid();
            var invisibleLinkVersionId = Guid.NewGuid();

            // 1. AllVersions on a version that IS visible — listed, openable
            Association allVersionsOnALiveVersion = BuildCrossPathAssociation(
                EntityType.ContentItem, visibleLaterVersionId, contentItemGroupId,
                Scope.AllVersions, EntityType.Tag, visibleTagId);

            // 2. AllVersions on a SUPERSEDED version whose group still has a visible one. This is
            //    the Q7 row: the group answers, so it is listed, and it must open. Resolving the
            //    by-id path on the key id instead reds both tests here.
            Association allVersionsOnASupersededVersion = BuildCrossPathAssociation(
                EntityType.ContentItem, supersededVersionId, contentItemGroupId,
                Scope.AllVersions, EntityType.Tag, visibleTagId);

            // 3. ThisVersionOnly on that same superseded version — its row is the predicate, and
            //    its row is not visible, so it drops on BOTH paths
            Association thisVersionOnlyOnASupersededVersion = BuildCrossPathAssociation(
                EntityType.ContentItem, supersededVersionId, contentItemGroupId,
                Scope.ThisVersionOnly, EntityType.Tag, visibleTagId);

            // 4. AllVersions on a group with nothing visible — drops on both paths
            Association allVersionsOnAnInvisibleGroup = BuildCrossPathAssociation(
                EntityType.ContentItem, invisibleGroupVersionId, invisibleGroupId,
                Scope.AllVersions, EntityType.Tag, visibleTagId);

            // 5. two non-versioned ends, both visible — listed, openable
            Association nonVersionedPair = BuildCrossPathAssociation(
                EntityType.BibleReference, visibleBibleReferenceId, visibleBibleReferenceId,
                Scope.ThisVersionOnly, EntityType.Tag, visibleTagId);

            // 6. a visible near end and an invisible far end — drops on both paths
            Association onAnInvisibleFarEnd = BuildCrossPathAssociation(
                EntityType.ContentItem, visibleLaterVersionId, contentItemGroupId,
                Scope.AllVersions, EntityType.Tag, invisibleTagId);

            // 7. THE SAME Q7 SHAPE ON SIDE B, AND ON THE OTHER VERSIONED TYPE. 'Link' sorts after
            //    every other endpoint type and EntityTypeVersioning defaults it to AllVersions,
            //    so a Link endpoint's ordinary shape is a versioned end on side B — which is
            //    exactly what rows 1-6 could not express. Its key id names a superseded version
            //    and its group still has a visible one, so the group answers, it is listed, and
            //    it must open.
            Association linkOnSideBAtAllVersionsOverASupersededVersion = BuildCrossPathAssociation(
                EntityType.ContentItem, visibleLaterVersionId, contentItemGroupId,
                Scope.AllVersions, EntityType.Link, supersededLinkVersionId,
                entityBGroupId: linkGroupId, entityBScope: Scope.AllVersions);

            // 8. the same end with nothing visible in its group — drops on both paths
            Association linkOnSideBAtAllVersionsOverAnInvisibleGroup = BuildCrossPathAssociation(
                EntityType.ContentItem, visibleLaterVersionId, contentItemGroupId,
                Scope.AllVersions, EntityType.Link, invisibleLinkVersionId,
                entityBGroupId: invisibleLinkGroupId, entityBScope: Scope.AllVersions);

            // 9. a versioned end on side B at ThisVersionOnly, naming a version that IS visible —
            //    listed and openable, and the control that stops case 7 passing for a resolver
            //    that simply ignored the B scope and always read the group
            Association linkOnSideBAtThisVersionOnly = BuildCrossPathAssociation(
                EntityType.ContentItem, visibleLaterVersionId, contentItemGroupId,
                Scope.AllVersions, EntityType.Link, visibleLinkVersionId,
                entityBGroupId: linkGroupId, entityBScope: Scope.ThisVersionOnly);

            // 10. a versioned end on side B at ThisVersionOnly naming a SUPERSEDED version whose
            //     group is visible — the row is the predicate, so it drops on both paths. Reds a
            //     resolver that answered a ThisVersionOnly B end at its group.
            Association linkOnSideBAtThisVersionOnlyOverASupersededVersion =
                BuildCrossPathAssociation(
                    EntityType.ContentItem, visibleLaterVersionId, contentItemGroupId,
                    Scope.AllVersions, EntityType.Link, supersededLinkVersionId,
                    entityBGroupId: linkGroupId, entityBScope: Scope.ThisVersionOnly);

            // 11. A NON-VERSIONED END CARRYING A STORED AllVersions SCOPE. Only a versioned type
            //     can legitimately be written AllVersions, so this is bad data — and both reads
            //     answer it at its ROW regardless, which is what the composite's Tag term does
            //     and what the by-id switch's default arm does. Nothing else pinned that, so the
            //     argument in Reads.cs was the only thing holding it.
            //
            //     ITS GROUP ID IS DELIBERATELY NOT ITS KEY ID. On a well-formed non-versioned
            //     endpoint the two are equal (§DOM4.5), which makes "answered at its row" and
            //     "answered at its group" the same call and pins nothing — a resolver that
            //     honoured the scope column here would pass unnoticed. Divergent ids are a second
            //     piece of bad data on a row that is bad data already, and they are what make the
            //     rule observable.
            Association nonVersionedEndCarryingAllVersions = BuildCrossPathAssociation(
                EntityType.BibleReference, visibleBibleReferenceId, visibleBibleReferenceId,
                Scope.ThisVersionOnly, EntityType.Tag, visibleTagId,
                entityBGroupId: Guid.NewGuid(), entityBScope: Scope.AllVersions);

            // 12. the same bad-data shape over a tag that is NOT visible — drops on both paths,
            //     so neither read can be said to have waved the row through on the scope column
            Association nonVersionedEndCarryingAllVersionsOverAnInvisibleRow =
                BuildCrossPathAssociation(
                    EntityType.BibleReference, visibleBibleReferenceId, visibleBibleReferenceId,
                    Scope.ThisVersionOnly, EntityType.Tag, invisibleTagId,
                    entityBGroupId: Guid.NewGuid(), entityBScope: Scope.AllVersions);

            world.StoredAssociations.AddRange(new[]
            {
                allVersionsOnALiveVersion,
                allVersionsOnASupersededVersion,
                thisVersionOnlyOnASupersededVersion,
                allVersionsOnAnInvisibleGroup,
                nonVersionedPair,
                onAnInvisibleFarEnd,
                linkOnSideBAtAllVersionsOverASupersededVersion,
                linkOnSideBAtAllVersionsOverAnInvisibleGroup,
                linkOnSideBAtThisVersionOnly,
                linkOnSideBAtThisVersionOnlyOverASupersededVersion,
                nonVersionedEndCarryingAllVersions,
                nonVersionedEndCarryingAllVersionsOverAnInvisibleRow,
            });

            world.ExpectedListedAssociationIds.AddRange(new[]
            {
                allVersionsOnALiveVersion.Id,
                allVersionsOnASupersededVersion.Id,
                nonVersionedPair.Id,
                linkOnSideBAtAllVersionsOverASupersededVersion.Id,
                linkOnSideBAtThisVersionOnly.Id,
                nonVersionedEndCarryingAllVersions.Id,
            });

            SetupVisibleAssociations(world.StoredAssociations.ToArray());

            foreach (Association storedAssociation in world.StoredAssociations)
            {
                this.associationServiceMock.Setup(service =>
                    service.RetrieveAssociationByIdAsync(
                        storedAssociation.Id,
                        It.IsAny<CancellationToken>()))
                            .ReturnsAsync(storedAssociation);
            }

            SetupEndpointReadsForBothPaths(visibleEndpoints);

            return world;
        }

        // Side B takes its own key id, group id and scope rather than being hard-wired to a
        // non-versioned row. It was hard-wired, and that is why every mutation on the B-side
        // branch of the by-id predicate stayed green: Link sorts after every other endpoint type
        // and defaults to AllVersions, so a Link endpoint's NORMAL shape is exactly the one this
        // fixture could not build.
        private static Association BuildCrossPathAssociation(
            EntityType entityAType,
            Guid entityAKeyId,
            Guid entityAGroupId,
            Scope entityAScope,
            EntityType entityBType,
            Guid entityBKeyId,
            Guid? entityBGroupId = null,
            Scope entityBScope = Scope.ThisVersionOnly) =>
            new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = entityAType,
                EntityAKeyId = entityAKeyId,
                EntityAGroupId = entityAGroupId,
                EntityAScope = entityAScope,
                EntityAContentType =
                    entityAType == EntityType.ContentItem ? ContentType.Story : null,
                EntityBType = entityBType,
                EntityBKeyId = entityBKeyId,
                EntityBGroupId = entityBGroupId ?? entityBKeyId,
                EntityBScope = entityBScope,
                EntityBContentType =
                    entityBType == EntityType.ContentItem ? ContentType.Story : null,
            };

        // ONE description of what is visible, driving every read both paths make: the six
        // collection reads the composite composes over, the group-keyed reads an AllVersions
        // endpoint is answered by, and the by-id reads everything else is answered by.
        //
        // An endpoint that is not in the set answers the way its own foundation answers — a
        // validation-shaped not-found from a by-id read, an empty slice from a group read — so
        // the stubs stand in for the posture rather than for a convenient result.
        private void SetupEndpointReadsForBothPaths(VisibleEndpointSets visibleEndpoints)
        {
            SetupEndpointCollectionReads(
                contentItems: visibleEndpoints.ContentItems,
                tags: visibleEndpoints.Tags,
                reactions: visibleEndpoints.Reactions,
                bibleReferences: visibleEndpoints.BibleReferences,
                comments: visibleEndpoints.Comments,
                links: visibleEndpoints.Links);

            // The broad "not visible" answers go in FIRST; Moq lets the later, narrower setups
            // win for the ids that ARE visible.
            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new ContentItemValidationException(
                            message: "not found", innerException: new Xeption()));

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemsByGroupIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Array.Empty<ContentItem>());

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new LinkValidationException(
                            message: "not found", innerException: new Xeption()));

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinksByGroupIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Array.Empty<Link>());

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new TagValidationException(
                            message: "not found", innerException: new Xeption()));

            this.reactionServiceMock.Setup(service =>
                service.RetrieveReactionByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new ReactionValidationException(
                            message: "not found", innerException: new Xeption()));

            this.commentServiceMock.Setup(service =>
                service.RetrieveCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new CommentValidationException(
                            message: "not found", innerException: new Xeption()));

            this.bibleReferenceServiceMock.Setup(service =>
                service.RetrieveBibleReferenceByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new BibleReferenceValidationException(
                            message: "not found", innerException: new Xeption()));

            foreach (ContentItem contentItem in visibleEndpoints.ContentItems)
            {
                this.contentItemServiceMock.Setup(service =>
                    service.RetrieveContentItemByIdAsync(
                        contentItem.Id,
                        It.IsAny<CancellationToken>()))
                            .ReturnsAsync(contentItem);
            }

            foreach (IGrouping<Guid, ContentItem> group in
                visibleEndpoints.ContentItems.GroupBy(contentItem => contentItem.GroupId))
            {
                List<ContentItem> groupContentItems = group.ToList();

                this.contentItemServiceMock.Setup(service =>
                    service.RetrieveContentItemsByGroupIdAsync(
                        group.Key,
                        It.IsAny<CancellationToken>()))
                            .ReturnsAsync(groupContentItems);
            }

            foreach (Link link in visibleEndpoints.Links)
            {
                this.linkServiceMock.Setup(service =>
                    service.RetrieveLinkByIdAsync(link.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(link);
            }

            foreach (IGrouping<Guid, Link> group in
                visibleEndpoints.Links.GroupBy(link => link.GroupId))
            {
                List<Link> groupLinks = group.ToList();

                this.linkServiceMock.Setup(service =>
                    service.RetrieveLinksByGroupIdAsync(group.Key, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(groupLinks);
            }

            foreach (Tag tag in visibleEndpoints.Tags)
            {
                this.tagServiceMock.Setup(service =>
                    service.RetrieveTagByIdAsync(tag.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(tag);
            }

            foreach (Reaction reaction in visibleEndpoints.Reactions)
            {
                this.reactionServiceMock.Setup(service =>
                    service.RetrieveReactionByIdAsync(reaction.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(reaction);
            }

            foreach (Comment comment in visibleEndpoints.Comments)
            {
                this.commentServiceMock.Setup(service =>
                    service.RetrieveCommentByIdAsync(comment.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(comment);
            }

            foreach (BibleReference bibleReference in visibleEndpoints.BibleReferences)
            {
                this.bibleReferenceServiceMock.Setup(service =>
                    service.RetrieveBibleReferenceByIdAsync(
                        bibleReference.Id,
                        It.IsAny<CancellationToken>()))
                            .ReturnsAsync(bibleReference);
            }
        }
    }
}
