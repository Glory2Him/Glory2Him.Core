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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmitIfIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.UpsertDataList(
                key: nameof(Link.Id),
                value: "Id is required");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            // when
            ValueTask<Link> submitTask =
                this.linkService.SubmitLinkByIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            // an invalid id never reaches storage
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectLinkByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNotAuthenticatedAsync(
            SecurityContext unauthenticatedContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedContext;

            // when
            ValueTask<Link> submitTask =
                this.linkService.SubmitLinkByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<LinkValidationException>(submitTask.AsTask);

            // then: the contribution gate refuses before any row is read
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectLinkByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.LinkReadOnly)]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsBlockedFromContributingAsync(
            string blockingRole)
        {
            // given: a read-only caller is blocked from every write, submit included, before the
            // row is even read
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockingRole);

            var unauthorizedLinkException =
                new UnauthorizedLinkException(
                    message: "The current user is blocked from contributing links.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkException);

            // when
            ValueTask<Link> submitTask =
                this.linkService.SubmitLinkByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectLinkByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid linkId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    linkId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Link)null);

            var notFoundLinkException =
                new NotFoundLinkException(
                    message: $"Link not found with id: {linkId}.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: notFoundLinkException);

            // when
            ValueTask<Link> submitTask =
                this.linkService.SubmitLinkByIdAsync(
                    linkId,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsSoftDeletedAsync()
        {
            // given: a soft-removed row is reported as not-found, matching the read posture
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Link storageLink = CreateSubmittableStorageLink();
            storageLink.IsDeleted = true;

            SetupLinkStorageRead(storageLink);

            var notFoundLinkException =
                new NotFoundLinkException(
                    message: $"Link not found with id: {storageLink.Id}.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: notFoundLinkException);

            // when
            ValueTask<Link> submitTask =
                this.linkService.SubmitLinkByIdAsync(
                    storageLink.Id,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNeitherOwnerNorPublisherAsync(
            string[] roles)
        {
            // given: a caller who neither owns the row nor holds the publisher tier may not
            // submit it. A reviewer is included among the role sets: they hold write permission
            // on content, but moving a submission status is never theirs (§8.6 HR-3).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            Link storageLink = CreateSubmittableStorageLink();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"not-the-owner-{Guid.NewGuid()}");

            SetupLinkStorageRead(storageLink);

            var unauthorizedLinkException =
                new UnauthorizedLinkException(
                    message: "The current user is not allowed to submit this link.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkException);

            // when
            ValueTask<Link> submitTask =
                this.linkService.SubmitLinkByIdAsync(
                    storageLink.Id,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnSubmitIfTheStoredRowIsNotDraftAsync(
            ApprovalStatus storageStatus)
        {
            // given: only a Draft may be submitted (issue #111 case 7). A row already Submitted
            // or Approved is not a fresh submission — re-submitting one would either re-open a
            // decided item or re-announce a pending one. The caller is the owner, so this proves
            // the state gate stands on its own, after authorization passes.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Link storageLink = CreateSubmittableStorageLink();
            storageLink.ApprovalStatus = storageStatus;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageLink.CreatedBy);

            SetupLinkStorageRead(storageLink);

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link cannot be submitted from status " +
                        $"{storageStatus}.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            // when
            ValueTask<Link> submitTask =
                this.linkService.SubmitLinkByIdAsync(
                    storageLink.Id,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(submitTask.AsTask);

            // then: nothing written, nothing announced
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        It.IsAny<LinkEventOperation>()),
                Times.Never);
        }
    }
}
