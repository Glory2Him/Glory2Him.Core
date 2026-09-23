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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        /// <summary>
        /// Both versioned endpoint types, both scopes, both sides — eight cases, matching what
        /// the collection read's own scope theory already covers.
        ///
        /// <para>The by-id side used to be a two-case theory over <c>ContentItem</c> on side A,
        /// and that is why deleting the <c>Link</c> arm of the by-id predicate outright, or
        /// pinning the B endpoint's scope to <c>ThisVersionOnly</c> — the exact Q7 defect, on
        /// side B — left the entire suite green. Neither is a hypothetical shape:
        /// <c>EntityType.Link</c> sorts after every other endpoint type and
        /// <c>EntityTypeVersioning</c> defaults it to <c>AllVersions</c>, so a versioned end on
        /// side B at <c>AllVersions</c> is a <c>Link</c> association's ordinary shape.</para>
        /// </summary>
        public static TheoryData<EntityType, Scope, bool> VersionedEndpointByIdCases()
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
        [MemberData(nameof(VersionedEndpointByIdCases))]
        public async Task ShouldReturnTheAssociationOnRetrieveByIdWhenBothEndpointsAreVisibleAsync(
            EntityType versionedType,
            Scope versionedEndpointScope,
            bool versionedEndpointIsOnSideA)
        {
            // given: the by-id read resolves its two endpoints DIRECTLY rather than composing —
            // it holds one row, so there is nothing here for a query composition to save. What it
            // does NOT vary is which row it asks about: an AllVersions endpoint is answered at its
            // group and a ThisVersionOnly endpoint at its row, on either side and for either
            // versioned type (§SEC14.3, two resolvers and one predicate).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var versionedKeyId = Guid.NewGuid();
            var versionedGroupId = Guid.NewGuid();
            var partnerId = Guid.NewGuid();

            EntityType partnerType = versionedEndpointIsOnSideA
                ? EntityType.Tag
                : EntityType.Comment;

            Association storedAssociation = BuildTestedEndpointAssociation(
                versionedType,
                versionedKeyId,
                versionedGroupId,
                partnerType,
                partnerId,
                versionedEndpointIsOnSideA,
                testedScope: versionedEndpointScope);

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storedAssociation);

            SetupVersionedEndpointRead(
                versionedType, versionedEndpointScope, versionedKeyId, versionedGroupId,
                isVisible: true);

            SetupNonVersionedEndpointRead(partnerType, partnerId, isVisible: true);

            // when
            Association actualAssociation =
                await this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeSameAs(storedAssociation);

            this.associationServiceMock.Verify(service =>
                service.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifyVersionedEndpointRead(
                versionedType, versionedEndpointScope, versionedKeyId, versionedGroupId);

            // the non-versioned partner is answered at its row whatever its scope column says
            VerifyNonVersionedEndpointRead(partnerType, partnerId);

            // VerifyNoOtherCalls is what makes the scope branch load-bearing here: a resolver
            // that read BOTH the group and the row would satisfy the verify above and fail this.
            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.commentServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // ── endpoint stubs, by the read the endpoint's scope makes the service ask for ──

        private void SetupVersionedEndpointRead(
            EntityType versionedType,
            Scope scope,
            Guid keyId,
            Guid groupId,
            bool isVisible)
        {
            if (versionedType == EntityType.ContentItem)
            {
                var contentItem = new ContentItem
                {
                    Id = keyId,
                    GroupId = groupId,

                    // set explicitly: every default-constructed ContentItem shares one content
                    // type, so leaving it alone would prove nothing about a content-typed end
                    ContentType = ContentType.Story,
                };

                if (scope == Scope.AllVersions)
                {
                    this.contentItemServiceMock.Setup(service =>
                        service.RetrieveContentItemsByGroupIdAsync(
                            groupId, It.IsAny<CancellationToken>()))
                                .ReturnsAsync(isVisible
                                    ? new List<ContentItem> { contentItem }
                                    : new List<ContentItem>());

                    return;
                }

                var contentItemByIdSetup = this.contentItemServiceMock.Setup(service =>
                    service.RetrieveContentItemByIdAsync(keyId, It.IsAny<CancellationToken>()));

                if (isVisible)
                {
                    contentItemByIdSetup.ReturnsAsync(contentItem);
                }
                else
                {
                    contentItemByIdSetup.ThrowsAsync(new ContentItemValidationException(
                        message: "not found", innerException: new Xeption()));
                }

                return;
            }

            var link = new Link { Id = keyId, GroupId = groupId };

            if (scope == Scope.AllVersions)
            {
                this.linkServiceMock.Setup(service =>
                    service.RetrieveLinksByGroupIdAsync(groupId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(isVisible
                            ? new List<Link> { link }
                            : new List<Link>());

                return;
            }

            var linkByIdSetup = this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(keyId, It.IsAny<CancellationToken>()));

            if (isVisible)
            {
                linkByIdSetup.ReturnsAsync(link);
            }
            else
            {
                linkByIdSetup.ThrowsAsync(new LinkValidationException(
                    message: "not found", innerException: new Xeption()));
            }
        }

        private void SetupNonVersionedEndpointRead(
            EntityType nonVersionedType,
            Guid endpointId,
            bool isVisible)
        {
            if (nonVersionedType == EntityType.Tag)
            {
                var tagSetup = this.tagServiceMock.Setup(service =>
                    service.RetrieveTagByIdAsync(endpointId, It.IsAny<CancellationToken>()));

                if (isVisible)
                {
                    tagSetup.ReturnsAsync(new Tag { Id = endpointId });
                }
                else
                {
                    tagSetup.ThrowsAsync(new TagValidationException(
                        message: "not found", innerException: new Xeption()));
                }

                return;
            }

            var commentSetup = this.commentServiceMock.Setup(service =>
                service.RetrieveCommentByIdAsync(endpointId, It.IsAny<CancellationToken>()));

            if (isVisible)
            {
                commentSetup.ReturnsAsync(new Comment { Id = endpointId });
            }
            else
            {
                commentSetup.ThrowsAsync(new CommentValidationException(
                    message: "not found", innerException: new Xeption()));
            }
        }

        private void VerifyNonVersionedEndpointRead(EntityType nonVersionedType, Guid endpointId)
        {
            if (nonVersionedType == EntityType.Tag)
            {
                this.tagServiceMock.Verify(service =>
                    service.RetrieveTagByIdAsync(endpointId, It.IsAny<CancellationToken>()),
                    Times.Once);

                return;
            }

            this.commentServiceMock.Verify(service =>
                service.RetrieveCommentByIdAsync(endpointId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyVersionedEndpointRead(
            EntityType versionedType,
            Scope scope,
            Guid keyId,
            Guid groupId)
        {
            if (versionedType == EntityType.ContentItem && scope == Scope.AllVersions)
            {
                this.contentItemServiceMock.Verify(service =>
                    service.RetrieveContentItemsByGroupIdAsync(
                        groupId, It.IsAny<CancellationToken>()),
                    Times.Once);

                return;
            }

            if (versionedType == EntityType.ContentItem)
            {
                this.contentItemServiceMock.Verify(service =>
                    service.RetrieveContentItemByIdAsync(keyId, It.IsAny<CancellationToken>()),
                    Times.Once);

                return;
            }

            if (scope == Scope.AllVersions)
            {
                this.linkServiceMock.Verify(service =>
                    service.RetrieveLinksByGroupIdAsync(groupId, It.IsAny<CancellationToken>()),
                    Times.Once);

                return;
            }

            this.linkServiceMock.Verify(service =>
                service.RetrieveLinkByIdAsync(keyId, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
