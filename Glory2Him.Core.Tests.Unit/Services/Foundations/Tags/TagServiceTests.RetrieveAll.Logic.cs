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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllTagsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            IQueryable<Tag> randomTags = CreateRandomTags();

            foreach (Tag tag in randomTags)
            {
                tag.IsDeleted = false;
            }

            IQueryable<Tag> storageTags = randomTags;
            IQueryable<Tag> expectedTags = storageTags;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageTags);

            // when
            IQueryable<Tag> actualTags =
                await this.tagService.RetrieveAllTagsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualTags.Should().BeEquivalentTo(expectedTags);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOnlyPublicTagsWhenCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Tag publicTag = CreateRandomTag();
            publicTag.IsDeleted = false;
            publicTag.ApprovalStatus = ApprovalStatus.Approved;
            publicTag.IsPublished = true;
            publicTag.PublishDate = null;

            Tag pastPublishedTag = CreateRandomTag();
            pastPublishedTag.IsDeleted = false;
            pastPublishedTag.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedTag.IsPublished = true;
            pastPublishedTag.PublishDate = randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            Tag draftTag = CreateRandomTag();
            draftTag.IsDeleted = false;
            draftTag.ApprovalStatus = ApprovalStatus.Draft;
            draftTag.IsPublished = false;

            Tag futurePublishedTag = CreateRandomTag();
            futurePublishedTag.IsDeleted = false;
            futurePublishedTag.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedTag.IsPublished = true;
            futurePublishedTag.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            Tag deletedTag = CreateRandomTag();
            deletedTag.IsDeleted = true;
            deletedTag.ApprovalStatus = ApprovalStatus.Approved;
            deletedTag.IsPublished = true;
            deletedTag.PublishDate = null;

            IQueryable<Tag> storageTags = new List<Tag>
            {
                publicTag,
                pastPublishedTag,
                draftTag,
                futurePublishedTag,
                deletedTag
            }.AsQueryable();

            IQueryable<Tag> expectedTags = new List<Tag>
            {
                publicTag,
                pastPublishedTag
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageTags);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<Tag> actualTags =
                await this.tagService.RetrieveAllTagsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualTags.Should().BeEquivalentTo(expectedTags);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllPublicAndOwnTagsWhenUserHasNoReviewRoleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Tag publicTag = CreateRandomTag();
            publicTag.IsDeleted = false;
            publicTag.ApprovalStatus = ApprovalStatus.Approved;
            publicTag.IsPublished = true;
            publicTag.PublishDate = null;

            Tag ownDraftTag = CreateRandomTag();
            ownDraftTag.IsDeleted = false;
            ownDraftTag.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftTag.IsPublished = false;
            ownDraftTag.CreatedBy = randomActorUserId;

            Tag othersDraftTag = CreateRandomTag();
            othersDraftTag.IsDeleted = false;
            othersDraftTag.ApprovalStatus = ApprovalStatus.Draft;
            othersDraftTag.IsPublished = false;

            Tag ownDeletedTag = CreateRandomTag();
            ownDeletedTag.IsDeleted = true;
            ownDeletedTag.CreatedBy = randomActorUserId;

            IQueryable<Tag> storageTags = new List<Tag>
            {
                publicTag,
                ownDraftTag,
                othersDraftTag,
                ownDeletedTag
            }.AsQueryable();

            IQueryable<Tag> expectedTags = new List<Tag>
            {
                publicTag,
                ownDraftTag
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageTags);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Tag> actualTags =
                await this.tagService.RetrieveAllTagsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualTags.Should().BeEquivalentTo(expectedTags);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllNonDeletedTagsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no clock, no
            // user-id resolution
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            Tag publicTag = CreateRandomTag();
            publicTag.IsDeleted = false;
            publicTag.ApprovalStatus = ApprovalStatus.Approved;
            publicTag.IsPublished = true;
            publicTag.PublishDate = null;

            Tag draftTag = CreateRandomTag();
            draftTag.IsDeleted = false;
            draftTag.ApprovalStatus = ApprovalStatus.Draft;
            draftTag.IsPublished = false;

            Tag futurePublishedTag = CreateRandomTag();
            futurePublishedTag.IsDeleted = false;
            futurePublishedTag.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedTag.IsPublished = true;
            futurePublishedTag.PublishDate = GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            Tag deletedTag = CreateRandomTag();
            deletedTag.IsDeleted = true;

            IQueryable<Tag> storageTags = new List<Tag>
            {
                publicTag,
                draftTag,
                futurePublishedTag,
                deletedTag
            }.AsQueryable();

            IQueryable<Tag> expectedTags = new List<Tag>
            {
                publicTag,
                draftTag,
                futurePublishedTag
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageTags);

            // when
            IQueryable<Tag> actualTags =
                await this.tagService.RetrieveAllTagsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualTags.Should().BeEquivalentTo(expectedTags);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
