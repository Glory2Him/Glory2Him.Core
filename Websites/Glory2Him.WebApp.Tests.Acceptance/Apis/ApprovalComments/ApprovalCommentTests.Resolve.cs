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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalComments;
using RESTFulSense.Exceptions;
using CoreApprovalComment = Glory2Him.Core.Models.Foundations.ApprovalComments.ApprovalComment;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalComments
{
    public partial class ApprovalCommentApiTests
    {
        [Fact]
        public async Task ShouldResolveApprovalCommentAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            try
            {
                // when
                ApprovalComment resolvedApprovalComment =
                    await this.apiBroker.ResolveApprovalCommentAsync(
                        randomApprovalComment.Id,
                        isResolved: true);

                ApprovalComment actualApprovalComment =
                    await this.apiBroker.GetApprovalCommentByIdAsync(randomApprovalComment.Id);

                // then
                resolvedApprovalComment.IsResolved.Should().BeTrue();
                actualApprovalComment.IsResolved.Should().BeTrue();

                // the operation owns IsResolved and nothing else — the wording is untouched
                actualApprovalComment.Comment.Should().Be(randomApprovalComment.Comment);
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// Unsettling rides the same route, and is not merely error-correction: a comment
        /// recorded as an observation may later turn out to need action.
        /// </summary>
        [Fact]
        public async Task ShouldUnresolveApprovalCommentAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            try
            {
                await this.apiBroker.ResolveApprovalCommentAsync(
                    randomApprovalComment.Id,
                    isResolved: true);

                // when
                ApprovalComment unresolvedApprovalComment =
                    await this.apiBroker.ResolveApprovalCommentAsync(
                        randomApprovalComment.Id,
                        isResolved: false);

                // then
                unresolvedApprovalComment.IsResolved.Should().BeFalse();
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The flag is bind-required, so a caller who hits the obvious URL and says nothing is
        /// refused. Without that, the absent <c>bool</c> would bind to <c>false</c> and this would
        /// be a 200 that silently UN-resolved the comment — re-blocking an approval that had been
        /// cleared, on a route segment reading <c>Resolve</c>. The service's own no-op guard
        /// cannot catch it, because for a resolved comment <c>false</c> is a real change; so the
        /// assertion checks the stored state as well as the status.
        /// </summary>
        [Fact]
        public async Task ShouldReturnBadRequestOnResolveIfTheFlagIsOmittedAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            try
            {
                await this.apiBroker.ResolveApprovalCommentAsync(
                    randomApprovalComment.Id,
                    isResolved: true);

                // when
                var resolveTask = this.apiBroker
                    .ResolveApprovalCommentWithNoFlagAsync(randomApprovalComment.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => resolveTask);

                CoreApprovalComment storedApprovalComment =
                    await this.apiBroker.GetCoreApprovalCommentByIdAsync(randomApprovalComment.Id);

                storedApprovalComment.IsResolved.Should().BeTrue();
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// A resolution that changes nothing is refused rather than treated as idempotent: a
        /// spurious Resolved fact would announce to anything watching
        /// <c>RequireReviewCommentResolutionBeforeApprovals</c> that a gate moved when it did not.
        /// </summary>
        [Fact]
        public async Task ShouldReturnBadRequestOnResolveIfNothingChangesAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            try
            {
                // when
                var resolveTask = this.apiBroker.ResolveApprovalCommentAsync(
                    randomApprovalComment.Id,
                    isResolved: false).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => resolveTask);
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
