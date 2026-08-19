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
        public async Task ShouldThrowValidationExceptionOnRetrievePublishedByGroupIdIfGroupIdIsInvalidAndLogItAsync()
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
                this.linkProcessingService.RetrievePublishedLinkByGroupIdAsync(
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
        public async Task ShouldThrowNotFoundExceptionOnRetrievePublishedByGroupIdIfNoPublishedRowAndLogItAsync()
        {
            // given: a group whose newest version is still in review, and which has never
            // had an approved version published, has no public row at all — including after
            // a fork off a Rejected row
            Guid inputGroupId = Guid.NewGuid();

            Link draftTipVersion = CreateRandomNonPublicLink(createdBy: GetRandomString());
            draftTipVersion.GroupId = inputGroupId;
            draftTipVersion.Version = 1;

            IQueryable<Link> storageLinks = new[] { draftTipVersion }.AsQueryable();

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
                this.linkProcessingService.RetrievePublishedLinkByGroupIdAsync(
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
                    message.Contains("no non-deleted published version"))),
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
            ShouldThrowNotFoundOnRetrievePublishedByGroupIdIfScheduledInFutureAndCallerIsAnonymousAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given: a published row whose publish date has not yet passed is not
            // canonically visible, so an anonymous caller is answered not-found
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link publishedVersion = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publishedVersion.GroupId = inputGroupId;
            publishedVersion.PublishDate = currentDateTime.AddDays(1);

            IQueryable<Link> storageLinks = new[] { publishedVersion }.AsQueryable();

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
                this.linkProcessingService.RetrievePublishedLinkByGroupIdAsync(
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
