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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Models.Comments;
using RESTFulSense.Exceptions;
using CoreComment = Glory2Him.Core.Models.Foundations.Comments.Comment;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Comments
{
    /// <summary>
    /// What the gates turn away, over real HTTP. The unit suite proves the attributes say what
    /// the design says; these prove the middleware and the foundation act on them — a gate that
    /// is present but never evaluated passes the former and fails here.
    /// </summary>
    public partial class CommentApiTests
    {
        [Fact]
        public async Task ShouldAllowAnonymousToReadCommentsAsync()
        {
            // given
            Comment randomComment = await PostRandomCommentAsync();

            try
            {
                // when
                this.apiBroker.ActAsAnonymous();
                List<Comment> actualComments = await this.apiBroker.GetAllCommentsAsync();

                // then
                actualComments.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreCommentByIdAsync(randomComment.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsAnonymousAsync()
        {
            // given
            Comment randomComment = CreateRandomComment();
            this.apiBroker.ActAsAnonymous();

            // when
            var postCommentTask = this.apiBroker.PostCommentAsync(randomComment).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postCommentTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnSubmitIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var submitCommentTask = this.apiBroker.SubmitCommentByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => submitCommentTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var hardDeleteTask = this.apiBroker.HardDeleteCommentByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);
        }

        /// <summary>
        /// A signed-in contributor is not an Admin. Design §14.7 posture A rule 3 restricts hard
        /// removal to Admin, and the attribute must turn the caller away before the service is
        /// reached — so the row is still there afterwards.
        /// </summary>
        [Fact]
        public async Task ShouldReturnForbiddenOnHardDeleteIfCallerIsNotAdministratorAsync()
        {
            // given
            Comment randomComment = await PostRandomCommentAsync();
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var hardDeleteTask =
                    this.apiBroker.HardDeleteCommentByIdAsync(randomComment.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => hardDeleteTask);

                this.apiBroker.ActAsSeededAdministrator();
                CoreComment survivingComment = await this.apiBroker.GetCoreCommentByIdAsync(randomComment.Id);
                survivingComment.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreCommentByIdAsync(randomComment.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnForbiddenOnApproveIfCallerIsNotInThePublisherTierAsync()
        {
            // given
            Comment randomComment = CreateRandomComment();
            this.apiBroker.ActAsContributor();

            // when
            var approveCommentTask = this.apiBroker.TransitionCommentApprovalAsync(randomComment).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => approveCommentTask);
        }

        /// <summary>
        /// A reviewer is not a publisher (HR-3). Comment-Reviewer clears no part of the approve
        /// gate, and the attribute must not admit it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnForbiddenOnApproveIfCallerIsOnlyAReviewerAsync()
        {
            // given
            Comment randomComment = CreateRandomComment();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.CommentReviewer);

            // when
            var approveCommentTask = this.apiBroker.TransitionCommentApprovalAsync(randomComment).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => approveCommentTask);
        }

        /// <summary>
        /// Past the attribute, the foundation decides ownership against the STORED row. A
        /// contributor who did not write the comment is refused even though the coarse gate let them
        /// through — which is the whole point of enforcing at every layer (design §14.6).
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPutIfCallerIsNotTheOwnerAsync()
        {
            // given
            Comment randomComment = await PostRandomCommentAsync();
            Comment modifiedComment = UpdateCommentWithRandomValues(randomComment);
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var putCommentTask = this.apiBroker.PutCommentAsync(modifiedComment).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => putCommentTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreCommentByIdAsync(randomComment.Id);
            }
        }

        /// <summary>
        /// The same contributor, on a comment they DID write, succeeds — so the refusal above is
        /// ownership and not merely the absence of a role.
        /// </summary>
        [Fact]
        public async Task ShouldAllowContributorToModifyTheirOwnCommentAsync()
        {
            // given
            string contributorUserId = this.apiBroker.ActAsContributor();
            Comment randomComment = await PostRandomCommentAsync();
            Comment modifiedComment = UpdateCommentWithRandomValues(randomComment);

            try
            {
                // when
                Comment actualComment = await this.apiBroker.PutCommentAsync(modifiedComment);

                // then
                actualComment.Content.Should().Be(modifiedComment.Content);
                actualComment.CreatedBy.Should().Be(contributorUserId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreCommentByIdAsync(randomComment.Id);
            }
        }

        /// <summary>
        /// The review tier is owner-OR-role, so a reviewer may write a comment they did not create.
        /// Both tiers are exercised — the global <c>Reviewer</c> and the entity-scoped
        /// <c>Comment-Reviewer</c> — because the foundation tests for both and seeding only one
        /// would leave half the rule dead.
        /// </summary>
        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.CommentReviewer)]
        public async Task ShouldAllowReviewerToModifyAnotherUsersCommentAsync(string reviewRoleName)
        {
            // given
            Comment randomComment = await PostRandomCommentAsync();
            Comment modifiedComment = UpdateCommentWithRandomValues(randomComment);
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), reviewRoleName);

            try
            {
                // when
                Comment actualComment = await this.apiBroker.PutCommentAsync(modifiedComment);

                // then
                actualComment.Content.Should().Be(modifiedComment.Content);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreCommentByIdAsync(randomComment.Id);
            }
        }

        /// <summary>
        /// Removal is owner-or-Admin, deliberately narrower than modify: a Reviewer holds write
        /// permission on someone else's comment but may not delete it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteIfCallerIsNeitherOwnerNorAdministratorAsync()
        {
            // given
            Comment randomComment = await PostRandomCommentAsync();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.CommentReviewer);

            try
            {
                // when
                var deleteCommentTask = this.apiBroker.DeleteCommentByIdAsync(randomComment.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => deleteCommentTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreCommentByIdAsync(randomComment.Id);
            }
        }

        /// <summary>
        /// The block tier (design §18.6): "assigned to users who misbehave, takes precedence over
        /// every other role". Both the global and the comment-scoped block are refused, and the
        /// refusal survives the caller also holding Admin — precedence is the whole point.
        /// </summary>
        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.CommentReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsBlockedAsync(string blockRoleName)
        {
            // given
            Comment randomComment = CreateRandomComment();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), blockRoleName);

            // when
            var postCommentTask = this.apiBroker.PostCommentAsync(randomComment).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postCommentTask);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.CommentReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfBlockedCallerAlsoHoldsAdminAsync(
            string blockRoleName)
        {
            // given
            Comment randomComment = CreateRandomComment();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Admin, blockRoleName);

            // when
            var postCommentTask = this.apiBroker.PostCommentAsync(randomComment).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postCommentTask);
        }

        /// <summary>
        /// Hard delete is the one write whose coarse gate is a role list, so a blocked Admin
        /// clears the attribute and is stopped by the foundation instead.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfBlockedCallerAlsoHoldsAdminAsync()
        {
            // given
            Comment randomComment = await PostRandomCommentAsync();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Admin, Roles.CommentReadOnly);

            try
            {
                // when
                var hardDeleteTask =
                    this.apiBroker.HardDeleteCommentByIdAsync(randomComment.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreCommentByIdAsync(randomComment.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfCommentDoesNotExistAsync()
        {
            // given
            Guid randomId = Guid.NewGuid();

            // when
            var getCommentTask = this.apiBroker.GetCommentByIdAsync(randomId).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getCommentTask);
        }

        // NO CONFLICT TESTS HERE, and the absence is a rule rather than an oversight.
        //
        // The Tag, Reaction and BibleReference suites each assert a 409 on a duplicate natural
        // key. Comment has no natural key: StorageBroker.Comment.Configurations declares no
        // index at all, and Content is required but uncapped, so there is nothing a second row
        // can collide with and no way to provoke the response over HTTP.
        //
        // The controller still catches AlreadyExistsCommentException and maps it to 409 — the
        // type exists, the service is entitled to throw it, and the unit suite covers that arm
        // by mocking it. What cannot exist is an ACCEPTANCE test, because acceptance tests reach
        // the endpoint through the real stack and the real stack has no rule to break.
    }
}
