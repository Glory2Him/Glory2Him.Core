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

        // Stubs the overlap probe and records the key the service asked it for.
        private sealed record OverlapProbeKey(
            EntityType EntityAType,
            EntityType EntityBType,
            string UserId,
            Guid EntityAGroupId,
            Guid EntityBGroupId,
            Scope EntityAScope,
            Scope EntityBScope,
            Guid EntityAEffectiveId,
            Guid EntityBEffectiveId,
            Guid? ExcludedAssociationId);

        private OverlapProbeKey? capturedOverlapProbeKey;

        private void SetupOverlapProbe(Association match)
        {
            this.capturedOverlapProbeKey = null;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectOverlappingAssociationAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Scope>(), It.IsAny<Scope>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((
                            EntityType entityAType,
                            EntityType entityBType,
                            string userId,
                            Guid entityAGroupId,
                            Guid entityBGroupId,
                            Scope entityAScope,
                            Scope entityBScope,
                            Guid entityAEffectiveId,
                            Guid entityBEffectiveId,
                            Guid? excludedAssociationId,
                            CancellationToken _) =>
                        {
                            this.capturedOverlapProbeKey = new OverlapProbeKey(
                                entityAType,
                                entityBType,
                                userId,
                                entityAGroupId,
                                entityBGroupId,
                                entityAScope,
                                entityBScope,
                                entityAEffectiveId,
                                entityBEffectiveId,
                                excludedAssociationId);

                            return match;
                        });
        }

        /// <summary>
        /// What the service still owns: the key it composes — including BOTH SCOPES, which the
        /// coverage-intersection clause needs and which no other probe passes — and the projection
        /// it returns.
        ///
        /// <para>The intersection rule itself moved into
        /// <c>IStorageBroker.SelectOverlappingAssociationAsync</c> so the probe could be awaited
        /// with the caller's token. Which rows it flags — an AllVersions request spanning a pinned
        /// row, a pinned request inside an AllVersions row, two pinned rows on DIFFERENT versions
        /// NOT overlapping, tombstones excluded, the row under modification excluded — is proved
        /// against real SQL in <c>AssociationNarrowReadTests</c>.</para>
        /// </summary>
        [Fact]
        public async Task ShouldProbeWithBothScopesAndProjectTheOverlappingRowAsync()
        {
            // given
            Guid groupG = Guid.NewGuid();
            Guid tagT = Guid.NewGuid();

            Association storedRequest = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.ThisVersionOnly, tagT);

            Association storedRow = CreateStoredRowForPair(
                storedRequest, ApprovalStatus.Approved, isDeleted: false);

            Association incoming = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagT);

            SetupOverlapProbe(storedRow);

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().NotBeNull();
            actualMatch!.Id.Should().Be(storedRow.Id);

            // and the key carries the request's own groups AND its scopes — dropping either scope
            // would collapse the coverage-intersection clause into plain id equality, which is the
            // mistake that would miss an AllVersions row entirely
            this.capturedOverlapProbeKey.Should().NotBeNull();
            this.capturedOverlapProbeKey!.EntityAType.Should().Be(incoming.EntityAType);
            this.capturedOverlapProbeKey.EntityBType.Should().Be(incoming.EntityBType);
            this.capturedOverlapProbeKey.EntityAGroupId.Should().Be(incoming.EntityAGroupId);
            this.capturedOverlapProbeKey.EntityBGroupId.Should().Be(incoming.EntityBGroupId);
            this.capturedOverlapProbeKey.EntityAScope.Should().Be(Scope.AllVersions);
            this.capturedOverlapProbeKey.EntityBScope.Should().Be(Scope.ThisVersionOnly);
            this.capturedOverlapProbeKey.UserId.Should().Be(incoming.UserId);
            this.capturedOverlapProbeKey.ExcludedAssociationId.Should().BeNull();
        }

        [Fact]
        public async Task ShouldReturnNullWhenNothingOverlapsAsync()
        {
            // given
            Association incoming = CreateContentItemTagPair(
                Guid.NewGuid(), contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, Guid.NewGuid());

            SetupOverlapProbe(match: null);

            // when
            AssociationPairMatch? actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().BeNull();
        }

        /// <summary>
        /// The row under modification must not overlap itself, and the id that says so is the
        /// caller's — so it has to survive the trip to the storage layer.
        /// </summary>
        [Fact]
        public async Task ShouldCarryTheExcludedAssociationIdIntoTheProbeAsync()
        {
            // given
            Association incoming = CreateContentItemTagPair(
                Guid.NewGuid(), contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, Guid.NewGuid());

            var excludedAssociationId = Guid.NewGuid();
            SetupOverlapProbe(match: null);

            // when
            await this.associationService.FindOverlappingAssociationAsync(
                incoming,
                excludedAssociationId,
                TestContext.Current.CancellationToken);

            // then
            this.capturedOverlapProbeKey.Should().NotBeNull();
            this.capturedOverlapProbeKey!.ExcludedAssociationId.Should().Be(excludedAssociationId);
        }

        /// <summary>
        /// NORMALISATION IS STILL THE SERVICE'S. Stored rows are canonical, so a reversed-order
        /// request that reached the storage layer unnormalised would be handed a key no row
        /// carries and would report no overlap — letting the double-render through.
        ///
        /// <para>Stated as an equality between the two keys rather than against a predicted
        /// canonical order, so the test does not restate the ordering rule it is checking.</para>
        /// </summary>
        [Fact]
        public async Task ShouldProbeTheSameOverlapKeyWhenTheRequestEndpointsAreReversedAsync()
        {
            // given
            Guid groupG = Guid.NewGuid();
            Guid tagT = Guid.NewGuid();

            Association canonicalIncoming = CreateContentItemTagPair(
                groupG, contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagT);

            Association reversedIncoming = ReverseEndpoints(canonicalIncoming);

            SetupOverlapProbe(match: null);

            // when
            await this.associationService.FindOverlappingAssociationAsync(
                canonicalIncoming,
                excludedAssociationId: null,
                TestContext.Current.CancellationToken);

            OverlapProbeKey? canonicalProbeKey = this.capturedOverlapProbeKey;

            SetupOverlapProbe(match: null);

            await this.associationService.FindOverlappingAssociationAsync(
                reversedIncoming,
                excludedAssociationId: null,
                TestContext.Current.CancellationToken);

            // then
            canonicalProbeKey.Should().NotBeNull();
            this.capturedOverlapProbeKey.Should().Be(canonicalProbeKey);
        }

        /// <summary>
        /// The token reaches the database call — the point of the narrow read.
        /// </summary>
        [Fact]
        public async Task ShouldPassTheCancellationTokenToTheStorageBrokerOnFindOverlapAsync()
        {
            // given
            Association incoming = CreateContentItemTagPair(
                Guid.NewGuid(), contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, Guid.NewGuid());

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            SetupOverlapProbe(match: null);

            // when
            await this.associationService.FindOverlappingAssociationAsync(
                incoming,
                excludedAssociationId: null,
                inputCancellationToken);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.SelectOverlappingAssociationAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Scope>(), It.IsAny<Scope>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    inputCancellationToken),
                Times.Once);
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
                broker.SelectOverlappingAssociationAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Scope>(), It.IsAny<Scope>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
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
                broker.SelectOverlappingAssociationAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Scope>(), It.IsAny<Scope>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
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
                broker.SelectOverlappingAssociationAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Scope>(), It.IsAny<Scope>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnFindOverlappingAssociationIfCancellationRequestedAsync()
        {
            // given
            Association incoming = CreateContentItemTagPair(
                Guid.NewGuid(), contentItemKeyId: Guid.NewGuid(), Scope.AllVersions, tagKeyId: Guid.NewGuid());

            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<AssociationPairMatch?> findTask =
                this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    excludedAssociationId: null,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(findTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectOverlappingAssociationAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Scope>(), It.IsAny<Scope>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // pins WHERE the guard sits, not merely that it exists. The operation mints an
            // envelope before it reads anything, so a guard that drifted below that await would
            // still surface OperationCanceledException and still satisfy the storage assertion
            // above — this is the assertion that catches the drift.
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }
    }
}
