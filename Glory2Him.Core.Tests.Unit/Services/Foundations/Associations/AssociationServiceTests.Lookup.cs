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

        // The same logical pair with its two endpoints swapped. A caller cannot replicate the
        // canonical order (it is an ordinal type-name / SqlGuid comparison), so a reversed-order
        // request is a natural, common input the probe must still match to the canonical stored row.
        private static Association ReverseEndpoints(Association association)
        {
            Association reversed = association.DeepClone();

            (reversed.EntityAType, reversed.EntityBType) =
                (association.EntityBType, association.EntityAType);

            (reversed.EntityAKeyId, reversed.EntityBKeyId) =
                (association.EntityBKeyId, association.EntityAKeyId);

            (reversed.EntityAGroupId, reversed.EntityBGroupId) =
                (association.EntityBGroupId, association.EntityAGroupId);

            (reversed.EntityAScope, reversed.EntityBScope) =
                (association.EntityBScope, association.EntityAScope);

            (reversed.EntityAContentType, reversed.EntityBContentType) =
                (association.EntityBContentType, association.EntityAContentType);

            return reversed;
        }

        // Stubs the pair probe and records the key the service asked it for. The endpoints the
        // storage layer is handed are the CANONICAL ones - normalisation stayed in the service
        // when the predicate moved down - so capturing them is how the reversed-order tests below
        // state their case.
        private sealed record PairProbeKey(
            EntityType EntityAType,
            EntityType EntityBType,
            Guid EntityAEffectiveId,
            Guid EntityBEffectiveId,
            string UserId);

        private PairProbeKey? capturedPairProbeKey;

        private void SetupPairProbe(Association match)
        {
            this.capturedPairProbeKey = null;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByPairAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((
                            EntityType entityAType,
                            EntityType entityBType,
                            Guid entityAEffectiveId,
                            Guid entityBEffectiveId,
                            string userId,
                            CancellationToken _) =>
                        {
                            this.capturedPairProbeKey = new PairProbeKey(
                                entityAType,
                                entityBType,
                                entityAEffectiveId,
                                entityBEffectiveId,
                                userId);

                            return match;
                        });
        }

        /// <summary>
        /// What the service still owns: the key it composes, and the projection it returns.
        ///
        /// <para>WHICH ROW the key selects — that the probe is unfiltered so a tombstone and
        /// another user's pending row both surface, and that a live row wins over a soft-deleted
        /// one — is a predicate in <c>IStorageBroker.SelectAssociationByPairAsync</c> now, proved
        /// against real SQL in <c>AssociationNarrowReadTests</c>. It had to move for the probe to
        /// be awaited with the caller's token rather than executed synchronously.</para>
        /// </summary>
        [Fact]
        public async Task ShouldProbeTheResolvedPairAndProjectTheRowAsync()
        {
            // given
            Association pairRequest = CreateResolvedPairRequest();

            Association storageRow = CreateStoredRowForPair(
                pairRequest, ApprovalStatus.Submitted, isDeleted: false);

            SetupPairProbe(storageRow);

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

            // and the key was composed from the request's own endpoints, resolved to the
            // EFFECTIVE ids the persisted computed columns carry
            this.capturedPairProbeKey.Should().NotBeNull();
            this.capturedPairProbeKey!.EntityAType.Should().Be(pairRequest.EntityAType);
            this.capturedPairProbeKey.EntityBType.Should().Be(pairRequest.EntityBType);
            this.capturedPairProbeKey.EntityAEffectiveId.Should().Be(pairRequest.EntityAGroupId);
            this.capturedPairProbeKey.EntityBEffectiveId.Should().Be(pairRequest.EntityBKeyId);
            this.capturedPairProbeKey.UserId.Should().Be(pairRequest.UserId);
        }

        [Fact]
        public async Task ShouldReturnNullWhenThePairIsUnoccupiedAsync()
        {
            // given: nothing occupies the key
            Association pairRequest = CreateResolvedPairRequest();
            SetupPairProbe(match: null);

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindAssociationByPairAsync(
                    pairRequest,
                    TestContext.Current.CancellationToken);

            // then: null, not an error — the caller inserts on a free pair
            actualMatch.Should().BeNull();
        }

        /// <summary>
        /// The projection carries the PROVENANCE of a soft-deleted row — CreatedBy and DeletedBy —
        /// so the resurrect rule can tell its own withdrawal from a moderator takedown.
        /// </summary>
        [Fact]
        public async Task ShouldProjectTheProvenanceOfASoftDeletedRowAsync()
        {
            // given
            Association pairRequest = CreateResolvedPairRequest();

            Association deletedRow = CreateStoredRowForPair(
                pairRequest, ApprovalStatus.Draft, isDeleted: true);

            deletedRow.DeletedBy = $"moderator-{Guid.NewGuid()}";

            SetupPairProbe(deletedRow);

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

        /// <summary>
        /// NORMALISATION IS STILL THE SERVICE'S, which is why this test stayed here when the
        /// predicate left. A caller cannot replicate the canonical order (it is an ordinal
        /// type-name / SqlGuid comparison), so a reversed-order request is an ordinary input; if
        /// the service did not canonicalise before asking, the storage layer would be handed a key
        /// no stored row carries — and against a takedown row, whose unique index filters
        /// WHERE IsDeleted = 0, the insert that followed would launder it.
        ///
        /// <para>Stated as an equality between the two keys rather than against a predicted
        /// canonical order, so the test does not restate the ordering rule it is checking.</para>
        /// </summary>
        [Fact]
        public async Task ShouldProbeTheSameKeyWhenTheRequestEndpointsAreReversedAsync()
        {
            // given
            Association canonicalRequest = CreateResolvedPairRequest();
            Association reversedRequest = ReverseEndpoints(canonicalRequest);

            Association storageRow = CreateStoredRowForPair(
                canonicalRequest, ApprovalStatus.Approved, isDeleted: false);

            SetupPairProbe(storageRow);

            // when
            await this.associationService.FindAssociationByPairAsync(
                canonicalRequest,
                TestContext.Current.CancellationToken);

            PairProbeKey? canonicalProbeKey = this.capturedPairProbeKey;

            SetupPairProbe(storageRow);

            AssociationPairMatch? reversedMatch =
                await this.associationService.FindAssociationByPairAsync(
                    reversedRequest,
                    TestContext.Current.CancellationToken);

            // then: the same key both times, so the reversed request finds the canonical row
            canonicalProbeKey.Should().NotBeNull();
            this.capturedPairProbeKey.Should().Be(canonicalProbeKey);

            reversedMatch.Should().NotBeNull();
            reversedMatch!.Id.Should().Be(storageRow.Id);
        }

        /// <summary>
        /// UserId is part of the key, and the EDITORIAL row carries none. Passing it through
        /// rather than defaulting it is what keeps one person's association out of another's
        /// probe — and a null that became an empty string would key on a value no row holds.
        /// </summary>
        [Fact]
        public async Task ShouldCarryTheRequestUserIdIntoTheProbeIncludingWhenItIsAbsentAsync()
        {
            // given
            Association editorialRequest = CreateResolvedPairRequest();
            editorialRequest.UserId = null;

            Association editorialRow = CreateStoredRowForPair(
                editorialRequest, ApprovalStatus.Approved, isDeleted: false);

            SetupPairProbe(editorialRow);

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindAssociationByPairAsync(
                    editorialRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().NotBeNull();
            actualMatch!.Id.Should().Be(editorialRow.Id);

            this.capturedPairProbeKey.Should().NotBeNull();
            this.capturedPairProbeKey!.UserId.Should().BeNull();
        }

        /// <summary>
        /// The token reaches the database call — the point of the narrow read.
        /// </summary>
        [Fact]
        public async Task ShouldPassTheCancellationTokenToTheStorageBrokerOnFindByPairAsync()
        {
            // given
            Association pairRequest = CreateResolvedPairRequest();
            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            SetupPairProbe(match: null);

            // when
            await this.associationService.FindAssociationByPairAsync(
                pairRequest,
                inputCancellationToken);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByPairAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<string>(), inputCancellationToken),
                Times.Once);
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
                broker.SelectAssociationByPairAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
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
                broker.SelectAssociationByPairAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
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
                broker.SelectAssociationByPairAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnFindAssociationByPairIfCancellationRequestedAsync()
        {
            // given
            Association pairRequest = CreateResolvedPairRequest();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<AssociationPairMatch?> findTask =
                this.associationService.FindAssociationByPairAsync(
                    pairRequest,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(findTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByPairAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // pins WHERE the guard sits, not merely that it exists. The operation mints an
            // envelope before it reads anything, so a guard that drifted below that await would
            // still surface OperationCanceledException and still satisfy the storage assertion
            // above — this is the assertion that catches the drift.
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }
    }
}
