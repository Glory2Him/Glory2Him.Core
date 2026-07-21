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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidLinkId = Guid.Empty;

            var invalidLinkException = new InvalidLinkException(
                message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.UpsertDataList(
                key: nameof(Link.Id),
                value: "Id is required");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: invalidLinkException);

            // when
            ValueTask<Link> hardRemoveLinkByIdTask =
                this.linkService.HardRemoveLinkByIdAsync(
                    invalidLinkId,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    hardRemoveLinkByIdTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfLinkNotFoundAndLogItAsync()
        {
            // given
            Guid someLinkId = Guid.NewGuid();
            Link noLink = null;

            var notFoundLinkException = new NotFoundLinkException(
                message: $"Link not found with id: {someLinkId}.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: notFoundLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noLink);

            // when
            ValueTask<Link> hardRemoveLinkByIdTask =
                this.linkService.HardRemoveLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    hardRemoveLinkByIdTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
