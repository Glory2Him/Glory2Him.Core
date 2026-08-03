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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllContentTypesAsync()
        {
            // given: an Admin caller sees every non-deleted row — no clock needed
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            IQueryable<ContentType> randomContentTypes = CreateRandomContentTypes();

            foreach (ContentType contentType in randomContentTypes)
            {
                contentType.IsDeleted = false;
            }

            IQueryable<ContentType> storageContentTypes = randomContentTypes;
            IQueryable<ContentType> expectedContentTypes = storageContentTypes;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentTypesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentTypes);

            // when
            IQueryable<ContentType> actualContentTypes =
                await this.contentTypeService.RetrieveAllContentTypesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentTypes.Should().BeEquivalentTo(expectedContentTypes);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentTypesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllNonDeletedContentTypesWhenUserIsAdminAsync()
        {
            // given: Admin sees drafts and future-published rows, but never deleted ones
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);

            ContentType publicContentType = CreateRandomContentType();
            publicContentType.IsDeleted = false;
            publicContentType.ApprovalStatus = ApprovalStatus.Approved;
            publicContentType.IsPublished = true;
            publicContentType.PublishDate = null;

            ContentType draftContentType = CreateRandomContentType();
            draftContentType.IsDeleted = false;
            draftContentType.ApprovalStatus = ApprovalStatus.Draft;
            draftContentType.IsPublished = false;

            ContentType deletedContentType = CreateRandomContentType();
            deletedContentType.IsDeleted = true;

            IQueryable<ContentType> storageContentTypes = new List<ContentType>
            {
                publicContentType,
                draftContentType,
                deletedContentType
            }.AsQueryable();

            IQueryable<ContentType> expectedContentTypes = new List<ContentType>
            {
                publicContentType,
                draftContentType
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentTypesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentTypes);

            // when
            IQueryable<ContentType> actualContentTypes =
                await this.contentTypeService.RetrieveAllContentTypesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentTypes.Should().BeEquivalentTo(expectedContentTypes);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentTypesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOnlyPublicContentTypesWhenCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentType publicContentType = CreateRandomContentType();
            publicContentType.IsDeleted = false;
            publicContentType.ApprovalStatus = ApprovalStatus.Approved;
            publicContentType.IsPublished = true;
            publicContentType.PublishDate = null;

            ContentType pastPublishedContentType = CreateRandomContentType();
            pastPublishedContentType.IsDeleted = false;
            pastPublishedContentType.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedContentType.IsPublished = true;
            pastPublishedContentType.PublishDate = randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            ContentType draftContentType = CreateRandomContentType();
            draftContentType.IsDeleted = false;
            draftContentType.ApprovalStatus = ApprovalStatus.Draft;
            draftContentType.IsPublished = false;

            ContentType futurePublishedContentType = CreateRandomContentType();
            futurePublishedContentType.IsDeleted = false;
            futurePublishedContentType.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedContentType.IsPublished = true;
            futurePublishedContentType.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            ContentType deletedContentType = CreateRandomContentType();
            deletedContentType.IsDeleted = true;
            deletedContentType.ApprovalStatus = ApprovalStatus.Approved;
            deletedContentType.IsPublished = true;
            deletedContentType.PublishDate = null;

            IQueryable<ContentType> storageContentTypes = new List<ContentType>
            {
                publicContentType,
                pastPublishedContentType,
                draftContentType,
                futurePublishedContentType,
                deletedContentType
            }.AsQueryable();

            IQueryable<ContentType> expectedContentTypes = new List<ContentType>
            {
                publicContentType,
                pastPublishedContentType
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentTypesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentTypes);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<ContentType> actualContentTypes =
                await this.contentTypeService.RetrieveAllContentTypesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentTypes.Should().BeEquivalentTo(expectedContentTypes);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentTypesAsync(It.IsAny<CancellationToken>()),
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

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldRetrieveAllOnlyPublicContentTypesWhenUserIsNotAdminAsync(
            string[] nonAdminRoles)
        {
            // given: unlike user-contributed content there is no public-plus-own branch —
            // a non-Admin caller sees exactly what an anonymous caller sees
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentType publicContentType = CreateRandomContentType();
            publicContentType.IsDeleted = false;
            publicContentType.ApprovalStatus = ApprovalStatus.Approved;
            publicContentType.IsPublished = true;
            publicContentType.PublishDate = null;

            ContentType draftContentType = CreateRandomContentType();
            draftContentType.IsDeleted = false;
            draftContentType.ApprovalStatus = ApprovalStatus.Draft;
            draftContentType.IsPublished = false;

            ContentType deletedContentType = CreateRandomContentType();
            deletedContentType.IsDeleted = true;
            deletedContentType.ApprovalStatus = ApprovalStatus.Approved;
            deletedContentType.IsPublished = true;
            deletedContentType.PublishDate = null;

            IQueryable<ContentType> storageContentTypes = new List<ContentType>
            {
                publicContentType,
                draftContentType,
                deletedContentType
            }.AsQueryable();

            IQueryable<ContentType> expectedContentTypes = new List<ContentType>
            {
                publicContentType
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentTypesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentTypes);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<ContentType> actualContentTypes =
                await this.contentTypeService.RetrieveAllContentTypesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentTypes.Should().BeEquivalentTo(expectedContentTypes);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentTypesAsync(It.IsAny<CancellationToken>()),
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
    }
}
