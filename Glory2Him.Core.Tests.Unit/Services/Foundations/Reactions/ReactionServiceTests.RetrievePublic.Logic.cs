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
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Reactions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAPubliclyVisibleReactionAsync()
        {
            // given
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            Reaction publicReaction = CreatePubliclyVisibleReaction(currentDateTime);
            var storageReactions = new List<Reaction> { publicReaction };
            Reaction expectedReaction = publicReaction.DeepClone();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            SetupSelectReactionsToQuery(storageReactions, cancellationToken);

            // when
            IReadOnlyList<Reaction> actualReactions =
                await this.reactionService.RetrievePublicReactionsAsync(cancellationToken);

            // then
            actualReactions.Should().ContainSingle();
            actualReactions[0].Should().BeEquivalentTo(expectedReaction);
            actualReactions[0].Name.Should().Be(expectedReaction.Name);
            actualReactions[0].UnicodeEmoji.Should().Be(expectedReaction.UnicodeEmoji);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            VerifySelectReactionsQueriedOnce(cancellationToken);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // an approved, published reaction whose publish date has already passed, so it is
        // visible to anybody under §SEC14.3 rule 4
        private static Reaction CreatePubliclyVisibleReaction(DateTimeOffset currentDateTime)
        {
            Reaction reaction = CreateRandomReaction();
            reaction.ApprovalStatus = ApprovalStatus.Approved;
            reaction.IsPublished = true;
            reaction.PublishDate = currentDateTime.AddDays(GetRandomNegativeNumber());

            return reaction;
        }

        // the condition is the service's, so the mock executes whatever function it is handed
        // over the seeded rows (§ARC12.2.1 rule 5): receiving a function proves nothing
        private void SetupSelectReactionsToQuery(
            List<Reaction> storageReactions,
            CancellationToken cancellationToken) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionsAsync(
                    It.IsAny<Func<IQueryable<Reaction>, IQueryable<Reaction>>>(),
                    cancellationToken))
                        .Returns((
                            Func<IQueryable<Reaction>, IQueryable<Reaction>> query,
                            CancellationToken _) =>
                            new ValueTask<IReadOnlyList<Reaction>>(
                                query(storageReactions.AsQueryable()).ToList()));

        private void VerifySelectReactionsQueriedOnce(CancellationToken cancellationToken) =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionsAsync(
                    It.IsAny<Func<IQueryable<Reaction>, IQueryable<Reaction>>>(),
                    cancellationToken),
                Times.Once);
    }
}
