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
using G2H.Security.Client.Models.Foundations.Access;
using Moq;

namespace G2H.Security.Client.Tests.Unit.Clients.Access
{
    public partial class AccessClientTests
    {
        [Fact]
        public async Task ShouldEvaluateApprovalConditionsAsync()
        {
            // given
            ApprovalConditionsRequest randomApprovalConditionsRequest =
                CreateRandomApprovalConditionsRequest();

            ApprovalConditionsVerdict randomApprovalConditionsVerdict =
                CreateRandomApprovalConditionsVerdict();

            ApprovalConditionsVerdict expectedApprovalConditionsVerdict =
                randomApprovalConditionsVerdict;

            this.accessServiceMock.Setup(service =>
                service.EvaluateApprovalConditionsAsync(randomApprovalConditionsRequest))
                    .ReturnsAsync(randomApprovalConditionsVerdict);

            // when
            ApprovalConditionsVerdict actualApprovalConditionsVerdict =
                await this.accessClient.EvaluateApprovalConditionsAsync(
                    randomApprovalConditionsRequest);

            // then
            actualApprovalConditionsVerdict.Should()
                .BeEquivalentTo(expectedApprovalConditionsVerdict);

            this.accessServiceMock.Verify(service =>
                service.EvaluateApprovalConditionsAsync(randomApprovalConditionsRequest),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDecideIfActorMayRecordApprovalReviewAsync()
        {
            // given
            RecordReviewRequest randomRecordReviewRequest =
                CreateRandomRecordReviewRequest();

            AccessVerdict randomAccessVerdict = CreateRandomAccessVerdict();
            AccessVerdict expectedAccessVerdict = randomAccessVerdict;

            this.accessServiceMock.Setup(service =>
                service.MayRecordApprovalReviewAsync(randomRecordReviewRequest))
                    .ReturnsAsync(randomAccessVerdict);

            // when
            AccessVerdict actualAccessVerdict =
                await this.accessClient.MayRecordApprovalReviewAsync(
                    randomRecordReviewRequest);

            // then
            actualAccessVerdict.Should().BeEquivalentTo(expectedAccessVerdict);

            this.accessServiceMock.Verify(service =>
                service.MayRecordApprovalReviewAsync(randomRecordReviewRequest),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDecideIfActorMayDecideApprovalAsync()
        {
            // given
            DecideApprovalRequest randomDecideApprovalRequest =
                CreateRandomDecideApprovalRequest();

            AccessVerdict randomAccessVerdict = CreateRandomAccessVerdict();
            AccessVerdict expectedAccessVerdict = randomAccessVerdict;

            this.accessServiceMock.Setup(service =>
                service.MayDecideApprovalAsync(randomDecideApprovalRequest))
                    .ReturnsAsync(randomAccessVerdict);

            // when
            AccessVerdict actualAccessVerdict =
                await this.accessClient.MayDecideApprovalAsync(randomDecideApprovalRequest);

            // then
            actualAccessVerdict.Should().BeEquivalentTo(expectedAccessVerdict);

            this.accessServiceMock.Verify(service =>
                service.MayDecideApprovalAsync(randomDecideApprovalRequest),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task ShouldDecideIfActorMayRecordApprovalCommentAsync()
        {
            // given
            RecordApprovalCommentRequest randomRecordApprovalCommentRequest =
                CreateRandomRecordApprovalCommentRequest();

            AccessVerdict randomAccessVerdict = CreateRandomAccessVerdict();
            AccessVerdict expectedAccessVerdict = randomAccessVerdict;

            this.accessServiceMock.Setup(service =>
                service.MayRecordApprovalCommentAsync(randomRecordApprovalCommentRequest))
                    .ReturnsAsync(randomAccessVerdict);

            // when
            AccessVerdict actualAccessVerdict =
                await this.accessClient.MayRecordApprovalCommentAsync(randomRecordApprovalCommentRequest);

            // then
            actualAccessVerdict.Should().BeEquivalentTo(expectedAccessVerdict);

            this.accessServiceMock.Verify(service =>
                service.MayRecordApprovalCommentAsync(randomRecordApprovalCommentRequest),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDecideIfActorMayAmendApprovalCommentAsync()
        {
            // given
            AmendApprovalCommentRequest randomAmendApprovalCommentRequest =
                CreateRandomAmendApprovalCommentRequest();

            AccessVerdict randomAccessVerdict = CreateRandomAccessVerdict();
            AccessVerdict expectedAccessVerdict = randomAccessVerdict;

            this.accessServiceMock.Setup(service =>
                service.MayAmendApprovalCommentAsync(randomAmendApprovalCommentRequest))
                    .ReturnsAsync(randomAccessVerdict);

            // when
            AccessVerdict actualAccessVerdict =
                await this.accessClient.MayAmendApprovalCommentAsync(randomAmendApprovalCommentRequest);

            // then
            actualAccessVerdict.Should().BeEquivalentTo(expectedAccessVerdict);

            this.accessServiceMock.Verify(service =>
                service.MayAmendApprovalCommentAsync(randomAmendApprovalCommentRequest),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDecideIfActorMayResolveApprovalCommentAsync()
        {
            // given
            ResolveApprovalCommentRequest randomResolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest();

            AccessVerdict randomAccessVerdict = CreateRandomAccessVerdict();
            AccessVerdict expectedAccessVerdict = randomAccessVerdict;

            this.accessServiceMock.Setup(service =>
                service.MayResolveApprovalCommentAsync(randomResolveApprovalCommentRequest))
                    .ReturnsAsync(randomAccessVerdict);

            // when
            AccessVerdict actualAccessVerdict =
                await this.accessClient.MayResolveApprovalCommentAsync(randomResolveApprovalCommentRequest);

            // then
            actualAccessVerdict.Should().BeEquivalentTo(expectedAccessVerdict);

            this.accessServiceMock.Verify(service =>
                service.MayResolveApprovalCommentAsync(randomResolveApprovalCommentRequest),
                    Times.Once);

            this.accessServiceMock.VerifyNoOtherCalls();
        }
    }
}
