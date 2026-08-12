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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfLinkIsNullAndLogItAsync()
        {
            // given
            Link nullLink = null;

            var nullLinkException =
                new NullLinkException(message: "Link is null.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: nullLinkException);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    nullLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnModifyIfLinkIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidLink = new Link
            {
                Id = Guid.Empty,
                Name = invalidText,
                Url = invalidText,
                LinkType = invalidText,
                ContentItemGroupId = Guid.Empty,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.Id),
                values: "Id is required");

            invalidLinkException.AddData(
                key: nameof(Link.Name),
                values: "Text is required");

            invalidLinkException.AddData(
                key: nameof(Link.Url),
                values: "Text is required");

            invalidLinkException.AddData(
                key: nameof(Link.LinkType),
                values: "Text is required");

            invalidLinkException.AddData(
                key: nameof(Link.ContentItemGroupId),
                values: "Id is required");

            invalidLinkException.AddData(
                key: nameof(Link.CreatedBy),
                values: "Text is required");

            invalidLinkException.AddData(
                key: nameof(Link.UpdatedBy),
                values: "Text is required");

            invalidLinkException.AddData(
                key: nameof(Link.CreatedWhen),
                values: "Date is required");

            invalidLinkException.AddData(
                key: nameof(Link.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfLinkNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link nonExistentLink = randomLink;
            Link noLink = null;

            var notFoundLinkException = new NotFoundLinkException(
                message: $"Link not found with id: {nonExistentLink.Id}.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: notFoundLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    nonExistentLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noLink);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    nonExistentLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    nonExistentLink.Id,
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedWhenNotSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link invalidLink = randomLink;
            Link storageLink = randomLink.DeepClone();
            storageLink.CreatedWhen = GetRandomDateTimeOffset();
            storageLink.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidLinkException = new InvalidLinkException(
                message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.CreatedWhen),
                values: $"Date is not the same as {nameof(Link.CreatedWhen)}");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink))
                        .ReturnsAsync(invalidLink);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink),
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedByNotSameAsInputAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link invalidLink = randomLink;
            Link storageLink = randomLink.DeepClone();
            storageLink.CreatedBy = GetRandomString();
            storageLink.UpdatedWhen = storageLink.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.CreatedBy),
                values: $"Text is not the same as {nameof(Link.CreatedBy)}");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink))
                        .ReturnsAsync(invalidLink);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink),
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageUpdatedWhenSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link invalidLink = randomLink;
            Link storageLink = randomLink.DeepClone();

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.UpdatedWhen),
                values: $"Date is the same as {nameof(Link.UpdatedWhen)}");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink))
                        .ReturnsAsync(invalidLink);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink),
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedByIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link invalidLink = randomLink;
            invalidLink.UpdatedBy = differentUserId;

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsSameAsCreatedWhenAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link invalidLink = randomLink;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidLink.UpdatedWhen = invalidLink.CreatedWhen;

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(Link.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidLink.UpdatedWhen}"
                });

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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

        [Theory]
        [MemberData(nameof(MinutesBeforeOrAfter))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsNotRecentAndLogItAsync(int minutes)
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link invalidLink = randomLink;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidLink.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidLink.UpdatedWhen}");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfLinkExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Link invalidLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.CreatedBy),
                values: $"Text exceed max length of {invalidLink.CreatedBy.Length - 1} characters");

            invalidLinkException.AddData(
                key: nameof(Link.UpdatedBy),
                values: $"Text exceed max length of {invalidLink.UpdatedBy.Length - 1} characters");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Link someLink = CreateRandomLink();

            var unauthorizedLinkException = new UnauthorizedLinkException(
                message: "The current user is not authenticated.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: unauthorizedLinkException);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    someLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

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

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.LinkReadOnly)]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsBlockedFromContributingAndLogItAsync(
            string blockedRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockedRole);
            Link someLink = CreateRandomLink();

            var unauthorizedLinkException = new UnauthorizedLinkException(
                message: "The current user is blocked from contributing links.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: unauthorizedLinkException);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    someLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotOwnerAndHasNoReviewRoleAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link inputLink = randomLink;
            Link storageLink = randomLink.DeepClone();
            storageLink.CreatedBy = GetRandomString();
            storageLink.UpdatedWhen = storageLink.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var unauthorizedLinkException = new UnauthorizedLinkException(
                message: "The current user is not allowed to modify this link.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: unauthorizedLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    inputLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(inputLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    inputLink.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateLinkAsync(
                    It.IsAny<Link>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalStatusChangedByNonPublisherAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link invalidLink = randomLink;
            Link storageLink = randomLink.DeepClone();
            storageLink.UpdatedWhen = storageLink.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidLink.ApprovalStatus = ApprovalStatus.Approved;

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.ApprovalStatus),
                values: "Value is not the same as storage approval status");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink))
                        .ReturnsAsync(invalidLink);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink),
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfIsPublishedChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link invalidLink = randomLink;
            Link storageLink = randomLink.DeepClone();
            storageLink.UpdatedWhen = storageLink.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidLink.IsPublished = true;

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.IsPublished),
                values: "Value is not the same as IsPublished");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink))
                        .ReturnsAsync(invalidLink);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink),
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfPublishDateChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link invalidLink = randomLink;
            Link storageLink = randomLink.DeepClone();
            storageLink.UpdatedWhen = storageLink.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidLink.PublishDate = randomDateTimeOffset;

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.PublishDate),
                values: "Date is not the same as PublishDate");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink))
                        .ReturnsAsync(invalidLink);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    invalidLink.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidLink,
                    storageLink),
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
