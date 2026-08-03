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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllBibleReferencesAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            IQueryable<BibleReference> randomBibleReferences = CreateRandomBibleReferences();

            foreach (BibleReference bibleReference in randomBibleReferences)
            {
                bibleReference.IsDeleted = false;
            }

            IQueryable<BibleReference> storageBibleReferences = randomBibleReferences;
            IQueryable<BibleReference> expectedBibleReferences = storageBibleReferences;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllBibleReferencesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageBibleReferences);

            // when
            IQueryable<BibleReference> actualBibleReferences =
                await this.bibleReferenceService.RetrieveAllBibleReferencesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReferences.Should().BeEquivalentTo(expectedBibleReferences);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllBibleReferencesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOnlyPublicBibleReferencesWhenCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            BibleReference publicBibleReference = CreateRandomBibleReference();
            publicBibleReference.IsDeleted = false;
            publicBibleReference.ApprovalStatus = ApprovalStatus.Approved;
            publicBibleReference.IsPublished = true;
            publicBibleReference.PublishDate = null;

            BibleReference pastPublishedBibleReference = CreateRandomBibleReference();
            pastPublishedBibleReference.IsDeleted = false;
            pastPublishedBibleReference.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedBibleReference.IsPublished = true;
            pastPublishedBibleReference.PublishDate = randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            BibleReference draftBibleReference = CreateRandomBibleReference();
            draftBibleReference.IsDeleted = false;
            draftBibleReference.ApprovalStatus = ApprovalStatus.Draft;
            draftBibleReference.IsPublished = false;

            BibleReference futurePublishedBibleReference = CreateRandomBibleReference();
            futurePublishedBibleReference.IsDeleted = false;
            futurePublishedBibleReference.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedBibleReference.IsPublished = true;
            futurePublishedBibleReference.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            BibleReference deletedBibleReference = CreateRandomBibleReference();
            deletedBibleReference.IsDeleted = true;
            deletedBibleReference.ApprovalStatus = ApprovalStatus.Approved;
            deletedBibleReference.IsPublished = true;
            deletedBibleReference.PublishDate = null;

            IQueryable<BibleReference> storageBibleReferences = new List<BibleReference>
            {
                publicBibleReference,
                pastPublishedBibleReference,
                draftBibleReference,
                futurePublishedBibleReference,
                deletedBibleReference
            }.AsQueryable();

            IQueryable<BibleReference> expectedBibleReferences = new List<BibleReference>
            {
                publicBibleReference,
                pastPublishedBibleReference
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllBibleReferencesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageBibleReferences);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<BibleReference> actualBibleReferences =
                await this.bibleReferenceService.RetrieveAllBibleReferencesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReferences.Should().BeEquivalentTo(expectedBibleReferences);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllBibleReferencesAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllPublicAndOwnBibleReferencesWhenUserHasNoReviewRoleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            BibleReference publicBibleReference = CreateRandomBibleReference();
            publicBibleReference.IsDeleted = false;
            publicBibleReference.ApprovalStatus = ApprovalStatus.Approved;
            publicBibleReference.IsPublished = true;
            publicBibleReference.PublishDate = null;

            BibleReference ownDraftBibleReference = CreateRandomBibleReference();
            ownDraftBibleReference.IsDeleted = false;
            ownDraftBibleReference.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftBibleReference.IsPublished = false;
            ownDraftBibleReference.CreatedBy = randomActorUserId;

            BibleReference othersDraftBibleReference = CreateRandomBibleReference();
            othersDraftBibleReference.IsDeleted = false;
            othersDraftBibleReference.ApprovalStatus = ApprovalStatus.Draft;
            othersDraftBibleReference.IsPublished = false;

            BibleReference ownDeletedBibleReference = CreateRandomBibleReference();
            ownDeletedBibleReference.IsDeleted = true;
            ownDeletedBibleReference.CreatedBy = randomActorUserId;

            IQueryable<BibleReference> storageBibleReferences = new List<BibleReference>
            {
                publicBibleReference,
                ownDraftBibleReference,
                othersDraftBibleReference,
                ownDeletedBibleReference
            }.AsQueryable();

            IQueryable<BibleReference> expectedBibleReferences = new List<BibleReference>
            {
                publicBibleReference,
                ownDraftBibleReference
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllBibleReferencesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageBibleReferences);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<BibleReference> actualBibleReferences =
                await this.bibleReferenceService.RetrieveAllBibleReferencesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReferences.Should().BeEquivalentTo(expectedBibleReferences);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllBibleReferencesAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllNonDeletedBibleReferencesWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no clock, no
            // user-id resolution
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            BibleReference publicBibleReference = CreateRandomBibleReference();
            publicBibleReference.IsDeleted = false;
            publicBibleReference.ApprovalStatus = ApprovalStatus.Approved;
            publicBibleReference.IsPublished = true;
            publicBibleReference.PublishDate = null;

            BibleReference draftBibleReference = CreateRandomBibleReference();
            draftBibleReference.IsDeleted = false;
            draftBibleReference.ApprovalStatus = ApprovalStatus.Draft;
            draftBibleReference.IsPublished = false;

            BibleReference futurePublishedBibleReference = CreateRandomBibleReference();
            futurePublishedBibleReference.IsDeleted = false;
            futurePublishedBibleReference.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedBibleReference.IsPublished = true;
            futurePublishedBibleReference.PublishDate = GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            BibleReference deletedBibleReference = CreateRandomBibleReference();
            deletedBibleReference.IsDeleted = true;

            IQueryable<BibleReference> storageBibleReferences = new List<BibleReference>
            {
                publicBibleReference,
                draftBibleReference,
                futurePublishedBibleReference,
                deletedBibleReference
            }.AsQueryable();

            IQueryable<BibleReference> expectedBibleReferences = new List<BibleReference>
            {
                publicBibleReference,
                draftBibleReference,
                futurePublishedBibleReference
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllBibleReferencesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageBibleReferences);

            // when
            IQueryable<BibleReference> actualBibleReferences =
                await this.bibleReferenceService.RetrieveAllBibleReferencesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReferences.Should().BeEquivalentTo(expectedBibleReferences);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllBibleReferencesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
