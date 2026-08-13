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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnCheckContentExistsIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            ContentType someContentType = ContentType.Quote;
            string someContentHash = GetRandomString();

            var unauthorizedContentItemException = new UnauthorizedContentItemException(
                message: "The current user is not authenticated.");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemException);

            // when
            ValueTask<bool> checkContentExistsTask =
                this.contentItemService.CheckContentItemContentExistsAsync(
                    someContentType,
                    someContentHash,
                    excludedGroupId: null,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    checkContentExistsTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldThrowValidationExceptionOnCheckContentExistsIfUserIsBlockedFromContributingAndLogItAsync(
            string blockedRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockedRole);
            ContentType someContentType = ContentType.Quote;
            string someContentHash = GetRandomString();

            var unauthorizedContentItemException = new UnauthorizedContentItemException(
                message: "The current user is blocked from contributing content items.");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemException);

            // when
            ValueTask<bool> checkContentExistsTask =
                this.contentItemService.CheckContentItemContentExistsAsync(
                    someContentType,
                    someContentHash,
                    excludedGroupId: null,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    checkContentExistsTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnCheckContentExistsIfInputsAreInvalidAndLogItAsync()
        {
            // given
            var invalidContentType = (ContentType)int.MaxValue;
            string invalidContentHash = null;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ContentType),
                values: "Value is not a supported content type");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ContentHash),
                values: "Text is required");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemException);

            // when
            ValueTask<bool> checkContentExistsTask =
                this.contentItemService.CheckContentItemContentExistsAsync(
                    invalidContentType,
                    invalidContentHash,
                    excludedGroupId: null,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    checkContentExistsTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnCheckContentExistsIfContentHashIsInvalidAndLogItAsync(
            string invalidContentHash)
        {
            // given
            ContentType someContentType = ContentType.Quote;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ContentHash),
                values: "Text is required");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemException);

            // when
            ValueTask<bool> checkContentExistsTask =
                this.contentItemService.CheckContentItemContentExistsAsync(
                    someContentType,
                    invalidContentHash,
                    excludedGroupId: null,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    checkContentExistsTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnCheckContentExistsIfContentTypeIsInvalidAndLogItAsync()
        {
            // given
            var invalidContentType = (ContentType)int.MaxValue;
            string someContentHash = GetRandomString();

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ContentType),
                values: "Value is not a supported content type");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemException);

            // when
            ValueTask<bool> checkContentExistsTask =
                this.contentItemService.CheckContentItemContentExistsAsync(
                    invalidContentType,
                    someContentHash,
                    excludedGroupId: null,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    checkContentExistsTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
