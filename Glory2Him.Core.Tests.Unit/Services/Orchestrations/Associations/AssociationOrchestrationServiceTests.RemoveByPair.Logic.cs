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
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Theory]
        [InlineData(EntityType.ContentItem, EntityType.Reaction)]
        [InlineData(EntityType.Reaction, EntityType.ContentItem)]
        public async Task ShouldRemoveTheReadersReactionByItsPairAsync(
            EntityType entityAType,
            EntityType entityBType)
        {
            // given: a signed-in reader holding Love on an item withdraws Love from it. Their row
            // is found by its pair, as the reader, and soft-deleted by its id. The pair is the
            // upsert's caller shape, so it may name the reaction on either endpoint (§ARC16.8.1).
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);
            Association removalRequest = CreateRawUpsertRequestBetween(entityAType, entityBType);
            SetupInboundEnvelopeFor(removalRequest);
            ContentItem resolvedContentItem = SetupMethodPathEndpointReads(removalRequest);

            Association expectedLookupPair =
                CreateResolvedPairFrom(removalRequest, resolvedContentItem, readerUserId);

            var readersRow = new PersonalAssociationMatch
            {
                Id = Guid.NewGuid(),
                EntityBKeyId = GetNamedReactionId(removalRequest),
                IsDeleted = false,
            };

            var withdrawnRow = new Association
            {
                Id = readersRow.Id,
                IsDeleted = true,
            };

            this.associationServiceMock.Setup(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(readersRow);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(withdrawnRow);

            var expectedResult = new AssociationRemovalResult
            {
                Status = AssociationRemovalStatus.Removed,
                AssociationId = readersRow.Id,
            };

            // when
            AssociationRemovalResult actualResult =
                await this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            // then: the reader's row is withdrawn, with no deletion reason, and answered Removed
            // with its id and nothing else
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(removalRequest),
                    Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    resolvedContentItem.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.reactionServiceMock.Verify(service =>
                service.RetrieveReactionByIdAsync(
                    GetNamedReactionId(removalRequest),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAnswerNothingToRemoveWhenTheReaderHoldsNoReactionAsync()
        {
            // given: the reader holds no reaction on the item, so the lookup finds no row
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            Association expectedLookupPair = SetupReadersWithdrawal(removalRequest, readerUserId);

            this.associationServiceMock.Setup(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((PersonalAssociationMatch)null);

            var expectedResult = new AssociationRemovalResult
            {
                Status = AssociationRemovalStatus.NothingToRemove,
                AssociationId = null,
            };

            // when
            AssociationRemovalResult actualResult =
                await this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            // then: nothing is removed, and no id is answered
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.associationServiceMock.Verify(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A signed-in reader's withdrawal: the envelope and both endpoint reads. Hands back the
        // pair the lookup is to be handed.
        private Association SetupReadersWithdrawal(Association removalRequest, string readerUserId)
        {
            SetupInboundEnvelopeFor(removalRequest);
            ContentItem resolvedContentItem = SetupMethodPathEndpointReads(removalRequest);

            return CreateResolvedPairFrom(removalRequest, resolvedContentItem, readerUserId);
        }

        // The reaction a withdrawal request names: the key of its Reaction endpoint, on whichever
        // side the request puts it.
        private static Guid GetNamedReactionId(Association removalRequest) =>
            removalRequest.EntityAType == EntityType.Reaction
                ? removalRequest.EntityAKeyId
                : removalRequest.EntityBKeyId;

        // A reader's reaction on an item as the withdrawal hands it to the lookup: the raw request
        // in the order it was sent, with the item at its group under AllVersions with its content
        // type, the reaction at its own id under ThisVersionOnly with none, and the reader's own
        // user id. Unlike the upsert's, it carries no status: a withdrawal writes none.
        private static Association CreateResolvedPairFrom(
            Association removalRequest,
            ContentItem resolvedContentItem,
            string readerUserId)
        {
            Association resolvedPair = removalRequest.DeepClone();
            bool isTheItemOnA = removalRequest.EntityAType == EntityType.ContentItem;

            resolvedPair.EntityAGroupId =
                isTheItemOnA ? resolvedContentItem.GroupId : removalRequest.EntityAKeyId;

            resolvedPair.EntityAContentType = isTheItemOnA ? resolvedContentItem.ContentType : null;
            resolvedPair.EntityAScope = isTheItemOnA ? Scope.AllVersions : Scope.ThisVersionOnly;

            resolvedPair.EntityBGroupId =
                isTheItemOnA ? removalRequest.EntityBKeyId : resolvedContentItem.GroupId;

            resolvedPair.EntityBContentType = isTheItemOnA ? null : resolvedContentItem.ContentType;
            resolvedPair.EntityBScope = isTheItemOnA ? Scope.ThisVersionOnly : Scope.AllVersions;
            resolvedPair.UserId = readerUserId;

            return resolvedPair;
        }
    }
}
