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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllLinksAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            IQueryable<Link> randomLinks = CreateRandomLinks();

            foreach (Link link in randomLinks)
            {
                link.IsDeleted = false;
            }

            IQueryable<Link> storageLinks = randomLinks;
            IQueryable<Link> expectedLinks = storageLinks;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            // when
            IQueryable<Link> actualLinks =
                await this.linkService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOnlyPublicLinksWhenCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Link publicLink = CreateRandomLink();
            publicLink.IsDeleted = false;
            publicLink.ApprovalStatus = ApprovalStatus.Approved;
            publicLink.IsPublished = true;
            publicLink.PublishDate = null;

            Link pastPublishedLink = CreateRandomLink();
            pastPublishedLink.IsDeleted = false;
            pastPublishedLink.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedLink.IsPublished = true;
            pastPublishedLink.PublishDate = randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            Link draftLink = CreateRandomLink();
            draftLink.IsDeleted = false;
            draftLink.ApprovalStatus = ApprovalStatus.Draft;
            draftLink.IsPublished = false;

            Link futurePublishedLink = CreateRandomLink();
            futurePublishedLink.IsDeleted = false;
            futurePublishedLink.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedLink.IsPublished = true;
            futurePublishedLink.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            Link deletedLink = CreateRandomLink();
            deletedLink.IsDeleted = true;
            deletedLink.ApprovalStatus = ApprovalStatus.Approved;
            deletedLink.IsPublished = true;
            deletedLink.PublishDate = null;

            IQueryable<Link> storageLinks = new List<Link>
            {
                publicLink,
                pastPublishedLink,
                draftLink,
                futurePublishedLink,
                deletedLink
            }.AsQueryable();

            IQueryable<Link> expectedLinks = new List<Link>
            {
                publicLink,
                pastPublishedLink
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<Link> actualLinks =
                await this.linkService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllPublicAndOwnLinksWhenUserHasNoReviewRoleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Link publicLink = CreateRandomLink();
            publicLink.IsDeleted = false;
            publicLink.ApprovalStatus = ApprovalStatus.Approved;
            publicLink.IsPublished = true;
            publicLink.PublishDate = null;

            Link ownDraftLink = CreateRandomLink();
            ownDraftLink.IsDeleted = false;
            ownDraftLink.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftLink.IsPublished = false;
            ownDraftLink.CreatedBy = randomActorUserId;

            Link othersDraftLink = CreateRandomLink();
            othersDraftLink.IsDeleted = false;
            othersDraftLink.ApprovalStatus = ApprovalStatus.Draft;
            othersDraftLink.IsPublished = false;

            Link ownDeletedLink = CreateRandomLink();
            ownDeletedLink.IsDeleted = true;
            ownDeletedLink.CreatedBy = randomActorUserId;

            IQueryable<Link> storageLinks = new List<Link>
            {
                publicLink,
                ownDraftLink,
                othersDraftLink,
                ownDeletedLink
            }.AsQueryable();

            IQueryable<Link> expectedLinks = new List<Link>
            {
                publicLink,
                ownDraftLink
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Link> actualLinks =
                await this.linkService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRetrieveAllNonDeletedLinksWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no clock, no
            // user-id resolution
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            Link publicLink = CreateRandomLink();
            publicLink.IsDeleted = false;
            publicLink.ApprovalStatus = ApprovalStatus.Approved;
            publicLink.IsPublished = true;
            publicLink.PublishDate = null;

            Link draftLink = CreateRandomLink();
            draftLink.IsDeleted = false;
            draftLink.ApprovalStatus = ApprovalStatus.Draft;
            draftLink.IsPublished = false;

            Link futurePublishedLink = CreateRandomLink();
            futurePublishedLink.IsDeleted = false;
            futurePublishedLink.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedLink.IsPublished = true;
            futurePublishedLink.PublishDate = GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            Link deletedLink = CreateRandomLink();
            deletedLink.IsDeleted = true;

            IQueryable<Link> storageLinks = new List<Link>
            {
                publicLink,
                draftLink,
                futurePublishedLink,
                deletedLink
            }.AsQueryable();

            IQueryable<Link> expectedLinks = new List<Link>
            {
                publicLink,
                draftLink,
                futurePublishedLink
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            // when
            IQueryable<Link> actualLinks =
                await this.linkService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
