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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveContentItemAssociationByIdAsync()
        {
            // given
            ContentItemAssociation randomContentItemAssociation = CreateRandomContentItemAssociation();
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation;
            storageContentItemAssociation.IsDeleted = false;
            storageContentItemAssociation.ApprovalStatus = ApprovalStatus.Approved;
            storageContentItemAssociation.IsPublished = true;
            storageContentItemAssociation.PublishDate = null;
            ContentItemAssociation expectedContentItemAssociation = storageContentItemAssociation;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            ContentItemAssociation actualContentItemAssociation =
                await this.contentItemAssociationService.RetrieveContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociation.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
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
        public async Task ShouldRetrieveNonPublicContentItemAssociationByIdWhenUserIsOwnerAsync()
        {
            // given
            ContentItemAssociation randomContentItemAssociation = CreateRandomContentItemAssociation();
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation;
            storageContentItemAssociation.IsDeleted = false;
            storageContentItemAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageContentItemAssociation.IsPublished = false;
            ContentItemAssociation expectedContentItemAssociation = storageContentItemAssociation;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageContentItemAssociation.CreatedBy);

            // when
            ContentItemAssociation actualContentItemAssociation =
                await this.contentItemAssociationService.RetrieveContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociation.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
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
        public async Task ShouldRetrieveNonPublicContentItemAssociationByIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the caller is not the owner but holds a review role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            string randomActorUserId = GetRandomString();
            ContentItemAssociation randomContentItemAssociation = CreateRandomContentItemAssociation();
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation;
            storageContentItemAssociation.IsDeleted = false;
            storageContentItemAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageContentItemAssociation.IsPublished = false;
            ContentItemAssociation expectedContentItemAssociation = storageContentItemAssociation;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ContentItemAssociation actualContentItemAssociation =
                await this.contentItemAssociationService.RetrieveContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociation.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
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
