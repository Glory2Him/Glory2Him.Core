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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveTheCallersOwnReactionOnEachItemAsync()
        {
            // given
            string callerUserId = GetRandomString();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            List<Guid> contentItemGroupIds = CreateRandomContentItemGroupIds(count: 3);

            Association firstReaction =
                CreateCallerReactionOn(contentItemGroupIds[0], callerUserId);

            // hosted on one version only, so the id asked for is its effective id and its
            // group id is not among those asked: a read keyed on the group would miss it
            Association secondReaction =
                CreateCallerReactionOnOneVersion(contentItemGroupIds[2], callerUserId);

            var storageAssociations = new List<Association>
            {
                firstReaction,
                secondReaction
            };

            var expectedPairKeys = new List<AssociationPairKey>
            {
                CreatePairKeyFor(firstReaction),
                CreatePairKeyFor(secondReaction)
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(callerUserId);

            SetupSelectAssociationsToQuery(storageAssociations, cancellationToken);

            // when
            IReadOnlyList<AssociationPairKey> actualPairKeys =
                await this.associationService.RetrieveCallerContentItemReactionsAsync(
                    contentItemGroupIds,
                    cancellationToken);

            // then
            actualPairKeys.Should().BeEquivalentTo(expectedPairKeys);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            VerifySelectAssociationsQueriedOnce(cancellationToken);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveTheCallersReactionWhateverItsApprovalAsync()
        {
            // given
            string callerUserId = GetRandomString();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            List<Guid> contentItemGroupIds = CreateRandomContentItemGroupIds(count: 2);

            Association submittedReaction =
                CreateCallerReactionOn(contentItemGroupIds[0], callerUserId);

            submittedReaction.ApprovalStatus = ApprovalStatus.Submitted;
            submittedReaction.IsPublished = false;
            submittedReaction.PublishDate = null;

            Association notYetPublishedReaction =
                CreateCallerReactionOn(contentItemGroupIds[1], callerUserId);

            notYetPublishedReaction.ApprovalStatus = ApprovalStatus.Approved;
            notYetPublishedReaction.IsPublished = true;

            notYetPublishedReaction.PublishDate =
                currentDateTime.AddDays(GetRandomNumber());

            var storageAssociations = new List<Association>
            {
                submittedReaction,
                notYetPublishedReaction
            };

            var expectedPairKeys = new List<AssociationPairKey>
            {
                CreatePairKeyFor(submittedReaction),
                CreatePairKeyFor(notYetPublishedReaction)
            };

            // the clock answers, so a read that asked the publish date would drop the row
            // rather than fail on an unstubbed call
            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(callerUserId);

            SetupSelectAssociationsToQuery(storageAssociations, cancellationToken);

            // when
            IReadOnlyList<AssociationPairKey> actualPairKeys =
                await this.associationService.RetrieveCallerContentItemReactionsAsync(
                    contentItemGroupIds,
                    cancellationToken);

            // then
            actualPairKeys.Should().BeEquivalentTo(expectedPairKeys);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            VerifySelectAssociationsQueriedOnce(cancellationToken);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldLeaveOutTheCallersWithdrawnReactionAsync()
        {
            // given
            string callerUserId = GetRandomString();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            List<Guid> contentItemGroupIds = CreateRandomContentItemGroupIds(count: 2);

            Association liveReaction =
                CreateCallerReactionOn(contentItemGroupIds[0], callerUserId);

            Association withdrawnReaction =
                CreateCallerReactionOn(contentItemGroupIds[1], callerUserId);

            withdrawnReaction.IsDeleted = true;
            withdrawnReaction.DeletedBy = callerUserId;
            withdrawnReaction.DeletedWhen = withdrawnReaction.UpdatedWhen;

            var storageAssociations = new List<Association>
            {
                liveReaction,
                withdrawnReaction
            };

            var expectedPairKeys = new List<AssociationPairKey>
            {
                CreatePairKeyFor(liveReaction)
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(callerUserId);

            SetupSelectAssociationsToQuery(storageAssociations, cancellationToken);

            // when
            IReadOnlyList<AssociationPairKey> actualPairKeys =
                await this.associationService.RetrieveCallerContentItemReactionsAsync(
                    contentItemGroupIds,
                    cancellationToken);

            // then
            actualPairKeys.Should().BeEquivalentTo(expectedPairKeys);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            VerifySelectAssociationsQueriedOnce(cancellationToken);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveOnlyTheCallersReactionsOnTheHostsAskedForAsync()
        {
            // given
            string callerUserId = GetRandomString();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            List<Guid> contentItemGroupIds = CreateRandomContentItemGroupIds(count: 2);

            Association matchingReaction =
                CreateCallerReactionOn(contentItemGroupIds[0], callerUserId);

            // each row below misses on exactly one term of the condition
            Association reactionOnAHostNotAskedFor =
                CreateCallerReactionOn(Guid.NewGuid(), callerUserId);

            Association reactionOnAHostThatIsNotAContentItem =
                CreateCallerReactionOn(contentItemGroupIds[1], callerUserId);

            reactionOnAHostThatIsNotAContentItem.EntityAType = EntityType.Attachment;
            reactionOnAHostThatIsNotAContentItem.EntityAContentType = null;

            Association pairingWithAFarEndThatIsNotAReaction =
                CreateCallerReactionOn(contentItemGroupIds[1], callerUserId);

            pairingWithAFarEndThatIsNotAReaction.EntityBType = EntityType.Tag;

            var storageAssociations = new List<Association>
            {
                matchingReaction,
                reactionOnAHostNotAskedFor,
                reactionOnAHostThatIsNotAContentItem,
                pairingWithAFarEndThatIsNotAReaction
            };

            var expectedPairKeys = new List<AssociationPairKey>
            {
                CreatePairKeyFor(matchingReaction)
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(callerUserId);

            SetupSelectAssociationsToQuery(storageAssociations, cancellationToken);

            // when
            IReadOnlyList<AssociationPairKey> actualPairKeys =
                await this.associationService.RetrieveCallerContentItemReactionsAsync(
                    contentItemGroupIds,
                    cancellationToken);

            // then
            actualPairKeys.Should().BeEquivalentTo(expectedPairKeys);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            VerifySelectAssociationsQueriedOnce(cancellationToken);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNeverRetrieveAnotherReadersReactionAsync()
        {
            // given
            string callerUserId = GetRandomString();
            string otherReaderUserId = GetRandomString();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            List<Guid> contentItemGroupIds = CreateRandomContentItemGroupIds(count: 1);

            Association callersReaction =
                CreateCallerReactionOn(contentItemGroupIds[0], callerUserId);

            // created by the caller but keyed to the other reader: the row is the other
            // reader's, so a read keyed on its author would hand it to the caller
            Association otherReadersReaction =
                CreateCallerReactionOn(contentItemGroupIds[0], otherReaderUserId);

            otherReadersReaction.CreatedBy = callerUserId;
            otherReadersReaction.UpdatedBy = callerUserId;

            var storageAssociations = new List<Association>
            {
                callersReaction,
                otherReadersReaction
            };

            var expectedPairKeys = new List<AssociationPairKey>
            {
                CreatePairKeyFor(callersReaction)
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(callerUserId);

            SetupSelectAssociationsToQuery(storageAssociations, cancellationToken);

            // when
            IReadOnlyList<AssociationPairKey> actualPairKeys =
                await this.associationService.RetrieveCallerContentItemReactionsAsync(
                    contentItemGroupIds,
                    cancellationToken);

            // then
            actualPairKeys.Should().BeEquivalentTo(expectedPairKeys);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            VerifySelectAssociationsQueriedOnce(cancellationToken);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldRetrieveNothingForAnAnonymousCallerAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            List<Guid> contentItemGroupIds = CreateRandomContentItemGroupIds(count: 2);

            // when
            IReadOnlyList<AssociationPairKey> actualPairKeys =
                await this.associationService.RetrieveCallerContentItemReactionsAsync(
                    contentItemGroupIds,
                    cancellationToken);

            // then
            actualPairKeys.Should().BeEmpty();

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveNothingForAnEmptyListOfItemsAsync()
        {
            // given
            string callerUserId = GetRandomString();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var emptyContentItemGroupIds = new List<Guid>();

            // the caller's own rows, so the function storage is handed has rows to leave out
            var storageAssociations = new List<Association>
            {
                CreateCallerReactionOn(Guid.NewGuid(), callerUserId),
                CreateCallerReactionOn(Guid.NewGuid(), callerUserId)
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(callerUserId);

            SetupSelectAssociationsToQuery(storageAssociations, cancellationToken);

            // when
            IReadOnlyList<AssociationPairKey> actualPairKeys =
                await this.associationService.RetrieveCallerContentItemReactionsAsync(
                    emptyContentItemGroupIds,
                    cancellationToken);

            // then
            actualPairKeys.Should().BeEmpty();

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            VerifySelectAssociationsQueriedOnce(cancellationToken);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private static List<Guid> CreateRandomContentItemGroupIds(int count) =>
            Enumerable.Range(start: 0, count: count)
                .Select(_ => Guid.NewGuid())
                .ToList();

        // a reader's reaction as storage would hand it back: hosted on the content item's
        // group, so its effective id on endpoint A is that group id
        private static Association CreateCallerReactionOn(
            Guid contentItemGroupId,
            string readerUserId)
        {
            Association reaction = CreateRandomReaction(readerUserId);
            reaction.EntityAGroupId = contentItemGroupId;

            return WithDatabaseComputedEffectiveIds(reaction);
        }

        // a reader's reaction on a single version of a content item: its effective id on
        // endpoint A is that version's id, and its group id is a different one
        private static Association CreateCallerReactionOnOneVersion(
            Guid contentItemId,
            string readerUserId)
        {
            Association reaction = CreateRandomReaction(readerUserId);
            reaction.EntityAScope = Scope.ThisVersionOnly;
            reaction.EntityAKeyId = contentItemId;
            reaction.EntityAGroupId = Guid.NewGuid();

            return WithDatabaseComputedEffectiveIds(reaction);
        }

        private static AssociationPairKey CreatePairKeyFor(Association reaction) =>
            new AssociationPairKey
            {
                EntityAEffectiveId = reaction.EntityAEffectiveId,
                EntityBKeyId = reaction.EntityBKeyId
            };

        // the condition is the service's, so the mock executes whatever function it is handed
        // over the seeded rows (§ARC12.2.1 rule 5): receiving a function proves nothing
        private void SetupSelectAssociationsToQuery(
            List<Association> storageAssociations,
            CancellationToken cancellationToken) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<AssociationPairKey>>>(),
                    cancellationToken))
                        .Returns((
                            Func<IQueryable<Association>, IQueryable<AssociationPairKey>> query,
                            CancellationToken _) =>
                            new ValueTask<IReadOnlyList<AssociationPairKey>>(
                                query(storageAssociations.AsQueryable()).ToList()));

        private void VerifySelectAssociationsQueriedOnce(CancellationToken cancellationToken) =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<AssociationPairKey>>>(),
                    cancellationToken),
                Times.Once);
    }
}
