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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveContentItemByIdAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem storageContentItem = randomContentItem;
            storageContentItem.IsDeleted = false;
            storageContentItem.ApprovalStatus = ApprovalStatus.Approved;
            storageContentItem.IsPublished = true;
            storageContentItem.PublishDate = null;
            ContentItem expectedContentItem = storageContentItem;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            ContentItem actualContentItem =
                await this.contentItemService.RetrieveContentItemByIdAsync(
                    randomContentItem.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
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
        public async Task ShouldRetrieveNonPublicContentItemByIdWhenUserIsOwnerAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem storageContentItem = randomContentItem;
            storageContentItem.IsDeleted = false;
            storageContentItem.ApprovalStatus = ApprovalStatus.Draft;
            storageContentItem.IsPublished = false;
            ContentItem expectedContentItem = storageContentItem;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageContentItem.CreatedBy);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.RetrieveContentItemByIdAsync(
                    randomContentItem.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
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
        public async Task ShouldRetrieveNonPublicContentItemByIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the caller is not the owner but holds a review role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            string randomActorUserId = GetRandomString();
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem storageContentItem = randomContentItem;
            storageContentItem.IsDeleted = false;
            storageContentItem.ApprovalStatus = ApprovalStatus.Draft;
            storageContentItem.IsPublished = false;
            ContentItem expectedContentItem = storageContentItem;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.RetrieveContentItemByIdAsync(
                    randomContentItem.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(
                    randomContentItem.Id,
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
