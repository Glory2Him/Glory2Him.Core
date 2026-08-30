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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given
            Guid inputLinkId = Guid.NewGuid();
            string inputDeletionReason = GetRandomString();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { Id = inputLinkId, DeletionReason = inputDeletionReason },
                securityContext: unauthenticatedSecurityContext!);

            var unauthorizedLinkProcessingException =
                new UnauthorizedLinkProcessingException(
                    message: "The current user is not authenticated.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(inputLinkId, inputDeletionReason))))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<Link> removeLinkTask =
                this.linkProcessingService.RemoveLinkByIdAsync(
                    inputLinkId,
                    inputDeletionReason,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    removeLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.linkServiceMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.LinkReadOnly)]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserHasBlockRoleAndLogItAsync(
            string blockRole)
        {
            // given
            Guid inputLinkId = Guid.NewGuid();
            string inputDeletionReason = GetRandomString();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { Id = inputLinkId, DeletionReason = inputDeletionReason },
                securityContext: CreateAuthenticatedSecurityContext(blockRole));

            var unauthorizedLinkProcessingException =
                new UnauthorizedLinkProcessingException(
                    message: "The current user is blocked from contributing links.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(inputLinkId, inputDeletionReason))))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<Link> removeLinkTask =
                this.linkProcessingService.RemoveLinkByIdAsync(
                    inputLinkId,
                    inputDeletionReason,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    removeLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.linkServiceMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            Guid invalidLinkId = Guid.Empty;

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { Id = invalidLinkId },
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidLinkProcessingException =
                new InvalidLinkProcessingException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkProcessingException.AddData(
                key: nameof(Link.Id),
                values: "Id is required");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(invalidLinkId, null))))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<Link> removeLinkTask =
                this.linkProcessingService.RemoveLinkByIdAsync(
                    invalidLinkId,
                    null,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    removeLinkTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfLinkIsAlreadyRemovedAndLogItAsync()
        {
            // given: a remove is idempotent from the caller's point of view, but an already
            // removed row must never be presented as a fresh removal
            Guid inputLinkId = Guid.NewGuid();
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLinkId,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            storageLink.IsDeleted = true;
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: storageLink,
                securityContext: securityContext);

            var notFoundLinkProcessingException =
                new NotFoundLinkProcessingException(message: "The link was not found.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(inputLinkId, null))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<Link> removeLinkTask =
                this.linkProcessingService.RemoveLinkByIdAsync(
                    inputLinkId,
                    null,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    removeLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.linkServiceMock.Verify(service =>
                service.RemoveLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.LinkReviewers)]
        [InlineData(Roles.Publishers)]
        [InlineData(Roles.LinkPublishers)]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfActorIsNotOwnerOrAdminAndLogItAsync(
            string? actorRole)
        {
            // given: only the owner and an Admin may take a link down — the review roles
            // moderate through the approval workflow, not through deletion
            Guid inputLinkId = Guid.NewGuid();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLinkId,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: GetRandomString());

            string[] actorRoles = actorRole is null
                ? Array.Empty<string>()
                : new[] { actorRole };

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(actorRoles);

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: storageLink,
                securityContext: securityContext);

            var unauthorizedLinkProcessingException =
                new UnauthorizedLinkProcessingException(
                    message: "The current user is not allowed to remove this link.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(inputLinkId, null))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ValueTask<Link> removeLinkTask =
                this.linkProcessingService.RemoveLinkByIdAsync(
                    inputLinkId,
                    null,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    removeLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.linkServiceMock.Verify(service =>
                service.RemoveLinkByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
