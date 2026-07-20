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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidContentTypeId = Guid.Empty;

            var invalidContentTypeException = new InvalidContentTypeException(
                message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.UpsertDataList(
                key: nameof(ContentType.Id),
                value: "Id is required");

            var expectedContentTypeValidationException = new ContentTypeValidationException(
                message: "Content type validation error occurred, fix the errors and try again.",
                innerException: invalidContentTypeException);

            // when
            ValueTask<ContentType> hardRemoveContentTypeByIdTask =
                this.contentTypeService.HardRemoveContentTypeByIdAsync(
                    invalidContentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    hardRemoveContentTypeByIdTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfContentTypeNotFoundAndLogItAsync()
        {
            // given
            Guid someContentTypeId = Guid.NewGuid();
            ContentType noContentType = null;

            var notFoundContentTypeException = new NotFoundContentTypeException(
                message: $"Content type not found with id: {someContentTypeId}.");

            var expectedContentTypeValidationException = new ContentTypeValidationException(
                message: "Content type validation error occurred, fix the errors and try again.",
                innerException: notFoundContentTypeException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noContentType);

            // when
            ValueTask<ContentType> hardRemoveContentTypeByIdTask =
                this.contentTypeService.HardRemoveContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    hardRemoveContentTypeByIdTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
