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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllAssociationsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);

            IQueryable<Association> randomAssociations =
                CreateRandomAssociations();

            foreach (Association association in randomAssociations)
            {
                association.IsDeleted = false;
            }

            IQueryable<Association> storageAssociations = randomAssociations;
            IQueryable<Association> expectedAssociations = storageAssociations;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOnlyPublicAssociationsWhenCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association publicAssociation = CreateRandomAssociation();
            publicAssociation.IsDeleted = false;
            publicAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicAssociation.IsPublished = true;
            publicAssociation.PublishDate = null;

            Association pastPublishedAssociation = CreateRandomAssociation();
            pastPublishedAssociation.IsDeleted = false;
            pastPublishedAssociation.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedAssociation.IsPublished = true;

            pastPublishedAssociation.PublishDate =
                randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            Association draftAssociation = CreateRandomAssociation();
            draftAssociation.IsDeleted = false;
            draftAssociation.ApprovalStatus = ApprovalStatus.Draft;
            draftAssociation.IsPublished = false;

            Association futurePublishedAssociation = CreateRandomAssociation();
            futurePublishedAssociation.IsDeleted = false;
            futurePublishedAssociation.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedAssociation.IsPublished = true;

            futurePublishedAssociation.PublishDate =
                randomDateTimeOffset.AddDays(GetRandomNumber());

            Association deletedAssociation = CreateRandomAssociation();
            deletedAssociation.IsDeleted = true;
            deletedAssociation.ApprovalStatus = ApprovalStatus.Approved;
            deletedAssociation.IsPublished = true;
            deletedAssociation.PublishDate = null;

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    publicAssociation,
                    pastPublishedAssociation,
                    draftAssociation,
                    futurePublishedAssociation,
                    deletedAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    publicAssociation,
                    pastPublishedAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllPublicAndOwnAssociationsWhenUserHasNoReviewRoleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association publicAssociation = CreateRandomAssociation();
            publicAssociation.IsDeleted = false;
            publicAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicAssociation.IsPublished = true;
            publicAssociation.PublishDate = null;

            Association ownDraftAssociation = CreateRandomAssociation();
            ownDraftAssociation.IsDeleted = false;
            ownDraftAssociation.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftAssociation.IsPublished = false;
            ownDraftAssociation.CreatedBy = randomActorUserId;

            Association othersDraftAssociation = CreateRandomAssociation();
            othersDraftAssociation.IsDeleted = false;
            othersDraftAssociation.ApprovalStatus = ApprovalStatus.Draft;
            othersDraftAssociation.IsPublished = false;

            Association ownDeletedAssociation = CreateRandomAssociation();
            ownDeletedAssociation.IsDeleted = true;
            ownDeletedAssociation.CreatedBy = randomActorUserId;

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    publicAssociation,
                    ownDraftAssociation,
                    othersDraftAssociation,
                    ownDeletedAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    publicAssociation,
                    ownDraftAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllNonDeletedAssociationsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no clock, no
            // user-id resolution
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            Association publicAssociation = CreateRandomAssociation();
            publicAssociation.IsDeleted = false;
            publicAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicAssociation.IsPublished = true;
            publicAssociation.PublishDate = null;

            Association draftAssociation = CreateRandomAssociation();
            draftAssociation.IsDeleted = false;
            draftAssociation.ApprovalStatus = ApprovalStatus.Draft;
            draftAssociation.IsPublished = false;

            Association futurePublishedAssociation = CreateRandomAssociation();
            futurePublishedAssociation.IsDeleted = false;
            futurePublishedAssociation.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedAssociation.IsPublished = true;

            futurePublishedAssociation.PublishDate =
                GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            Association deletedAssociation = CreateRandomAssociation();
            deletedAssociation.IsDeleted = true;

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    publicAssociation,
                    draftAssociation,
                    futurePublishedAssociation,
                    deletedAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    publicAssociation,
                    draftAssociation,
                    futurePublishedAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
