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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllContentItemAssociationsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);

            IQueryable<ContentItemAssociation> randomContentItemAssociations =
                CreateRandomContentItemAssociations();

            foreach (ContentItemAssociation contentItemAssociation in randomContentItemAssociations)
            {
                contentItemAssociation.IsDeleted = false;
            }

            IQueryable<ContentItemAssociation> storageContentItemAssociations = randomContentItemAssociations;
            IQueryable<ContentItemAssociation> expectedContentItemAssociations = storageContentItemAssociations;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItemAssociations);

            // when
            IQueryable<ContentItemAssociation> actualContentItemAssociations =
                await this.contentItemAssociationService.RetrieveAllContentItemAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociations.Should().BeEquivalentTo(expectedContentItemAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOnlyPublicContentItemAssociationsWhenCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItemAssociation publicContentItemAssociation = CreateRandomContentItemAssociation();
            publicContentItemAssociation.IsDeleted = false;
            publicContentItemAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicContentItemAssociation.IsPublished = true;
            publicContentItemAssociation.PublishDate = null;

            ContentItemAssociation pastPublishedContentItemAssociation = CreateRandomContentItemAssociation();
            pastPublishedContentItemAssociation.IsDeleted = false;
            pastPublishedContentItemAssociation.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedContentItemAssociation.IsPublished = true;

            pastPublishedContentItemAssociation.PublishDate =
                randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            ContentItemAssociation draftContentItemAssociation = CreateRandomContentItemAssociation();
            draftContentItemAssociation.IsDeleted = false;
            draftContentItemAssociation.ApprovalStatus = ApprovalStatus.Draft;
            draftContentItemAssociation.IsPublished = false;

            ContentItemAssociation futurePublishedContentItemAssociation = CreateRandomContentItemAssociation();
            futurePublishedContentItemAssociation.IsDeleted = false;
            futurePublishedContentItemAssociation.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedContentItemAssociation.IsPublished = true;

            futurePublishedContentItemAssociation.PublishDate =
                randomDateTimeOffset.AddDays(GetRandomNumber());

            ContentItemAssociation deletedContentItemAssociation = CreateRandomContentItemAssociation();
            deletedContentItemAssociation.IsDeleted = true;
            deletedContentItemAssociation.ApprovalStatus = ApprovalStatus.Approved;
            deletedContentItemAssociation.IsPublished = true;
            deletedContentItemAssociation.PublishDate = null;

            IQueryable<ContentItemAssociation> storageContentItemAssociations =
                new List<ContentItemAssociation>
                {
                    publicContentItemAssociation,
                    pastPublishedContentItemAssociation,
                    draftContentItemAssociation,
                    futurePublishedContentItemAssociation,
                    deletedContentItemAssociation
                }.AsQueryable();

            IQueryable<ContentItemAssociation> expectedContentItemAssociations =
                new List<ContentItemAssociation>
                {
                    publicContentItemAssociation,
                    pastPublishedContentItemAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItemAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<ContentItemAssociation> actualContentItemAssociations =
                await this.contentItemAssociationService.RetrieveAllContentItemAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociations.Should().BeEquivalentTo(expectedContentItemAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemAssociationsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllPublicAndOwnContentItemAssociationsWhenUserHasNoReviewRoleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItemAssociation publicContentItemAssociation = CreateRandomContentItemAssociation();
            publicContentItemAssociation.IsDeleted = false;
            publicContentItemAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicContentItemAssociation.IsPublished = true;
            publicContentItemAssociation.PublishDate = null;

            ContentItemAssociation ownDraftContentItemAssociation = CreateRandomContentItemAssociation();
            ownDraftContentItemAssociation.IsDeleted = false;
            ownDraftContentItemAssociation.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftContentItemAssociation.IsPublished = false;
            ownDraftContentItemAssociation.CreatedBy = randomActorUserId;

            ContentItemAssociation othersDraftContentItemAssociation = CreateRandomContentItemAssociation();
            othersDraftContentItemAssociation.IsDeleted = false;
            othersDraftContentItemAssociation.ApprovalStatus = ApprovalStatus.Draft;
            othersDraftContentItemAssociation.IsPublished = false;

            ContentItemAssociation ownDeletedContentItemAssociation = CreateRandomContentItemAssociation();
            ownDeletedContentItemAssociation.IsDeleted = true;
            ownDeletedContentItemAssociation.CreatedBy = randomActorUserId;

            IQueryable<ContentItemAssociation> storageContentItemAssociations =
                new List<ContentItemAssociation>
                {
                    publicContentItemAssociation,
                    ownDraftContentItemAssociation,
                    othersDraftContentItemAssociation,
                    ownDeletedContentItemAssociation
                }.AsQueryable();

            IQueryable<ContentItemAssociation> expectedContentItemAssociations =
                new List<ContentItemAssociation>
                {
                    publicContentItemAssociation,
                    ownDraftContentItemAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItemAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<ContentItemAssociation> actualContentItemAssociations =
                await this.contentItemAssociationService.RetrieveAllContentItemAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociations.Should().BeEquivalentTo(expectedContentItemAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemAssociationsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllNonDeletedContentItemAssociationsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no clock, no
            // user-id resolution
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            ContentItemAssociation publicContentItemAssociation = CreateRandomContentItemAssociation();
            publicContentItemAssociation.IsDeleted = false;
            publicContentItemAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicContentItemAssociation.IsPublished = true;
            publicContentItemAssociation.PublishDate = null;

            ContentItemAssociation draftContentItemAssociation = CreateRandomContentItemAssociation();
            draftContentItemAssociation.IsDeleted = false;
            draftContentItemAssociation.ApprovalStatus = ApprovalStatus.Draft;
            draftContentItemAssociation.IsPublished = false;

            ContentItemAssociation futurePublishedContentItemAssociation = CreateRandomContentItemAssociation();
            futurePublishedContentItemAssociation.IsDeleted = false;
            futurePublishedContentItemAssociation.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedContentItemAssociation.IsPublished = true;

            futurePublishedContentItemAssociation.PublishDate =
                GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            ContentItemAssociation deletedContentItemAssociation = CreateRandomContentItemAssociation();
            deletedContentItemAssociation.IsDeleted = true;

            IQueryable<ContentItemAssociation> storageContentItemAssociations =
                new List<ContentItemAssociation>
                {
                    publicContentItemAssociation,
                    draftContentItemAssociation,
                    futurePublishedContentItemAssociation,
                    deletedContentItemAssociation
                }.AsQueryable();

            IQueryable<ContentItemAssociation> expectedContentItemAssociations =
                new List<ContentItemAssociation>
                {
                    publicContentItemAssociation,
                    draftContentItemAssociation,
                    futurePublishedContentItemAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItemAssociations);

            // when
            IQueryable<ContentItemAssociation> actualContentItemAssociations =
                await this.contentItemAssociationService.RetrieveAllContentItemAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociations.Should().BeEquivalentTo(expectedContentItemAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
