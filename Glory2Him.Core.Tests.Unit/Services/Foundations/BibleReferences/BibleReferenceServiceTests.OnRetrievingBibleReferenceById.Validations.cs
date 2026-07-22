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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievingBibleReferenceByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<BibleReference>? nullEnvelope = null;

            var invalidBibleReferenceEventException =
                new InvalidBibleReferenceEventException(
                    message: "Invalid bible reference event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceEventException);

            // when
            ValueTask<EventEnvelope<BibleReference>?> onRetrieveTask =
                this.bibleReferenceService.OnRetrievingBibleReferenceByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    onRetrieveTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughNotFoundValidationExceptionOnRetrievingBibleReferenceByIdEventAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<BibleReference>
            {
                Content = new BibleReference { Id = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    requestEnvelope.Content.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((BibleReference?)null);

            // when
            ValueTask<EventEnvelope<BibleReference>?> onRetrieveTask =
                this.bibleReferenceService.OnRetrievingBibleReferenceByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    onRetrieveTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped —
            // the substrate wrapper must not double-wrap it.
            actualBibleReferenceValidationException.InnerException
                .Should().BeOfType<NotFoundBibleReferenceException>();

            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
