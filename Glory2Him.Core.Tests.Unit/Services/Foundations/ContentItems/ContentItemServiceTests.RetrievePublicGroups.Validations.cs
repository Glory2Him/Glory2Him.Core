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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievePublicGroupsIfIdsAreNullAndLogItAsync()
        {
            // given
            IReadOnlyList<Guid> nullContentItemIds = null;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.UpsertDataList(
                key: "contentItemIds",
                value: "Ids are required");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemException);

            // when
            ValueTask<IReadOnlyList<PublicContentItemGroup>> retrievePublicContentItemGroupsTask =
                this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds: nullContentItemIds,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    retrievePublicContentItemGroupsTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievePublicGroupsIfAnIdIsEmptyAndLogItAsync()
        {
            // given: the empty id sits among valid ones - one bad member refuses the list
            IReadOnlyList<Guid> invalidContentItemIds = new[]
            {
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid()
            };

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.UpsertDataList(
                key: "contentItemIds",
                value: "Ids must not contain an empty id");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemException);

            // when
            ValueTask<IReadOnlyList<PublicContentItemGroup>> retrievePublicContentItemGroupsTask =
                this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds: invalidContentItemIds,
                    cancellationToken: TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    retrievePublicContentItemGroupsTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
