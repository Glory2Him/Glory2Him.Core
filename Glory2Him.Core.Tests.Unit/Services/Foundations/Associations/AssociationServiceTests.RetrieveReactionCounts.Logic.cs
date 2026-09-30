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
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        // what the query-shaping function produced when the mocked read ran it
        private IReadOnlyList<AssociationPairCount> reactionCountQueryResult;

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
                CreateCountedReactionOnOneVersion(firstContentItemGroupId, loveReactionId),
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
            actualAssociationPairCounts.Should().BeSameAs(this.reactionCountQueryResult);

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
            actualAssociationPairCounts.Should().BeSameAs(this.reactionCountQueryResult);

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

            // pinned to another version of the host: its group id is the one asked for, but its
            // effective id is that version's key id, which was not asked for
            Association reactionOnAnotherVersionOfTheHost =
                CreateCountedReactionOnOneVersion(Guid.NewGuid(), reactionId);

            reactionOnAnotherVersionOfTheHost.EntityAGroupId = contentItemGroupId;

            var storageAssociations = new List<Association>
            {
                countedReaction,
                reactionOnAnotherVersionOfTheHost,
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
            actualAssociationPairCounts.Should().BeSameAs(this.reactionCountQueryResult);

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
            actualAssociationPairCounts.Should().BeSameAs(this.reactionCountQueryResult);

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

        [Theory]
        [MemberData(nameof(EveryKindOfCaller))]
        public async Task ShouldRetrieveReactionCountsWithoutReadingTheCallerAsync(
            SecurityContext callerSecurityContext)
        {
            // given: whoever is calling is what an envelope would capture, were one minted
            this.ambientSecurityContext = callerSecurityContext;
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid contentItemGroupId = Guid.NewGuid();
            Guid reactionId = Guid.NewGuid();

            var storageAssociations = new List<Association>
            {
                CreateCountedReaction(contentItemGroupId, reactionId),
                CreateCountedReaction(contentItemGroupId, reactionId)
            };

            IReadOnlyList<Guid> inputContentItemGroupIds = new List<Guid> { contentItemGroupId };
            IReadOnlyList<Guid> inputReactionIds = new List<Guid> { reactionId };

            var expectedAssociationPairCounts = new List<AssociationPairCount>
            {
                CreateAssociationPairCount(contentItemGroupId, reactionId, count: 2)
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
            actualAssociationPairCounts.Should().BeSameAs(this.reactionCountQueryResult);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            VerifyReactionCountReadAskedOnce();

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // §ARC16.8, Anonymity: the same counts for an anonymous visitor, a signed-in reader, a
        // reviewer and an administrator — none of whom the read may even look at
        public static TheoryData<SecurityContext> EveryKindOfCaller() =>
            new TheoryData<SecurityContext>
            {
                null,
                new SecurityContext { IsAuthenticated = false },
                CreateAuthenticatedSecurityContext(),
                CreateAuthenticatedSecurityContext(Roles.Reviewers),
                CreateAuthenticatedSecurityContext(Roles.Administrators)
            };

        // A reader's live, Approved reaction with no publish date: a row every term of the
        // count admits. Every id column holds its own value, so a read keyed on the wrong column
        // misses the row. The host is AllVersions with a key id unlike its group id, so its
        // effective id is the group id and a count on the key id misses it. The far end is
        // AllVersions with a group id unlike its key id, so its effective id and group id both
        // differ from the key id the count asks for. A far-end reaction is derived
        // ThisVersionOnly in production; it is not here only so that the three columns differ.
        private static Association CreateCountedReaction(Guid contentItemGroupId, Guid reactionId)
        {
            Association reaction = CreateRandomReaction(readerUserId: Guid.NewGuid().ToString());
            reaction.EntityAKeyId = Guid.NewGuid();
            reaction.EntityAGroupId = contentItemGroupId;
            reaction.EntityAScope = Scope.AllVersions;
            reaction.EntityBKeyId = reactionId;
            reaction.EntityBGroupId = Guid.NewGuid();
            reaction.EntityBScope = Scope.AllVersions;
            reaction.IsDeleted = false;
            reaction.ApprovalStatus = ApprovalStatus.Approved;
            reaction.PublishDate = null;

            return WithDatabaseComputedEffectiveIds(reaction);
        }

        // The same counted row with its host pinned to one version: ThisVersionOnly, so its
        // effective id is the version's key id and its group id is another value. A count that
        // filters or groups on the host's group id rather than its effective id misses it.
        private static Association CreateCountedReactionOnOneVersion(
            Guid contentItemEffectiveId,
            Guid reactionId)
        {
            Association reaction = CreateCountedReaction(Guid.NewGuid(), reactionId);
            reaction.EntityAKeyId = contentItemEffectiveId;
            reaction.EntityAScope = Scope.ThisVersionOnly;

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
        // hands it over the in-memory rows, and keeps the list that function produced. Each logic
        // test asserts that the service returns THAT list, the same instance, and that it holds
        // the expected counts. So the condition, the grouping and the count are under test only
        // as parts of the function storage runs: a service that filtered, grouped or counted after
        // the await would return a list of its own and fail. The function is authored inside the
        // service and cannot be named here, which is why it is the one argument matched by type;
        // the token is matched exactly.
        private void SetupReactionCountReadOver(IEnumerable<Association> storageAssociations) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<AssociationPairCount>>>(),
                    TestContext.Current.CancellationToken))
                        .Returns((
                            Func<IQueryable<Association>, IQueryable<AssociationPairCount>> query,
                            CancellationToken _) =>
                        {
                            this.reactionCountQueryResult =
                                query(storageAssociations.AsQueryable()).ToList();

                            return new ValueTask<IReadOnlyList<AssociationPairCount>>(
                                this.reactionCountQueryResult);
                        });

        private void VerifyReactionCountReadAskedOnce() =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<AssociationPairCount>>>(),
                    TestContext.Current.CancellationToken),
                Times.Once);
    }
}
