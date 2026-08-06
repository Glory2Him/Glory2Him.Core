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
using Glory2Him.Core.Models.Foundations.Associations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAssociationByIdAsync()
        {
            // given
            Association randomAssociation = CreateRandomAssociation();
            Association storageAssociation = randomAssociation;
            storageAssociation.IsDeleted = false;
            storageAssociation.ApprovalStatus = ApprovalStatus.Approved;
            storageAssociation.IsPublished = true;
            storageAssociation.PublishDate = null;
            Association expectedAssociation = storageAssociation;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            Association actualAssociation =
                await this.associationService.RetrieveAssociationByIdAsync(
                    randomAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
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
        public async Task ShouldRetrieveNonPublicAssociationByIdWhenUserIsOwnerAsync()
        {
            // given
            Association randomAssociation = CreateRandomAssociation();
            Association storageAssociation = randomAssociation;
            storageAssociation.IsDeleted = false;
            storageAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageAssociation.IsPublished = false;
            Association expectedAssociation = storageAssociation;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageAssociation.CreatedBy);

            // when
            Association actualAssociation =
                await this.associationService.RetrieveAssociationByIdAsync(
                    randomAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
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
        public async Task ShouldRetrieveNonPublicAssociationByIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the caller is not the owner but holds a review role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            string randomActorUserId = GetRandomString();
            Association randomAssociation = CreateRandomAssociation();
            Association storageAssociation = randomAssociation;
            storageAssociation.IsDeleted = false;
            storageAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageAssociation.IsPublished = false;
            Association expectedAssociation = storageAssociation;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            Association actualAssociation =
                await this.associationService.RetrieveAssociationByIdAsync(
                    randomAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
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
