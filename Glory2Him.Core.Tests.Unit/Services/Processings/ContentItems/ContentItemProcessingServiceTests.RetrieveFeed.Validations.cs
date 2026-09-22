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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    /// <summary>
    /// THE FEED'S PAGE IS CALLER-SUPPLIED, so it is validated — and it is validated HERE
    /// because nothing else caps it. <c>ODataPageSizeConvention</c> fills in
    /// <c>EnableQueryAttribute.PageSize</c> and visits no action without that attribute, and
    /// the feed route deliberately carries none; a read that did not cap itself would serve the
    /// whole visible catalogue to anyone who asked for it.
    ///
    /// <para>The numbers below are written as LITERALS rather than read from the service's
    /// constant. A test that asks the same source as the code passes after someone changes it,
    /// which is the opposite of what pinning a contract is for.</para>
    /// </summary>
    public partial class ContentItemProcessingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveFeedIfTakeIsZeroAsync()
        {
            // given: a page of nothing is not a page. Note this is reachable only because
            // `take` is nullable — an absent parameter is 50, not 0.
            var invalidContentItemProcessingException =
                new InvalidContentItemProcessingException(
                    message: "Content item feed page is invalid, fix the errors and try again.");

            invalidContentItemProcessingException.AddData(
                key: "take",
                values: "Value must be between 1 and 50");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingException);

            // when
            ValueTask<IReadOnlyList<ContentItem>> retrieveContentItemFeedTask =
                this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                    skip: 0,
                    take: 0,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveContentItemFeedTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveFeedIfTakeExceedsTheMaximumAsync()
        {
            // given: REFUSED, never silently clamped. A caller who asked for 51 and received
            // 50 would have no way to tell a capped page from a short one.
            var invalidContentItemProcessingException =
                new InvalidContentItemProcessingException(
                    message: "Content item feed page is invalid, fix the errors and try again.");

            invalidContentItemProcessingException.AddData(
                key: "take",
                values: "Value must be between 1 and 50");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingException);

            // when
            ValueTask<IReadOnlyList<ContentItem>> retrieveContentItemFeedTask =
                this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                    skip: 0,
                    take: 51,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveContentItemFeedTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveFeedIfSkipIsNegativeAsync()
        {
            // given
            var invalidContentItemProcessingException =
                new InvalidContentItemProcessingException(
                    message: "Content item feed page is invalid, fix the errors and try again.");

            invalidContentItemProcessingException.AddData(
                key: "skip",
                values: "Value must be 0 or greater");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingException);

            // when
            ValueTask<IReadOnlyList<ContentItem>> retrieveContentItemFeedTask =
                this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                    skip: -1,
                    take: 10,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveContentItemFeedTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
