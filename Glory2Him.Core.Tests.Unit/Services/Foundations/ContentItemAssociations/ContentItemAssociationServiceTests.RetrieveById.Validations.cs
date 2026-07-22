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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidContentItemAssociationId = Guid.Empty;

            var invalidContentItemAssociationException = new InvalidContentItemAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedContentItemAssociationValidationException = new ContentItemAssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemAssociationException);

            // when
            ValueTask<ContentItemAssociation> retrieveContentItemAssociationByIdTask =
                this.contentItemAssociationService.RetrieveContentItemAssociationByIdAsync(
                    invalidContentItemAssociationId,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    retrieveContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfContentItemAssociationNotFoundAndLogItAsync()
        {
            // given
            Guid someContentItemAssociationId = Guid.NewGuid();
            ContentItemAssociation nullContentItemAssociation = null;

            var notFoundContentItemAssociationException =
                new NotFoundContentItemAssociationException(
                    message: $"Content item association not found with id: {someContentItemAssociationId}.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullContentItemAssociation);

            // when
            ValueTask<ContentItemAssociation> retrieveContentItemAssociationByIdTask =
                this.contentItemAssociationService.RetrieveContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    retrieveContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
