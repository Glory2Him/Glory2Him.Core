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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievingReactionByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Reaction>? nullEnvelope = null;

            var invalidReactionEventException =
                new InvalidReactionEventException(
                    message: "Invalid reaction event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionEventException);

            // when
            ValueTask<EventEnvelope<Reaction>?> onRetrieveTask =
                this.reactionService.OnRetrievingReactionByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    onRetrieveTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughNotFoundValidationExceptionOnRetrievingReactionByIdEventAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Reaction>
            {
                Content = new Reaction { Id = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    requestEnvelope.Content.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((Reaction?)null);

            // when
            ValueTask<EventEnvelope<Reaction>?> onRetrieveTask =
                this.reactionService.OnRetrievingReactionByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    onRetrieveTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped —
            // the substrate wrapper must not double-wrap it.
            actualReactionValidationException.InnerException
                .Should().BeOfType<NotFoundReactionException>();

            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
