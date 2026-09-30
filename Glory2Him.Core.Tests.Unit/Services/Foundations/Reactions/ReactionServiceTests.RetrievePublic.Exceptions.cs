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
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnRetrievePublicIfSqlErrorOccursAndLogItAsync()
        {
            // given
            SqlException sqlException = GetSqlException();

            var failedStorageReactionException = new FailedStorageReactionException(
                message: "Failed reaction storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedReactionDependencyException = new ReactionDependencyException(
                message: "Reaction dependency error occurred, contact support.",
                innerException: failedStorageReactionException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionsAsync(
                    It.IsAny<Func<IQueryable<Reaction>, IQueryable<Reaction>>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<IReadOnlyList<Reaction>> retrievePublicReactionsTask =
                this.reactionService.RetrievePublicReactionsAsync(
                    TestContext.Current.CancellationToken);

            ReactionDependencyException actualReactionDependencyException =
                await Assert.ThrowsAsync<ReactionDependencyException>(
                    retrievePublicReactionsTask.AsTask);

            // then
            actualReactionDependencyException.Should().BeEquivalentTo(
                expectedReactionDependencyException);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionsAsync(
                    It.IsAny<Func<IQueryable<Reaction>, IQueryable<Reaction>>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedReactionDependencyException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
