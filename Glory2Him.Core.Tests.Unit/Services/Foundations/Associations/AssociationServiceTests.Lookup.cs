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
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        // A resolved association: valid endpoint types, non-empty key/group, a definite scope —
        // the shape the orchestration hands the probe after endpoint resolution.
        private static Association CreateResolvedPairRequest()
        {
            Association association = CreateRandomAssociation();
            association.EntityAType = EntityType.ContentItem;
            association.EntityAContentType = ContentType.Story;
            association.EntityAKeyId = Guid.NewGuid();
            association.EntityAGroupId = Guid.NewGuid();
            association.EntityAScope = Scope.AllVersions;
            association.EntityBType = EntityType.Tag;
            association.EntityBContentType = null;
            association.EntityBKeyId = Guid.NewGuid();
            association.EntityBGroupId = association.EntityBKeyId;
            association.EntityBScope = Scope.ThisVersionOnly;
            association.UserId = null;

            return association;
        }

        // A stored row occupying the same canonical pair as the request, carrying the effective
        // ids the database would have computed so the probe's column comparison matches it.
        private static Association CreateStoredRowForPair(
            Association pairRequest,
            ApprovalStatus approvalStatus,
            bool isDeleted)
        {
            Association storageRow = pairRequest.DeepClone();
            storageRow.Id = Guid.NewGuid();
            storageRow.ApprovalStatus = approvalStatus;
            storageRow.IsDeleted = isDeleted;
            storageRow.CreatedBy = $"author-{Guid.NewGuid()}";
            storageRow.DeletedBy = isDeleted ? storageRow.CreatedBy : null;

            return WithDatabaseComputedEffectiveIds(storageRow);
        }

        [Fact]
        public async Task ShouldReturnTheMatchWhenALiveRowOccupiesThePairAsync()
        {
            // given
            Association pairRequest = CreateResolvedPairRequest();

            Association storageRow = CreateStoredRowForPair(
                pairRequest, ApprovalStatus.Submitted, isDeleted: false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { storageRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindAssociationByPairAsync(
                    pairRequest,
                    TestContext.Current.CancellationToken);

            // then: a non-leaking projection of exactly the matched row, nothing else
            actualMatch.Should().NotBeNull();
            actualMatch!.Id.Should().Be(storageRow.Id);
            actualMatch.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            actualMatch.IsDeleted.Should().BeFalse();
            actualMatch.CreatedBy.Should().Be(storageRow.CreatedBy);
            actualMatch.DeletedBy.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldReturnNullWhenThePairIsUnoccupiedAsync()
        {
            // given: the store holds only a row for a DIFFERENT pair
            Association pairRequest = CreateResolvedPairRequest();

            Association otherPairRow = CreateStoredRowForPair(
                CreateResolvedPairRequest(), ApprovalStatus.Approved, isDeleted: false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { otherPairRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindAssociationByPairAsync(
                    pairRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().BeNull();
        }

        [Fact]
        public async Task ShouldSeeAnotherUsersPendingRowBecauseTheProbeIsUnfilteredAsync()
        {
            // given: a pending row belonging to a DIFFERENT author. The read posture hides it
            // from the current caller, so a visibility-filtered lookup would miss it and let the
            // duplicate through — the whole reason the probe reads the unfiltered store.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association pairRequest = CreateResolvedPairRequest();

            Association anotherUsersRow = CreateStoredRowForPair(
                pairRequest, ApprovalStatus.Submitted, isDeleted: false);
            anotherUsersRow.CreatedBy = $"someone-else-{Guid.NewGuid()}";

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { anotherUsersRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindAssociationByPairAsync(
                    pairRequest,
                    TestContext.Current.CancellationToken);

            // then: found despite belonging to another user
            actualMatch.Should().NotBeNull();
            actualMatch!.Id.Should().Be(anotherUsersRow.Id);
            actualMatch.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
        }

        [Fact]
        public async Task ShouldPreferTheLiveRowOverASoftDeletedOneOnTheSamePairAsync()
        {
            // given: both a soft-deleted row and a live row occupy the pair (the unique index
            // filters WHERE IsDeleted = 0, so a live row can coexist with deleted ones). The
            // probe must return the LIVE one — that is the row a resubmission collides with.
            Association pairRequest = CreateResolvedPairRequest();

            Association deletedRow = CreateStoredRowForPair(
                pairRequest, ApprovalStatus.Rejected, isDeleted: true);

            Association liveRow = CreateStoredRowForPair(
                pairRequest, ApprovalStatus.Approved, isDeleted: false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { deletedRow, liveRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindAssociationByPairAsync(
                    pairRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().NotBeNull();
            actualMatch!.Id.Should().Be(liveRow.Id);
            actualMatch.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldReturnTheSoftDeletedRowWithItsProvenanceWhenNoLiveRowExistsAsync()
        {
            // given: only a soft-deleted row occupies the pair. The probe returns it — including
            // its CreatedBy and DeletedBy — so the resurrect rule can decide whether to restore
            // (own row) or refuse (a moderator takedown).
            Association pairRequest = CreateResolvedPairRequest();

            Association deletedRow = CreateStoredRowForPair(
                pairRequest, ApprovalStatus.Draft, isDeleted: true);
            deletedRow.DeletedBy = $"moderator-{Guid.NewGuid()}";

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { deletedRow }.AsQueryable());

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindAssociationByPairAsync(
                    pairRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().NotBeNull();
            actualMatch!.Id.Should().Be(deletedRow.Id);
            actualMatch.IsDeleted.Should().BeTrue();
            actualMatch.CreatedBy.Should().Be(deletedRow.CreatedBy);
            actualMatch.DeletedBy.Should().Be(deletedRow.DeletedBy);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindByPairIfAssociationIsNullAsync()
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
                this.associationService.FindAssociationByPairAsync(
                    nullAssociation,
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
        public async Task ShouldThrowUnauthorizedOnFindByPairIfCallerIsGloballyBlockedAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);

            Association pairRequest = CreateResolvedPairRequest();

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is blocked from contributing content item associations.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            // when
            ValueTask<AssociationPairMatch?> findTask =
                this.associationService.FindAssociationByPairAsync(
                    pairRequest,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(findTask.AsTask);

            // then: the blocked caller never reaches the store
            actualException.Should().BeEquivalentTo(expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindByPairIfAnEndpointIsUnresolvedAsync()
        {
            // given: an unresolved endpoint (Guid.Empty key) would key the lookup off nothing
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association pairRequest = CreateResolvedPairRequest();
            pairRequest.EntityBKeyId = Guid.Empty;

            // when
            ValueTask<AssociationPairMatch?> findTask =
                this.associationService.FindAssociationByPairAsync(
                    pairRequest,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<AssociationValidationException>(findTask.AsTask);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
