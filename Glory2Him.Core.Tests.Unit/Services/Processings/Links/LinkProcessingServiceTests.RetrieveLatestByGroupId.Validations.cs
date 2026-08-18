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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveLatestByGroupIdIfGroupIdIsInvalidAndLogItAsync()
        {
            // given
            Guid invalidGroupId = Guid.Empty;

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { GroupId = invalidGroupId },
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidLinkProcessingException =
                new InvalidLinkProcessingException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkProcessingException.AddData(
                key: nameof(Link.GroupId),
                values: "Id is required");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(invalidGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<Link> retrieveLinkTask =
                this.linkProcessingService.RetrieveLatestLinkByGroupIdAsync(
                    invalidGroupId,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    retrieveLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowNotFoundExceptionOnRetrieveLatestByGroupIdIfNoLatestVersionAndLogItAsync()
        {
            // given: a group whose only row is soft-deleted has no readable tip — the tip is
            // derived from the LIVE rows, and there are none — and the answer is a
            // reason-free not-found for every caller
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link deletedLatestVersion = CreateRandomDeletedLink(currentDateTime);
            deletedLatestVersion.GroupId = inputGroupId;
            deletedLatestVersion.Version = 1;

            IQueryable<Link> storageLinks = new[] { deletedLatestVersion }.AsQueryable();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { GroupId = inputGroupId },
                securityContext: CreateAuthenticatedSecurityContext(Roles.Admin));

            var notFoundLinkProcessingException =
                new NotFoundLinkProcessingException(message: "The link was not found.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            // when
            ValueTask<Link> retrieveLinkTask =
                this.linkProcessingService.RetrieveLatestLinkByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    retrieveLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(It.Is<string>(message =>
                    message.Contains("no non-deleted latest version"))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task
            ShouldThrowNotFoundExceptionOnRetrieveLatestByGroupIdIfTipIsNonPublicAndCallerIsAnonymousAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given: an unprivileged probe must not be able to tell a non-public tip from a
            // missing group
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link latestVersion = CreateRandomNonPublicLink(createdBy: GetRandomString());
            latestVersion.GroupId = inputGroupId;
            latestVersion.Version = 1;

            IQueryable<Link> storageLinks = new[] { latestVersion }.AsQueryable();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { GroupId = inputGroupId },
                securityContext: unauthenticatedSecurityContext!);

            var notFoundLinkProcessingException =
                new NotFoundLinkProcessingException(message: "The link was not found.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<Link> retrieveLinkTask =
                this.linkProcessingService.RetrieveLatestLinkByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    retrieveLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);
        }
    }
}
