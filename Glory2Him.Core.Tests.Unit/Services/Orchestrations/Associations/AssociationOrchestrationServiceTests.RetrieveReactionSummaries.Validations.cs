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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveReactionSummariesIfIdsAreNullAndLogItAsync()
        {
            // given
            IReadOnlyList<Guid> nullContentItemIds = null;

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.UpsertDataList(
                key: "contentItemIds",
                value: "List is required");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<IReadOnlyList<ContentItemReactionSummary>> retrieveTask =
                this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    nullContentItemIds,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
            VerifyNothingIsReadForTheSummaries();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveReactionSummariesIfThereAreNoIdsAndLogItAsync()
        {
            // given
            IReadOnlyList<Guid> noContentItemIds = new List<Guid>();

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.UpsertDataList(
                key: "contentItemIds",
                value: "List must hold at least one id");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<IReadOnlyList<ContentItemReactionSummary>> retrieveTask =
                this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    noContentItemIds,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
            VerifyNothingIsReadForTheSummaries();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveReactionSummariesIfThereAreMoreThan25IdsAndLogItAsync()
        {
            // given
            IReadOnlyList<Guid> tooManyContentItemIds = CreateDistinctContentItemIds(count: 26);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.UpsertDataList(
                key: "contentItemIds",
                value: "List must hold no more than 25 distinct ids");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<IReadOnlyList<ContentItemReactionSummary>> retrieveTask =
                this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    tooManyContentItemIds,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
            VerifyNothingIsReadForTheSummaries();
        }

        [Fact]
        public async Task ShouldNotThrowValidationExceptionOnRetrieveReactionSummariesIfTwentyFiveDistinctIdsCarryDuplicatesAsync()
        {
            // given: 25 distinct ids, three of them supplied twice, so the list holds 28 entries
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            List<Guid> distinctContentItemIds = CreateDistinctContentItemIds(count: 25);
            Reaction love = CreatePublicReaction(name: "Love");

            List<Guid> contentItemIds = distinctContentItemIds
                .Concat(distinctContentItemIds.Take(3))
                .ToList();

            SetupPublicContentItemGroups(distinctContentItemIds);
            SetupPublicReactions(love);
            SetupWinningSettings(hosts: []);
            SetupReactionCounts(contentItemGroupIds: [], reactionIds: [love.Id]);

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: the bound counts distinct ids, and the 25 are handed to the public groups read
            actualSummaries.Should().BeEmpty();

            this.contentItemServiceMock.Verify(service =>
                service.RetrievePublicContentItemGroupsAsync(
                    It.Is(SameIdsAs(distinctContentItemIds)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private static List<Guid> CreateDistinctContentItemIds(int count) =>
            Enumerable.Range(start: 0, count)
                .Select(_ => Guid.NewGuid())
                .ToList();

        // Refused before any read: no host, no vocabulary, no setting, no count, no caller's
        // reaction, and no envelope minted.
        private void VerifyNothingIsReadForTheSummaries()
        {
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
        }
    }
}
