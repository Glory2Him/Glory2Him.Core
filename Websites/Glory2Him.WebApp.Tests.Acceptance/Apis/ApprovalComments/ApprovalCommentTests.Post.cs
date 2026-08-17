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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalComments;
using RESTFulSense.Exceptions;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalComments
{
    public partial class ApprovalCommentApiTests
    {
        [Fact]
        public async Task ShouldPostApprovalCommentAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalComment randomApprovalComment = CreateRandomApprovalComment(randomApproval.Id);
            ApprovalComment inputApprovalComment = randomApprovalComment;
            ApprovalComment expectedApprovalComment = inputApprovalComment;

            try
            {
                // when
                await this.apiBroker.PostApprovalCommentAsync(inputApprovalComment);

                ApprovalComment actualApprovalComment =
                    await this.apiBroker.GetApprovalCommentByIdAsync(inputApprovalComment.Id);

                // then
                actualApprovalComment.Should().BeEquivalentTo(expectedApprovalComment, options => options
                    .Excluding(property => property.CreatedBy)
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen));
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    inputApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The parent round is not decoration. A comment aimed at an approval that does not
        /// exist is refused at the access gate rather than left to the foreign key, so the
        /// caller gets an authorization answer and no row is written (§7.7 rule 1).
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfParentApprovalDoesNotExistAsync()
        {
            // given
            ApprovalComment randomApprovalComment = CreateRandomApprovalComment(Guid.NewGuid());

            // when
            var postApprovalCommentTask =
                this.apiBroker.PostApprovalCommentAsync(randomApprovalComment).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postApprovalCommentTask);
        }

        /// <summary>
        /// The comment is the substance of the record, so an empty one is refused at the door.
        ///
        /// <para><c>400</c> is asserted specifically because the foundation rule is the ONLY thing
        /// that refuses these. The column is <c>nvarchar(1000) NOT NULL</c>, which sounds like a
        /// backstop but is not one here: <c>""</c> and <c>"   "</c> both satisfy NOT NULL, so
        /// without the rule they would simply be stored. Only the <c>null</c> case would reach a
        /// constraint at all — and even that surfaces as a dependency failure (<c>424</c>, not
        /// <c>500</c>: EFxceptions does not map SQL 515, so the <c>DbUpdateException</c> becomes
        /// an <c>ApprovalCommentDependencyException</c>), which is a storage answer to what is
        /// really bad input. A 400 is the assertion that the rule, not the column, is deciding.
        ///
        /// <para>Model binding does not help either — <c>Program.cs</c> sets
        /// <c>SuppressImplicitRequiredAttributeForNonNullableReferenceTypes</c>, so even the null
        /// case reaches the service rather than being refused as model state.</para></para>
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldReturnBadRequestOnPostIfCommentIsBlankAsync(string blankComment)
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalComment invalidApprovalComment = CreateRandomApprovalComment(randomApproval.Id);
            invalidApprovalComment.Comment = blankComment;

            try
            {
                // when
                var postApprovalCommentTask =
                    this.apiBroker.PostApprovalCommentAsync(invalidApprovalComment).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => postApprovalCommentTask);

                // and nothing was written — a refused post must leave no row behind
                var getApprovalCommentTask =
                    this.apiBroker.GetApprovalCommentByIdAsync(invalidApprovalComment.Id).AsTask();

                await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getApprovalCommentTask);
            }
            finally
            {
                // Unconditional, even though a refused post should leave nothing to remove. If the
                // rule ever stops firing, the post succeeds, the assertion above throws, and the
                // comment is left holding its parent round in place through the NoAction foreign
                // key — so a broken rule would silently strand rows in the dev database.
                await this.apiBroker.RemoveCoreApprovalCommentByIdAsync(invalidApprovalComment.Id);
                await this.apiBroker.RemoveApprovalByIdAsync(randomApproval.Id);
            }
        }

        /// <summary>
        /// The other end of the same cap. 1000 characters is accepted and 1001 refused, and again
        /// the refusal is a <c>400</c> from the rule rather than a <c>424</c> from the column —
        /// SQL Server does not truncate silently on insert, it raises an error, which arrives as a
        /// dependency failure. Storage refusing over-long text is not wrong, it is just the wrong
        /// layer answering, and it tells the caller nothing about which field was at fault.
        /// </summary>
        [Theory]
        [InlineData(1000, false)]
        [InlineData(1001, true)]
        public async Task ShouldEnforceTheCommentCapOnPostAsync(int commentLength, bool expectRefusal)
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalComment inputApprovalComment = CreateRandomApprovalComment(randomApproval.Id);
            inputApprovalComment.Comment = new string('x', commentLength);

            try
            {
                // when
                var postApprovalCommentTask =
                    this.apiBroker.PostApprovalCommentAsync(inputApprovalComment).AsTask();

                // then
                if (expectRefusal)
                {
                    await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => postApprovalCommentTask);
                }
                else
                {
                    await postApprovalCommentTask;

                    ApprovalComment actualApprovalComment =
                        await this.apiBroker.GetApprovalCommentByIdAsync(inputApprovalComment.Id);

                    // stored whole, not silently truncated by the column
                    actualApprovalComment.Comment.Should().Be(inputApprovalComment.Comment);
                }
            }
            finally
            {
                // Unconditional rather than flagged on whether the post succeeded: the removal is
                // already a no-op when the row is absent, and a flag set after the call would miss
                // a post that landed and then failed its assertion.
                await this.apiBroker.RemoveCoreApprovalCommentByIdAsync(inputApprovalComment.Id);
                await this.apiBroker.RemoveApprovalByIdAsync(randomApproval.Id);
            }
        }

    }
}
