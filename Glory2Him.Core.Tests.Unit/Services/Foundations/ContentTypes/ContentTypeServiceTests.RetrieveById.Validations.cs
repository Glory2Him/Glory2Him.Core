// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
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
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidContentTypeId = Guid.Empty;

            var invalidContentTypeException = new InvalidContentTypeException(
                message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedContentTypeValidationException = new ContentTypeValidationException(
                message: "Content type validation error occurred, fix the errors and try again.",
                innerException: invalidContentTypeException);

            // when
            ValueTask<Glory2Him.Core.Models.Foundations.ContentTypes.ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    invalidContentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    retrieveContentTypeByIdTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfContentTypeNotFoundAndLogItAsync()
        {
            // given
            Guid someContentTypeId = Guid.NewGuid();
            ContentType nullContentType = null;

            var notFoundContentTypeException =
                new NotFoundContentTypeException(
                    message: $"Content type not found with id: {someContentTypeId}.");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentTypeException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullContentType);

            // when
            ValueTask<ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    retrieveContentTypeByIdTask.AsTask);

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
