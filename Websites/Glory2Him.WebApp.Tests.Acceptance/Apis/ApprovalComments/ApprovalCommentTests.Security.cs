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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalComments;
using RESTFulSense.Exceptions;
using CoreApprovalComment = Glory2Him.Core.Models.Foundations.ApprovalComments.ApprovalComment;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalComments
{
    /// <summary>
    /// What the gates turn away, over real HTTP. The unit suite proves the attributes say what
    /// the design says; these prove the middleware and the foundation act on them — a gate that
    /// is present but never evaluated passes the former and fails here.
    ///
    /// <para>Approval comments are §14.7 <b>posture D</b>: a review thread is never public, and a
    /// row the caller may not see is reported as not found rather than refused, so a read cannot
    /// be used to probe which comments exist.</para>
    /// </summary>
    public partial class ApprovalCommentApiTests
    {
        [Fact]
        public async Task ShouldReturnUnauthorizedOnGetAllIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var getAllTask = this.apiBroker.GetAllApprovalCommentsAsync().AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => getAllTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnGetByIdIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var getByIdTask = this.apiBroker.GetApprovalCommentByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => getByIdTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsAnonymousAsync()
        {
            // given
            ApprovalComment randomApprovalComment = CreateRandomApprovalComment(Guid.NewGuid());
            this.apiBroker.ActAsAnonymous();

            // when
            var postTask = this.apiBroker.PostApprovalCommentAsync(randomApprovalComment).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnResolveIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var resolveTask = this.apiBroker
                .ResolveApprovalCommentAsync(Guid.NewGuid(), isResolved: true).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => resolveTask);
        }

        /// <summary>
        /// The block tier (design §18.6) takes precedence over every other role. Approval
        /// workflow records carry no entity-scoped block, so only the global one applies — and
        /// it still refuses a caller who also holds Admin.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsBlockedAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalComment randomApprovalComment = CreateRandomApprovalComment(randomApproval.Id);
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.ReadOnly);

            try
            {
                // when
                var postTask = this.apiBroker.PostApprovalCommentAsync(randomApprovalComment).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfBlockedCallerAlsoHoldsAdminAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalComment randomApprovalComment = CreateRandomApprovalComment(randomApproval.Id);
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Admin, Roles.ReadOnly);

            try
            {
                // when
                var postTask = this.apiBroker.PostApprovalCommentAsync(randomApprovalComment).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// A signed-in contributor is not an Admin. Hard removal admits no owner branch, so the
        /// attribute must turn the caller away before the service is reached — the row is still
        /// there afterwards.
        /// </summary>
        [Fact]
        public async Task ShouldReturnForbiddenOnHardDeleteIfCallerIsNotAdministratorAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var hardDeleteTask = this.apiBroker
                    .HardDeleteApprovalCommentByIdAsync(randomApprovalComment.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => hardDeleteTask);

                this.apiBroker.ActAsSeededAdministrator();

                CoreApprovalComment survivingApprovalComment =
                    await this.apiBroker.GetCoreApprovalCommentByIdAsync(randomApprovalComment.Id);

                survivingApprovalComment.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// Hard delete is the one write here whose coarse gate is a role list, so a blocked Admin
        /// clears the attribute and is stopped by the foundation instead — which is the whole
        /// point of §18.6 precedence: the block tier beats every other role, on the one operation
        /// that destroys the row and its audit trail. Nothing but a request through the real
        /// pipeline can prove that composition, because the attribute cannot express it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfBlockedCallerAlsoHoldsAdminAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Admin, Roles.ReadOnly);

            try
            {
                // when
                var hardDeleteTask = this.apiBroker
                    .HardDeleteApprovalCommentByIdAsync(randomApprovalComment.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);

                this.apiBroker.ActAsSeededAdministrator();

                CoreApprovalComment survivingApprovalComment =
                    await this.apiBroker.GetCoreApprovalCommentByIdAsync(randomApprovalComment.Id);

                survivingApprovalComment.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The block tier on the remaining writes. Each of these reaches
        /// <c>ValidateUserIsAllowedToComment</c>, which refuses a globally blocked caller before
        /// anything about the stored row is looked at — so the refusal is the block, not ownership.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnWritesIfCallerIsBlockedAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            string blockedUserId = this.apiBroker.ActAsContributor();

            ApprovalComment ownApprovalComment = await this.apiBroker.PostApprovalCommentAsync(
                CreateRandomApprovalComment(randomApproval.Id));

            ApprovalComment modifiedApprovalComment =
                UpdateApprovalCommentWithRandomValues(ownApprovalComment);

            // the SAME user, now blocked — so what changes the answer is the block and nothing else
            this.apiBroker.ActAs(blockedUserId, Roles.ReadOnly);

            try
            {
                // when / then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() =>
                    this.apiBroker.PutApprovalCommentAsync(modifiedApprovalComment).AsTask());

                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() =>
                    this.apiBroker.ResolveApprovalCommentAsync(
                        ownApprovalComment.Id,
                        isResolved: true).AsTask());

                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() =>
                    this.apiBroker.DeleteApprovalCommentByIdAsync(ownApprovalComment.Id).AsTask());
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    ownApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// Past the attribute, the foundation decides ownership against the STORED row. Modify is
        /// the author and nobody else — not a Reviewer, who may read the thread without owning
        /// the power to rewrite someone else's words.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Admin)]
        public async Task ShouldReturnUnauthorizedOnPutIfCallerIsNotTheAuthorAsync(string roleName)
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            ApprovalComment modifiedApprovalComment =
                UpdateApprovalCommentWithRandomValues(randomApprovalComment);

            ActAsOtherUser(roleName);

            try
            {
                // when
                var putTask = this.apiBroker.PutApprovalCommentAsync(modifiedApprovalComment).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => putTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The same contributor, on a comment they DID write, succeeds — so the refusal above is
        /// ownership and not merely the absence of a role.
        /// </summary>
        [Fact]
        public async Task ShouldAllowContributorToModifyTheirOwnApprovalCommentAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            string contributorUserId = this.apiBroker.ActAsContributor();

            ApprovalComment createdApprovalComment = await this.apiBroker.PostApprovalCommentAsync(
                CreateRandomApprovalComment(randomApproval.Id));

            ApprovalComment modifiedApprovalComment =
                UpdateApprovalCommentWithRandomValues(createdApprovalComment);

            try
            {
                // when
                ApprovalComment actualApprovalComment =
                    await this.apiBroker.PutApprovalCommentAsync(modifiedApprovalComment);

                // then
                actualApprovalComment.Comment.Should().Be(modifiedApprovalComment.Comment);
                actualApprovalComment.CreatedBy.Should().Be(contributorUserId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    createdApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// Withdrawal matches modify and is deliberately narrower than the read posture: an
        /// Admin who needs past an unresolved comment resolves it — retracting someone else's
        /// words is not theirs to do.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Admin)]
        public async Task ShouldReturnUnauthorizedOnDeleteIfCallerIsNotTheAuthorAsync(string roleName)
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            ActAsOtherUser(roleName);

            try
            {
                // when
                var deleteTask = this.apiBroker
                    .DeleteApprovalCommentByIdAsync(randomApprovalComment.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => deleteTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// Resolve is owner-OR-Admin, and that widening is the whole reason the operation exists
        /// (§14.7 rule 5). A Reviewer clears neither branch.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Publisher)]
        public async Task ShouldReturnUnauthorizedOnResolveIfCallerIsNeitherAuthorNorAdminAsync(
            string roleName)
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            ActAsOtherUser(roleName);

            try
            {
                // when
                var resolveTask = this.apiBroker
                    .ResolveApprovalCommentAsync(randomApprovalComment.Id, isResolved: true).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => resolveTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The Admin branch of resolve, on a comment written by somebody else — the case modify
        /// deliberately cannot express.
        /// </summary>
        [Fact]
        public async Task ShouldAllowAdministratorToResolveAnotherUsersApprovalCommentAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            string contributorUserId = this.apiBroker.ActAsContributor();

            ApprovalComment createdApprovalComment = await this.apiBroker.PostApprovalCommentAsync(
                CreateRandomApprovalComment(randomApproval.Id));

            try
            {
                // when
                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Admin);

                ApprovalComment resolvedApprovalComment = await this.apiBroker
                    .ResolveApprovalCommentAsync(createdApprovalComment.Id, isResolved: true);

                // then
                resolvedApprovalComment.IsResolved.Should().BeTrue();

                // the Admin settled the flag without touching the author's words or the audit
                resolvedApprovalComment.CreatedBy.Should().Be(contributorUserId);
                resolvedApprovalComment.Comment.Should().Be(createdApprovalComment.Comment);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    createdApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// Posture D on the single-row read: a caller who is neither the author nor in a review
        /// role is told not found, never forbidden — the distinction is what stops the endpoint
        /// being a probe for which comments exist.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfCallerIsNeitherAuthorNorReviewerAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var getByIdTask = this.apiBroker
                    .GetApprovalCommentByIdAsync(randomApprovalComment.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getByIdTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The other side of the same posture: a review role reads a thread it did not write.
        /// Both tiers are exercised — the global roles and an entity-scoped one — because the
        /// foundation admits any "-Reviewer"/"-Publisher" suffix by the §16.6 convention, and
        /// testing only the global names would leave that half of the rule dead.
        /// </summary>
        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Publisher)]
        [InlineData(Roles.TagReviewer)]
        public async Task ShouldAllowReviewRoleToReadAnotherUsersApprovalCommentAsync(string roleName)
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);

            try
            {
                // when
                ApprovalComment actualApprovalComment = await this.apiBroker
                    .GetApprovalCommentByIdAsync(randomApprovalComment.Id);

                // then
                actualApprovalComment.Id.Should().Be(randomApprovalComment.Id);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The collection twin of the posture: a row the caller may not see drops out of the set
        /// instead of erroring, so a collection read never reveals how many comments a thread
        /// holds. The caller's OWN comment is still there, which is what separates "filtered" from
        /// "empty because the read failed".
        /// </summary>
        [Fact]
        public async Task ShouldFilterOtherUsersApprovalCommentsOutOfTheCollectionReadAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment otherUsersApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            this.apiBroker.ActAsContributor();

            ApprovalComment ownApprovalComment = await this.apiBroker.PostApprovalCommentAsync(
                CreateRandomApprovalComment(randomApproval.Id));

            try
            {
                // when
                List<ApprovalComment> actualApprovalComments =
                    await this.apiBroker.GetAllApprovalCommentsAsync();

                // then
                actualApprovalComments.Should().Contain(approvalComment =>
                    approvalComment.Id == ownApprovalComment.Id);

                actualApprovalComments.Should().NotContain(approvalComment =>
                    approvalComment.Id == otherUsersApprovalComment.Id);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreApprovalCommentByIdAsync(ownApprovalComment.Id);

                await RemoveApprovalCommentAndApprovalAsync(
                    otherUsersApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// A caller who is not the seeded administrator, optionally holding one role. Passing
        /// <c>null</c> gives the ordinary contributor — the "holds nothing at all" case.
        /// </summary>
        private void ActAsOtherUser(string roleName)
        {
            if (roleName is null)
            {
                this.apiBroker.ActAsContributor();

                return;
            }

            this.apiBroker.ActAs(Guid.NewGuid().ToString(), roleName);
        }
    }
}
