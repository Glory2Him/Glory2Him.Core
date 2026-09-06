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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalComments;
using RESTFulSense.Exceptions;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalComments
{
    public partial class ApprovalCommentApiTests
    {
        [Fact]
        public async Task ShouldPutApprovalCommentAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            ApprovalComment modifiedApprovalComment =
                UpdateApprovalCommentWithRandomValues(randomApprovalComment);

            try
            {
                // when
                await this.apiBroker.PutApprovalCommentAsync(modifiedApprovalComment);

                ApprovalComment actualApprovalComment =
                    await this.apiBroker.GetApprovalCommentByIdAsync(randomApprovalComment.Id);

                // then
                actualApprovalComment.Should().BeEquivalentTo(modifiedApprovalComment, options => options
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen));
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// THE TYPE IS THE AUTHOR'S TO CORRECT, and it survives the round trip. It is deliberately
        /// absent from the pin list (§12.3.1): the row belongs to whoever wrote it, and turning a
        /// remark into an ask — or back — is them changing their own words.
        ///
        /// <para>Stated rather than drawn. The generic PUT test above compares whole rows, so it
        /// would pass unchanged if the filler happened to leave CommentType at its default on both
        /// sides, and a regression that dropped the field from the update would go unnoticed.</para>
        /// </summary>
        [Theory]
        [InlineData(ApprovalCommentType.Comment, ApprovalCommentType.Question)]
        [InlineData(ApprovalCommentType.Question, ApprovalCommentType.Comment)]
        public async Task ShouldPutApprovalCommentWithACorrectedTypeAsync(
            ApprovalCommentType bornAs,
            ApprovalCommentType correctedTo)
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalComment inputApprovalComment = CreateRandomApprovalComment(randomApproval.Id);
            inputApprovalComment.CommentType = bornAs;

            // An ask is born outstanding: the add gate refuses a settled one, so the fixture has
            // to state the pairing the server will accept rather than whatever the filler drew.
            inputApprovalComment.IsResolved = bornAs == ApprovalCommentType.Comment;

            ApprovalComment createdApprovalComment =
                await this.apiBroker.PostApprovalCommentAsync(inputApprovalComment);

            try
            {
                // when
                createdApprovalComment.CommentType = correctedTo;
                await this.apiBroker.PutApprovalCommentAsync(createdApprovalComment);

                ApprovalComment actualApprovalComment =
                    await this.apiBroker.GetApprovalCommentByIdAsync(createdApprovalComment.Id);

                // then
                actualApprovalComment.CommentType.Should().Be(correctedTo);

                // and the resolution is untouched — the two are one decision at BIRTH only, and
                // an amend is not a birth
                actualApprovalComment.IsResolved.Should().Be(createdApprovalComment.IsResolved);
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    createdApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// Resolve owns <c>IsResolved</c> and nothing else, so an ask that gets settled is still an
        /// ask afterwards — otherwise a settled question would become indistinguishable from a
        /// remark, which is the exact confusion <c>ApprovalCommentType</c> exists to prevent.
        /// </summary>
        [Fact]
        public async Task ShouldLeaveTheTypeAloneWhenAnAskIsSettledAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalComment inputApprovalComment = CreateRandomApprovalComment(randomApproval.Id);
            inputApprovalComment.CommentType = ApprovalCommentType.Question;
            inputApprovalComment.IsResolved = false;

            ApprovalComment createdApprovalComment =
                await this.apiBroker.PostApprovalCommentAsync(inputApprovalComment);

            try
            {
                // when
                ApprovalComment resolvedApprovalComment = await this.apiBroker
                    .ResolveApprovalCommentAsync(createdApprovalComment.Id, isResolved: true);

                // then
                resolvedApprovalComment.IsResolved.Should().BeTrue();
                resolvedApprovalComment.CommentType.Should().Be(ApprovalCommentType.Question);
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    createdApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The rule has to hold on modify as well as add, or the text can simply be emptied one
        /// write later — the comment lands with substance, is blanked by the next PUT, and goes on
        /// holding its approval shut while saying nothing.
        ///
        /// <para>The second assertion is the one that matters: the stored text must be UNCHANGED.
        /// A refusal that had already written the blank would be no protection at all.</para>
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldReturnBadRequestOnPutIfCommentIsBlankedAsync(string blankComment)
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            ApprovalComment blankedApprovalComment =
                UpdateApprovalCommentWithRandomValues(randomApprovalComment);

            blankedApprovalComment.Comment = blankComment;

            try
            {
                // when
                var putApprovalCommentTask =
                    this.apiBroker.PutApprovalCommentAsync(blankedApprovalComment).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => putApprovalCommentTask);

                ApprovalComment actualApprovalComment =
                    await this.apiBroker.GetApprovalCommentByIdAsync(randomApprovalComment.Id);

                actualApprovalComment.Comment.Should().Be(randomApprovalComment.Comment);
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }
    }
}
