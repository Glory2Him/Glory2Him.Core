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
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldCountEachReactionGivenToEachItemAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid firstContentItemGroupId = Guid.NewGuid();
            Guid secondContentItemGroupId = Guid.NewGuid();
            Guid loveReactionId = Guid.NewGuid();
            Guid joyReactionId = Guid.NewGuid();

            var storageAssociations = new List<Association>
            {
                CreateCountedReaction(firstContentItemGroupId, loveReactionId),
                CreateCountedReaction(firstContentItemGroupId, loveReactionId),
                CreateCountedReaction(firstContentItemGroupId, loveReactionId),
                CreateCountedReaction(firstContentItemGroupId, joyReactionId),
                CreateCountedReaction(secondContentItemGroupId, loveReactionId)
            };

            IReadOnlyList<Guid> inputContentItemGroupIds =
                new List<Guid> { firstContentItemGroupId, secondContentItemGroupId };

            IReadOnlyList<Guid> inputReactionIds =
                new List<Guid> { loveReactionId, joyReactionId };

            var expectedAssociationPairCounts = new List<AssociationPairCount>
            {
                CreateAssociationPairCount(firstContentItemGroupId, loveReactionId, count: 3),
                CreateAssociationPairCount(firstContentItemGroupId, joyReactionId, count: 1),
                CreateAssociationPairCount(secondContentItemGroupId, loveReactionId, count: 1)
            };

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupReactionCountReadOver(storageAssociations);

            // when
            IReadOnlyList<AssociationPairCount> actualAssociationPairCounts =
                await this.associationService.RetrieveContentItemReactionCountsAsync(
                    inputContentItemGroupIds,
                    inputReactionIds,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociationPairCounts.Should().BeEquivalentTo(expectedAssociationPairCounts);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            VerifyReactionCountReadAskedOnce();

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldCountOnlyVisibleReactionRowsAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid contentItemGroupId = Guid.NewGuid();
            Guid reactionId = Guid.NewGuid();

            // counted: no publish date, a publish date at the current moment, and one before it
            Association unscheduledReaction =
                CreateCountedReaction(contentItemGroupId, reactionId);

            Association dueNowReaction = CreateCountedReaction(contentItemGroupId, reactionId);
            dueNowReaction.PublishDate = randomDateTimeOffset;

            Association pastReaction = CreateCountedReaction(contentItemGroupId, reactionId);
            pastReaction.PublishDate = randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            // not counted: each misses exactly one term of §SEC14.3 rules 1, 2 and 5
            Association softDeletedReaction =
                CreateCountedReaction(contentItemGroupId, reactionId);

            softDeletedReaction.IsDeleted = true;

            Association submittedReaction = CreateCountedReaction(contentItemGroupId, reactionId);
            submittedReaction.ApprovalStatus = ApprovalStatus.Submitted;

            Association futureReaction = CreateCountedReaction(contentItemGroupId, reactionId);
            futureReaction.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            var storageAssociations = new List<Association>
            {
                unscheduledReaction,
                dueNowReaction,
                pastReaction,
                softDeletedReaction,
                submittedReaction,
                futureReaction
            };

            IReadOnlyList<Guid> inputContentItemGroupIds = new List<Guid> { contentItemGroupId };
            IReadOnlyList<Guid> inputReactionIds = new List<Guid> { reactionId };

            var expectedAssociationPairCounts = new List<AssociationPairCount>
            {
                CreateAssociationPairCount(contentItemGroupId, reactionId, count: 3)
            };

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupReactionCountReadOver(storageAssociations);

            // when
            IReadOnlyList<AssociationPairCount> actualAssociationPairCounts =
                await this.associationService.RetrieveContentItemReactionCountsAsync(
                    inputContentItemGroupIds,
                    inputReactionIds,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociationPairCounts.Should().BeEquivalentTo(expectedAssociationPairCounts);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            VerifyReactionCountReadAskedOnce();

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldCountOnlyTheHostsAndReactionsAskedForAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid contentItemGroupId = Guid.NewGuid();
            Guid reactionId = Guid.NewGuid();
            Association countedReaction = CreateCountedReaction(contentItemGroupId, reactionId);

            // not counted: each misses exactly one term of §ARC16.8's predicate
            Association reactionOnAnotherHost =
                CreateCountedReaction(Guid.NewGuid(), reactionId);

            Association reactionOnAHostThatIsNotAContentItem =
                CreateCountedReaction(contentItemGroupId, reactionId);

            reactionOnAHostThatIsNotAContentItem.EntityAType = EntityType.Comment;
            reactionOnAHostThatIsNotAContentItem.EntityAContentType = null;

            Association anotherReactionOnTheHost =
                CreateCountedReaction(contentItemGroupId, Guid.NewGuid());

            Association farEndThatIsNotAReaction =
                CreateCountedReaction(contentItemGroupId, reactionId);

            farEndThatIsNotAReaction.EntityBType = EntityType.Tag;

            var storageAssociations = new List<Association>
            {
                countedReaction,
                reactionOnAnotherHost,
                reactionOnAHostThatIsNotAContentItem,
                anotherReactionOnTheHost,
                farEndThatIsNotAReaction
            };

            IReadOnlyList<Guid> inputContentItemGroupIds = new List<Guid> { contentItemGroupId };
            IReadOnlyList<Guid> inputReactionIds = new List<Guid> { reactionId };

            var expectedAssociationPairCounts = new List<AssociationPairCount>
            {
                CreateAssociationPairCount(contentItemGroupId, reactionId, count: 1)
            };

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupReactionCountReadOver(storageAssociations);

            // when
            IReadOnlyList<AssociationPairCount> actualAssociationPairCounts =
                await this.associationService.RetrieveContentItemReactionCountsAsync(
                    inputContentItemGroupIds,
                    inputReactionIds,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociationPairCounts.Should().BeEquivalentTo(expectedAssociationPairCounts);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            VerifyReactionCountReadAskedOnce();

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnNoEntryForAReactionNobodyGaveAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid contentItemGroupId = Guid.NewGuid();
            Guid givenReactionId = Guid.NewGuid();
            Guid ungivenReactionId = Guid.NewGuid();

            var storageAssociations = new List<Association>
            {
                CreateCountedReaction(contentItemGroupId, givenReactionId)
            };

            IReadOnlyList<Guid> inputContentItemGroupIds = new List<Guid> { contentItemGroupId };

            IReadOnlyList<Guid> inputReactionIds =
                new List<Guid> { givenReactionId, ungivenReactionId };

            var expectedAssociationPairCounts = new List<AssociationPairCount>
            {
                CreateAssociationPairCount(contentItemGroupId, givenReactionId, count: 1)
            };

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupReactionCountReadOver(storageAssociations);

            // when
            IReadOnlyList<AssociationPairCount> actualAssociationPairCounts =
                await this.associationService.RetrieveContentItemReactionCountsAsync(
                    inputContentItemGroupIds,
                    inputReactionIds,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociationPairCounts.Should().BeEquivalentTo(expectedAssociationPairCounts);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            VerifyReactionCountReadAskedOnce();

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A reader's live, Approved reaction with no publish date: a row every term of the
        // count admits. The host is written AllVersions with a key id that differs from its
        // group id, so a count keyed on the key id rather than the effective id misses it.
        private static Association CreateCountedReaction(Guid contentItemGroupId, Guid reactionId)
        {
            Association reaction = CreateRandomReaction(readerUserId: Guid.NewGuid().ToString());
            reaction.EntityAKeyId = Guid.NewGuid();
            reaction.EntityAGroupId = contentItemGroupId;
            reaction.EntityAScope = Scope.AllVersions;
            reaction.EntityBKeyId = reactionId;
            reaction.EntityBGroupId = reactionId;
            reaction.EntityBScope = Scope.ThisVersionOnly;
            reaction.IsDeleted = false;
            reaction.ApprovalStatus = ApprovalStatus.Approved;
            reaction.PublishDate = null;

            return WithDatabaseComputedEffectiveIds(reaction);
        }

        private static AssociationPairCount CreateAssociationPairCount(
            Guid contentItemGroupId,
            Guid reactionId,
            int count) =>
            new AssociationPairCount
            {
                EntityAEffectiveId = contentItemGroupId,
                EntityBKeyId = reactionId,
                Count = count
            };

        // §ARC12.2.1 rule 5: the mocked read EXECUTES the query-shaping function the service
        // hands it over the in-memory rows, so every term of the condition, the grouping and the
        // count are under test. The function is authored inside the service and cannot be named
        // here, which is why it is the one argument matched by type; the token is matched exactly.
        private void SetupReactionCountReadOver(IEnumerable<Association> storageAssociations) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<AssociationPairCount>>>(),
                    TestContext.Current.CancellationToken))
                        .Returns((
                            Func<IQueryable<Association>, IQueryable<AssociationPairCount>> query,
                            CancellationToken _) =>
                            new ValueTask<IReadOnlyList<AssociationPairCount>>(
                                query(storageAssociations.AsQueryable()).ToList()));

        private void VerifyReactionCountReadAskedOnce() =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<AssociationPairCount>>>(),
                    TestContext.Current.CancellationToken),
                Times.Once);
    }
}
