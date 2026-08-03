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
using Glory2Him.Core.Models.Foundations.Reactions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveReactionByIdAsync()
        {
            // given
            Reaction randomReaction = CreateRandomReaction();
            Reaction storageReaction = randomReaction;
            storageReaction.IsDeleted = false;
            storageReaction.ApprovalStatus = ApprovalStatus.Approved;
            storageReaction.IsPublished = true;
            storageReaction.PublishDate = null;
            Reaction expectedReaction = storageReaction;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            Reaction actualReaction =
                await this.reactionService.RetrieveReactionByIdAsync(
                    randomReaction.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(expectedReaction);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
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
        public async Task ShouldRetrieveNonPublicReactionByIdWhenUserIsOwnerAsync()
        {
            // given
            Reaction randomReaction = CreateRandomReaction();
            Reaction storageReaction = randomReaction;
            storageReaction.IsDeleted = false;
            storageReaction.ApprovalStatus = ApprovalStatus.Draft;
            storageReaction.IsPublished = false;
            Reaction expectedReaction = storageReaction;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageReaction.CreatedBy);

            // when
            Reaction actualReaction =
                await this.reactionService.RetrieveReactionByIdAsync(
                    randomReaction.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(expectedReaction);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
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
        public async Task ShouldRetrieveNonPublicReactionByIdWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the caller is not the owner but holds a review role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            string randomActorUserId = GetRandomString();
            Reaction randomReaction = CreateRandomReaction();
            Reaction storageReaction = randomReaction;
            storageReaction.IsDeleted = false;
            storageReaction.ApprovalStatus = ApprovalStatus.Draft;
            storageReaction.IsPublished = false;
            Reaction expectedReaction = storageReaction;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            Reaction actualReaction =
                await this.reactionService.RetrieveReactionByIdAsync(
                    randomReaction.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(expectedReaction);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    randomReaction.Id,
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
