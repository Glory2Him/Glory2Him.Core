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

            world.StoredAssociations.AddRange(new[]
            {
                allVersionsOnALiveVersion,
                allVersionsOnASupersededVersion,
                thisVersionOnlyOnASupersededVersion,
                allVersionsOnAnInvisibleGroup,
                nonVersionedPair,
                onAnInvisibleFarEnd,
            });

            world.ExpectedListedAssociationIds.AddRange(new[]
            {
                allVersionsOnALiveVersion.Id,
                allVersionsOnASupersededVersion.Id,
                nonVersionedPair.Id,
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

        private static Association BuildCrossPathAssociation(
            EntityType entityAType,
            Guid entityAKeyId,
            Guid entityAGroupId,
            Scope entityAScope,
            EntityType entityBType,
            Guid entityBKeyId) =>
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
                EntityBGroupId = entityBKeyId,
                EntityBScope = Scope.ThisVersionOnly,
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
