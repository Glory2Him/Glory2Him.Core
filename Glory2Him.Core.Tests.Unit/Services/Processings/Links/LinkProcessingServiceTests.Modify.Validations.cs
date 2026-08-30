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
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given
            Link randomLink = CreateRandomLink();
            Link inputLink = randomLink;

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: unauthenticatedSecurityContext!);

            var unauthorizedLinkProcessingException =
                new UnauthorizedLinkProcessingException(
                    message: "The current user is not authenticated.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkProcessingService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.linkServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.LinkReadOnly)]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserHasBlockRoleAndLogItAsync(
            string blockRole)
        {
            // given
            Link randomLink = CreateRandomLink();
            Link inputLink = randomLink;

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext(blockRole));

            var unauthorizedLinkProcessingException =
                new UnauthorizedLinkProcessingException(
                    message: "The current user is blocked from contributing links.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkProcessingService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.linkServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfLinkIsNullAndLogItAsync()
        {
            // given
            Link nullLink = null!;

            var nullLinkProcessingException =
                new NullLinkProcessingException(message: "Link is null.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: nullLinkProcessingException);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkProcessingService.ModifyLinkAsync(
                    nullLink,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
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
            var invalidLink = new Link
            {
                Id = Guid.Empty,
                Name = invalidText!,
                Url = invalidText!,
                LinkType = invalidText!
            };

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: invalidLink,
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidLinkProcessingException =
                new InvalidLinkProcessingException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkProcessingException.AddData(
                key: nameof(Link.Id),
                values: "Id is required");

            invalidLinkProcessingException.AddData(
                key: nameof(Link.Name),
                values: "Text is required");

            invalidLinkProcessingException.AddData(
                key: nameof(Link.Url),
                values: "Text is required");

            invalidLinkProcessingException.AddData(
                key: nameof(Link.LinkType),
                values: "Text is required");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(invalidLink))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkProcessingService.ModifyLinkAsync(
                    invalidLink,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    modifyLinkTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfLinkIsSoftDeletedAndLogItAsync()
        {
            // given: a removed row is gone for every caller — an owner editing their own
            // soft-deleted link is answered not-found, never resurrected
            Link inputLink = CreateRandomLink();
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            storageLink.IsDeleted = true;
            SetupGroupTipRead(storageLink);
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: securityContext);

            var notFoundLinkProcessingException =
                new NotFoundLinkProcessingException(message: "The link was not found.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkProcessingService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.linkServiceMock.Verify(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfLinkIsNotLatestVersionAndLogItAsync()
        {
            // given: only the edit tip of a group is modifiable — editing a superseded
            // version would fork off history and leave two live chains from one row.
            //
            // The row is superseded because a LIVE SIBLING IN ITS GROUP CARRIES A HIGHER
            // VERSION, which is the only thing "not the latest" can now mean: the tip is
            // derived from the rows, so there is no flag to set false and no way to describe
            // a row as superseded without the row that superseded it actually existing.
            Link inputLink = CreateRandomLink();
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: ApprovalStatus.Approved,
                createdBy: actorUserId);

            Link supersedingLink = CreateSupersedingLink(storageLink);
            SetupGroupTipRead(storageLink, supersedingLink);
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: securityContext);

            var invalidLinkProcessingException =
                new InvalidLinkProcessingException(
                    message: "Only the latest version of a link may be modified.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkProcessingService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            // the setup said what it meant: same group, strictly higher version, alive
            supersedingLink.GroupId.Should().Be(storageLink.GroupId);
            supersedingLink.Version.Should().BeGreaterThan(storageLink.Version);
            supersedingLink.IsDeleted.Should().BeFalse();

            // and the refusal came from reading the group, not from trusting the row
            this.linkServiceMock.Verify(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.linkServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft, null)]
        [InlineData(ApprovalStatus.Submitted, null)]
        [InlineData(ApprovalStatus.Dismissed, null)]
        [InlineData(ApprovalStatus.Approved, null)]
        [InlineData(ApprovalStatus.Approved, Roles.Reviewers)]
        [InlineData(ApprovalStatus.Approved, Roles.LinkReviewers)]
        [InlineData(ApprovalStatus.Approved, Roles.Publishers)]
        [InlineData(ApprovalStatus.Approved, Roles.LinkPublishers)]
        [InlineData(ApprovalStatus.Approved, Roles.Administrators)]
        [InlineData(ApprovalStatus.Rejected, null)]
        [InlineData(ApprovalStatus.Rejected, Roles.Reviewers)]
        [InlineData(ApprovalStatus.Rejected, Roles.LinkReviewers)]
        [InlineData(ApprovalStatus.Rejected, Roles.Publishers)]
        [InlineData(ApprovalStatus.Rejected, Roles.LinkPublishers)]
        [InlineData(ApprovalStatus.Rejected, Roles.Administrators)]
        public async Task ShouldThrowValidationExceptionOnModifyIfActorIsNotPermittedAndLogItAsync(
            ApprovalStatus approvalStatus,
            string? actorRole)
        {
            // given: a plain authenticated user never touches someone else's link, and a
            // terminal link — Approved or Rejected — belongs to its owner alone: no role
            // (Reviewer, Publisher or Admin) may modify it on the owner's behalf, because
            // the only edit a terminal row admits is a fork, and a moderator forking
            // someone else's decided row would author a version in their name
            Link randomLink = CreateRandomLink();
            Link inputLink = randomLink;

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: approvalStatus,
                createdBy: GetRandomString());

            SetupGroupTipRead(storageLink);

            string[] actorRoles = actorRole is null
                ? Array.Empty<string>()
                : new[] { actorRole };

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(actorRoles);

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: securityContext);

            var unauthorizedLinkProcessingException =
                new UnauthorizedLinkProcessingException(
                    message: "The current user is not allowed to modify this link.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ValueTask<Link> modifyLinkTask =
                this.linkProcessingService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    modifyLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.linkServiceMock.Verify(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.linkServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
