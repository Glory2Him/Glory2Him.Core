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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        // A ContentItem (versioned) endpoint in group G paired with a Tag. The ContentItem's scope
        // and key id are parameters so a test can build an AllVersions row and a ThisVersionOnly
        // row that share the group and the tag — the shape a mixed-scope overlap takes.
        private static Association CreateContentItemTagPair(
            Guid contentItemGroupId,
            Guid contentItemKeyId,
            Scope contentItemScope,
            Guid tagKeyId)
        {
            Association association = CreateRandomAssociation();
            association.EntityAType = EntityType.ContentItem;
            association.EntityAContentType = ContentType.Story;
            association.EntityAKeyId = contentItemKeyId;
            association.EntityAGroupId = contentItemGroupId;
            association.EntityAScope = contentItemScope;
            association.EntityBType = EntityType.Tag;
            association.EntityBContentType = null;
            association.EntityBKeyId = tagKeyId;
            association.EntityBGroupId = tagKeyId;
            association.EntityBScope = Scope.ThisVersionOnly;
            association.UserId = null;

            return association;
        }

        // Both endpoints VERSIONED (ContentItem and Link) so each side's scope and key id can be
        // set independently — the shape needed to exercise the coverage-intersection clause on the
        // B endpoint, not just the A endpoint. (ContentItem sorts before Link, so this is already
        // canonical.)
        private static Association CreateContentItemLinkPair(
            Guid contentItemGroupId,
            Scope contentItemScope,
            Guid linkGroupId,
            Guid linkKeyId,
            Scope linkScope)
        {
            Association association = CreateRandomAssociation();
            association.EntityAType = EntityType.ContentItem;
            association.EntityAContentType = ContentType.Story;
            association.EntityAKeyId = Guid.NewGuid();
            association.EntityAGroupId = contentItemGroupId;
            association.EntityAScope = contentItemScope;
            association.EntityBType = EntityType.Link;
            association.EntityBContentType = null;
            association.EntityBKeyId = linkKeyId;
            association.EntityBGroupId = linkGroupId;
            association.EntityBScope = linkScope;
            association.UserId = null;

            return association;
        }

        [Fact]
        public async Task ShouldReturnOverlapWhenAllVersionsRequestSpansAThisVersionOnlyRowAsync()
        {
            // given: a stored row pins the ContentItem side to one version (ThisVersionOnly), and
            // the request covers the whole group (AllVersions). They cover the same version, so
            // they would render the same pairing twice — an overlap the unique index cannot see.
            Guid groupG = Guid.NewGuid();
            Guid tagT = Guid.NewGuid();

            Association storedRequest = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.ThisVersionOnly, tagT);

            Association storedRow = CreateStoredRowForPair(
                storedRequest, ApprovalStatus.Approved, isDeleted: false);

            Association incoming = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagT);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { storedRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().NotBeNull();
            actualMatch!.Id.Should().Be(storedRow.Id);
        }

        [Fact]
        public async Task ShouldReturnOverlapWhenThisVersionOnlyRequestFallsInsideAnAllVersionsRowAsync()
        {
            // given: the reverse direction — the stored row spans the whole group (AllVersions) and
            // the request pins one version (ThisVersionOnly). The AllVersions row already covers
            // that version.
            Guid groupG = Guid.NewGuid();
            Guid tagT = Guid.NewGuid();

            Association storedRequest = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagT);

            Association storedRow = CreateStoredRowForPair(
                storedRequest, ApprovalStatus.Submitted, isDeleted: false);

            Association incoming = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.ThisVersionOnly, tagT);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { storedRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().NotBeNull();
            actualMatch!.Id.Should().Be(storedRow.Id);
        }

        [Fact]
        public async Task ShouldNotFlagTwoThisVersionOnlyRowsOnDifferentVersionsAsOverlapAsync()
        {
            // given: both the stored row and the request pin the ContentItem side to a version, but
            // DIFFERENT versions of the same group. Other versions do not inherit, so these are
            // legal and must NOT be reported as an overlap — the over-block the probe must avoid.
            Guid groupG = Guid.NewGuid();
            Guid tagT = Guid.NewGuid();

            Association storedRequest = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.ThisVersionOnly, tagT);

            Association storedRow = CreateStoredRowForPair(
                storedRequest, ApprovalStatus.Approved, isDeleted: false);

            Association incoming = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.ThisVersionOnly, tagT);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { storedRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().BeNull();
        }

        [Fact]
        public async Task ShouldNotFlagTwoThisVersionOnlyRowsOnDifferentVersionsOfTheBEndpointAsync()
        {
            // given: the A endpoints overlap (both AllVersions in the same ContentItem group), but
            // the B endpoints — both versioned Links in ONE group — pin DIFFERENT versions. Overlap
            // needs BOTH endpoints to intersect, so this is legal. This is the B-side mirror of the
            // A-side over-block: it isolates the coverage clause on endpoint B, which the Tag-on-B
            // fixtures never exercise (a Tag's effective id always equals its group).
            Guid contentItemGroup = Guid.NewGuid();
            Guid linkGroup = Guid.NewGuid();

            Association storedRequest = CreateContentItemLinkPair(
                contentItemGroup, Scope.AllVersions,
                linkGroup, linkKeyId: Guid.NewGuid(), Scope.ThisVersionOnly);

            Association storedRow = CreateStoredRowForPair(
                storedRequest, ApprovalStatus.Approved, isDeleted: false);

            Association incoming = CreateContentItemLinkPair(
                contentItemGroup, Scope.AllVersions,
                linkGroup, linkKeyId: Guid.NewGuid(), Scope.ThisVersionOnly);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { storedRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().BeNull();
        }

        [Fact]
        public async Task ShouldNotFlagOverlapWhenTheStoredRowBelongsToADifferentUserAsync()
        {
            // given: a row that would fully overlap the request except it carries a different
            // UserId (a per-user reaction row vs an editorial, user-less request). Overlap is
            // partitioned by user — the same pairing held by two different users is not a
            // double-render — so it must not be flagged.
            Guid groupG = Guid.NewGuid();
            Guid tagT = Guid.NewGuid();

            Association storedRequest = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.ThisVersionOnly, tagT);

            Association storedRow = CreateStoredRowForPair(
                storedRequest, ApprovalStatus.Approved, isDeleted: false);
            storedRow.UserId = $"user-{Guid.NewGuid()}";

            Association incoming = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagT);
            incoming.UserId = null;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { storedRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().BeNull();
        }

        [Fact]
        public async Task ShouldNotFlagOverlapWhenOnlyOneEndpointSharesAGroupAsync()
        {
            // given: the ContentItem side overlaps (both AllVersions in group G) but the tag differs,
            // so it is a DIFFERENT pair, not a double-render. Overlap requires BOTH endpoints.
            Guid groupG = Guid.NewGuid();

            Association storedRequest = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagKeyId: Guid.NewGuid());

            Association storedRow = CreateStoredRowForPair(
                storedRequest, ApprovalStatus.Approved, isDeleted: false);

            Association incoming = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagKeyId: Guid.NewGuid());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { storedRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().BeNull();
        }

        [Fact]
        public async Task ShouldIgnoreASoftDeletedOverlappingRowAsync()
        {
            // given: only a soft-deleted row overlaps. A deleted row does not render, so it cannot
            // double-render — the probe considers live rows only.
            Guid groupG = Guid.NewGuid();
            Guid tagT = Guid.NewGuid();

            Association storedRequest = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.ThisVersionOnly, tagT);

            Association deletedRow = CreateStoredRowForPair(
                storedRequest, ApprovalStatus.Approved, isDeleted: true);

            Association incoming = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagT);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { deletedRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().BeNull();
        }

        [Fact]
        public async Task ShouldExcludeTheRowUnderModificationFromItsOwnOverlapCheckAsync()
        {
            // given: the only overlapping row IS the row being modified, so excluding it leaves
            // nothing — a row never overlaps itself.
            Guid groupG = Guid.NewGuid();
            Guid tagT = Guid.NewGuid();

            Association storedRequest = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.ThisVersionOnly, tagT);

            Association storedRow = CreateStoredRowForPair(
                storedRequest, ApprovalStatus.Approved, isDeleted: false);

            Association incoming = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagT);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { storedRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: storedRow.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().BeNull();
        }

        [Fact]
        public async Task ShouldDetectOverlapWhenTheRequestEndpointsAreReversedAsync()
        {
            // given: stored rows are canonical; the request arrives with its endpoints the other
            // way round. An orientation-sensitive probe would miss the overlap.
            Guid groupG = Guid.NewGuid();
            Guid tagT = Guid.NewGuid();

            Association storedRequest = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.ThisVersionOnly, tagT);

            Association storedRow = CreateStoredRowForPair(
                storedRequest, ApprovalStatus.Approved, isDeleted: false);

            Association reversedIncoming = ReverseEndpoints(
                CreateContentItemTagPair(
                    groupG, contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagT));

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { storedRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    reversedIncoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().NotBeNull();
            actualMatch!.Id.Should().Be(storedRow.Id);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindOverlapIfAssociationIsNullAsync()
        {
            // given
            Association nullAssociation = null;

            var nullAssociationException =
                new NullAssociationException(message: "Content item association is null.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: nullAssociationException);

            // when
            ValueTask<AssociationPairMatch?> findTask =
                this.associationService.FindOverlappingAssociationAsync(
                    nullAssociation,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(findTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnFindOverlapIfCallerIsGloballyBlockedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);

            Association incoming = CreateContentItemTagPair(
                Guid.NewGuid(), contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagKeyId: Guid.NewGuid());

            // when
            ValueTask<AssociationPairMatch?> findTask =
                this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<AssociationValidationException>(findTask.AsTask);

            // then: the blocked caller never reaches the store
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindOverlapIfAnEndpointIsUnresolvedAsync()
        {
            // given: an unresolved endpoint (Guid.Empty key) would key the check off nothing
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association incoming = CreateContentItemTagPair(
                Guid.NewGuid(), contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagKeyId: Guid.NewGuid());
            incoming.EntityBKeyId = Guid.Empty;

            // when
            ValueTask<AssociationPairMatch?> findTask =
                this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<AssociationValidationException>(findTask.AsTask);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
