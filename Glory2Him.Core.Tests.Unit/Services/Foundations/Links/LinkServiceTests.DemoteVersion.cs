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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    /// <summary>
    /// The version fork's demotion of the previous latest row — the only operation permitted to
    /// move <c>IsLatestVersion</c> (design §3.4 rule 18, §9.7.1 rule 2).
    /// </summary>
    public partial class LinkServiceTests
    {
        // A stored row as the fork finds it: the current tip, and published as an approved one
        // would be, so a test can prove the demotion leaves publication alone.
        private Link CreateDemotableStorageLink(string ownerUserId)
        {
            Link link = CreateRandomLink();
            link.IsDeleted = false;
            link.IsLatestVersion = true;
            link.ApprovalStatus = ApprovalStatus.Approved;
            link.IsPublished = true;
            link.PublishDate = GetRandomDateTimeOffset();
            link.CreatedBy = ownerUserId;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(ownerUserId);

            return link;
        }

        [Fact]
        public async Task ShouldDemoteLinkVersionAsync()
        {
            // given: the write the fork could not make before. IsLatestVersion is an IVersion
            // member and the general modify pins it, so demoting through the modify asked the
            // one path required to refuse this write to make it.
            string ownerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Link storageLink = CreateDemotableStorageLink(ownerUserId);
            Link savedLink = null;

            // Snapshotted BEFORE the act. The service copies onto the very instance the storage
            // read hands back, so asserting against storageLink after the fact would compare the
            // row with itself and pass however the operation behaved — verified: nulling
            // PublishDate in the do-work left all 273 Link tests green.
            Link expectedUntouched = storageLink.DeepClone();

            SetupLinkStorageRead(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Link>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Link entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(
                    It.IsAny<Link>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Link, CancellationToken>(
                            (entity, _) => savedLink = entity.DeepClone())
                        .ReturnsAsync((Link entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<LinkEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Link>>(
                            new EventPublishResult<Link>()));

            // when
            await this.linkService.DemoteLinkVersionAsync(
                storageLink.Id,
                TestContext.Current.CancellationToken);

            // then
            savedLink.IsLatestVersion.Should().BeFalse();

            // IsLatestVersion marks the edit tip and nothing else. The previously published row
            // stays publicly visible until the new version is approved (§3.4.1), so a demotion
            // that touched publication would take the group dark mid-fork.
            savedLink.IsPublished.Should().BeTrue();
            savedLink.PublishDate.Should().Be(expectedUntouched.PublishDate);
            savedLink.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            // its OWN fact — never Modified, which the approval workflow reads as an amendment
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Demoted),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Modified),
                Times.Never);
        }

        [Theory]
        [InlineData(Roles.Publisher)]
        [InlineData(Roles.Admin)]
        [InlineData(Roles.Reviewer)]
        public async Task ShouldThrowUnauthorizedOnDemoteIfCallerIsNotTheOwnerAsync(
            string role)
        {
            // given: §3.4 rule 8 — the owner is the only creator of new versions, and Publisher
            // and Admin roles never fork one. The demotion is a step inside that fork, so the
            // roles that may otherwise write the row are refused it. A Reviewer holds write
            // permission through the modify and must still never move the version tip.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(role);
            Link storageLink = CreateDemotableStorageLink(GetRandomString());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            SetupLinkStorageRead(storageLink);

            var unauthorizedLinkException =
                new UnauthorizedLinkException(
                    message: "The current user is not allowed to demote this link version.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkException);

            // when
            ValueTask<Link> demoteTask =
                this.linkService.DemoteLinkVersionAsync(
                    storageLink.Id,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(demoteTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDemoteIfTheRowIsNotTheLatestAsync()
        {
            // given: a row that is already not the tip has nothing to demote. Letting the call
            // through would publish a Demoted fact for a write that changed nothing, leaving a
            // subscriber to infer a version move that never happened.
            string ownerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Link storageLink = CreateDemotableStorageLink(ownerUserId);
            storageLink.IsLatestVersion = false;

            SetupLinkStorageRead(storageLink);

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is not the latest version and cannot be demoted.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            // when
            ValueTask<Link> demoteTask =
                this.linkService.DemoteLinkVersionAsync(
                    storageLink.Id,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(demoteTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnDemoteIfTheRowIsSoftDeletedAsync()
        {
            // given: a removed row is a takedown. Reported as not-found, matching the read
            // posture, so a removed id is not distinguishable from one that never existed.
            string ownerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Link storageLink = CreateDemotableStorageLink(ownerUserId);
            storageLink.IsDeleted = true;

            SetupLinkStorageRead(storageLink);

            var notFoundLinkException =
                new NotFoundLinkException(
                    message: $"Link not found with id: {storageLink.Id}.");

            // when
            ValueTask<Link> demoteTask =
                this.linkService.DemoteLinkVersionAsync(
                    storageLink.Id,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(demoteTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeEquivalentTo(notFoundLinkException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        It.IsAny<Link>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDemoteIfIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var invalidLinkException =
                new InvalidLinkException(
                    message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.AddData(
                key: nameof(Link.Id),
                values: "Id is required");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkException);

            // when
            ValueTask<Link> demoteTask =
                this.linkService.DemoteLinkVersionAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(demoteTask.AsTask);

            // then: the row was never read
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectLinkByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
