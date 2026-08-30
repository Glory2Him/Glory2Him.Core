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
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllReactionsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            IQueryable<Reaction> randomReactions = CreateRandomReactions();

            foreach (Reaction reaction in randomReactions)
            {
                reaction.IsDeleted = false;
            }

            IQueryable<Reaction> storageReactions = randomReactions;
            IQueryable<Reaction> expectedReactions = storageReactions;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllReactionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageReactions);

            // when
            IQueryable<Reaction> actualReactions =
                await this.reactionService.RetrieveAllReactionsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualReactions.Should().BeEquivalentTo(expectedReactions);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllReactionsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOnlyPublicReactionsWhenCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Reaction publicReaction = CreateRandomReaction();
            publicReaction.IsDeleted = false;
            publicReaction.ApprovalStatus = ApprovalStatus.Approved;
            publicReaction.IsPublished = true;
            publicReaction.PublishDate = null;

            Reaction pastPublishedReaction = CreateRandomReaction();
            pastPublishedReaction.IsDeleted = false;
            pastPublishedReaction.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedReaction.IsPublished = true;
            pastPublishedReaction.PublishDate = randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            Reaction draftReaction = CreateRandomReaction();
            draftReaction.IsDeleted = false;
            draftReaction.ApprovalStatus = ApprovalStatus.Draft;
            draftReaction.IsPublished = false;

            Reaction futurePublishedReaction = CreateRandomReaction();
            futurePublishedReaction.IsDeleted = false;
            futurePublishedReaction.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedReaction.IsPublished = true;
            futurePublishedReaction.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            Reaction deletedReaction = CreateRandomReaction();
            deletedReaction.IsDeleted = true;
            deletedReaction.ApprovalStatus = ApprovalStatus.Approved;
            deletedReaction.IsPublished = true;
            deletedReaction.PublishDate = null;

            IQueryable<Reaction> storageReactions = new List<Reaction>
            {
                publicReaction,
                pastPublishedReaction,
                draftReaction,
                futurePublishedReaction,
                deletedReaction
            }.AsQueryable();

            IQueryable<Reaction> expectedReactions = new List<Reaction>
            {
                publicReaction,
                pastPublishedReaction
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllReactionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageReactions);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<Reaction> actualReactions =
                await this.reactionService.RetrieveAllReactionsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualReactions.Should().BeEquivalentTo(expectedReactions);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllReactionsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllPublicAndOwnReactionsWhenUserHasNoReviewRoleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Reaction publicReaction = CreateRandomReaction();
            publicReaction.IsDeleted = false;
            publicReaction.ApprovalStatus = ApprovalStatus.Approved;
            publicReaction.IsPublished = true;
            publicReaction.PublishDate = null;

            Reaction ownDraftReaction = CreateRandomReaction();
            ownDraftReaction.IsDeleted = false;
            ownDraftReaction.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftReaction.IsPublished = false;
            ownDraftReaction.CreatedBy = randomActorUserId;

            Reaction othersDraftReaction = CreateRandomReaction();
            othersDraftReaction.IsDeleted = false;
            othersDraftReaction.ApprovalStatus = ApprovalStatus.Draft;
            othersDraftReaction.IsPublished = false;

            Reaction ownDeletedReaction = CreateRandomReaction();
            ownDeletedReaction.IsDeleted = true;
            ownDeletedReaction.CreatedBy = randomActorUserId;

            IQueryable<Reaction> storageReactions = new List<Reaction>
            {
                publicReaction,
                ownDraftReaction,
                othersDraftReaction,
                ownDeletedReaction
            }.AsQueryable();

            IQueryable<Reaction> expectedReactions = new List<Reaction>
            {
                publicReaction,
                ownDraftReaction
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllReactionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageReactions);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Reaction> actualReactions =
                await this.reactionService.RetrieveAllReactionsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualReactions.Should().BeEquivalentTo(expectedReactions);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllReactionsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllNonDeletedReactionsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no clock, no
            // user-id resolution
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            Reaction publicReaction = CreateRandomReaction();
            publicReaction.IsDeleted = false;
            publicReaction.ApprovalStatus = ApprovalStatus.Approved;
            publicReaction.IsPublished = true;
            publicReaction.PublishDate = null;

            Reaction draftReaction = CreateRandomReaction();
            draftReaction.IsDeleted = false;
            draftReaction.ApprovalStatus = ApprovalStatus.Draft;
            draftReaction.IsPublished = false;

            Reaction futurePublishedReaction = CreateRandomReaction();
            futurePublishedReaction.IsDeleted = false;
            futurePublishedReaction.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedReaction.IsPublished = true;
            futurePublishedReaction.PublishDate = GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            Reaction deletedReaction = CreateRandomReaction();
            deletedReaction.IsDeleted = true;

            IQueryable<Reaction> storageReactions = new List<Reaction>
            {
                publicReaction,
                draftReaction,
                futurePublishedReaction,
                deletedReaction
            }.AsQueryable();

            IQueryable<Reaction> expectedReactions = new List<Reaction>
            {
                publicReaction,
                draftReaction,
                futurePublishedReaction
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllReactionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageReactions);

            // when
            IQueryable<Reaction> actualReactions =
                await this.reactionService.RetrieveAllReactionsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualReactions.Should().BeEquivalentTo(expectedReactions);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllReactionsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
