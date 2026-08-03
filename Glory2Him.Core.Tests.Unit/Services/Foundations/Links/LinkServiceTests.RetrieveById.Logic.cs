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

using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveLinkByIdAsync()
        {
            // given
            Link randomLink = CreateRandomLink();
            Link storageLink = randomLink;
            storageLink.IsDeleted = false;
            storageLink.ApprovalStatus = ApprovalStatus.Approved;
            storageLink.IsPublished = true;
            storageLink.PublishDate = null;
            Link expectedLink = storageLink;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            Link actualLink =
                await this.linkService.RetrieveLinkByIdAsync(
                    randomLink.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldRetrieveNonPublicLinkByIdWhenUserIsOwnerAsync()
        {
            // given
            Link randomLink = CreateRandomLink();
            Link storageLink = randomLink;
            storageLink.IsDeleted = false;
            storageLink.ApprovalStatus = ApprovalStatus.Draft;
            storageLink.IsPublished = false;
            Link expectedLink = storageLink;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageLink.CreatedBy);

            // when
            Link actualLink =
                await this.linkService.RetrieveLinkByIdAsync(
                    randomLink.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldRetrieveNonPublicLinkByIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the caller is not the owner but holds a review role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            string randomActorUserId = GetRandomString();
            Link randomLink = CreateRandomLink();
            Link storageLink = randomLink;
            storageLink.IsDeleted = false;
            storageLink.ApprovalStatus = ApprovalStatus.Draft;
            storageLink.IsPublished = false;
            Link expectedLink = storageLink;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            Link actualLink =
                await this.linkService.RetrieveLinkByIdAsync(
                    randomLink.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    TestContext.Current.CancellationToken),
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
    }
}
