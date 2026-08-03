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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveBibleReferenceByIdAsync()
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            BibleReference storageBibleReference = randomBibleReference;
            storageBibleReference.IsDeleted = false;
            storageBibleReference.ApprovalStatus = ApprovalStatus.Approved;
            storageBibleReference.IsPublished = true;
            storageBibleReference.PublishDate = null;
            BibleReference expectedBibleReference = storageBibleReference;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageBibleReference);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
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
        public async Task ShouldRetrieveNonPublicBibleReferenceByIdWhenUserIsOwnerAsync()
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            BibleReference storageBibleReference = randomBibleReference;
            storageBibleReference.IsDeleted = false;
            storageBibleReference.ApprovalStatus = ApprovalStatus.Draft;
            storageBibleReference.IsPublished = false;
            BibleReference expectedBibleReference = storageBibleReference;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageBibleReference);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageBibleReference.CreatedBy);

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
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
        public async Task ShouldRetrieveNonPublicBibleReferenceByIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the caller is not the owner but holds a review role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            string randomActorUserId = GetRandomString();
            BibleReference randomBibleReference = CreateRandomBibleReference();
            BibleReference storageBibleReference = randomBibleReference;
            storageBibleReference.IsDeleted = false;
            storageBibleReference.ApprovalStatus = ApprovalStatus.Draft;
            storageBibleReference.IsPublished = false;
            BibleReference expectedBibleReference = storageBibleReference;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageBibleReference);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
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
