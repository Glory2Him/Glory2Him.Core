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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        // The predicate that decides which invitations a closing round retires (§7.9 rule 8). It
        // lives here rather than in the orchestration for the reason its neighbour above does,
        // and more sharply: only ONE of the three routes to an outcome runs under a moderator's
        // identity. An automatic approval runs under whoever's edit or review tipped the round,
        // ordinarily the author, and the caller-facing request read applies §14.7 posture D — so
        // deciding what to retire from that view retires nothing, throws nothing, and leaves the
        // panel showing an ask nobody can answer.
        //
        // This is the only place the predicate is exercised. The orchestration's tests mock
        // IAccessBroker, so they can only assert that a closing flow ASKS — what the answer
        // should be is settled here, against a real broker over a seeded storage broker.
        [Fact]
        public async Task ShouldFindOnlyTheOutstandingRequestsBelongingToTheApprovalAsync()
        {
            // given: three rows that each fail the filter differently, so a predicate that drops
            // either clause is distinguishable from one that does not
            Guid approvalId = Guid.NewGuid();
            Guid otherApprovalId = Guid.NewGuid();

            var outstandingRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = "invited-one",
            };

            // Same round, already gone — withdrawn by a moderator (§7.9 rule 5) or retired when
            // its target answered (rule 6). "Pending" is exactly IsDeleted = false and there is
            // no second definition of it; handing this one back would spend a retirement on a row
            // that is already deleted.
            var alreadyGoneRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = "invited-two",
                IsDeleted = true,
            };

            // ANOTHER round entirely. This is the clause whose loss is worst: without it, one
            // round closing would retire every open round's invitations in the table.
            var otherApprovalsRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = otherApprovalId,
                RequestedUserId = "invited-three",
            };

            SetupApprovalReviewRequests(
                outstandingRequest,
                alreadyGoneRequest,
                otherApprovalsRequest);

            // when
            List<Guid> actualRetirableRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualRetirableRequestIds.Should().Equal(new[] { outstandingRequest.Id },
                because: "only an invitation that is on THIS round and still outstanding is " +
                    "something the close has taken the answer away from");

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewRequestsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// The person who cast the deciding vote must not have their own invitation retired
        /// under the close reason (§12.5.4 business rule 4(ii)). Excluding whoever holds a review
        /// that still STANDS is what lets the two retirements (rules 6 and 8) stop depending on
        /// which order they run in.
        /// </summary>
        [Fact]
        public async Task ShouldExcludeAnInviteeWithAStandingReviewFromTheRetirableSetAsync()
        {
            // given: two pending invitations, one belonging to somebody who has already answered
            // with a review that still stands
            Guid approvalId = Guid.NewGuid();

            var answeredInviteeRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = "answered-invitee",
            };

            var unansweredInviteeRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = "unanswered-invitee",
            };

            SetupApprovalReviewRequests(answeredInviteeRequest, unansweredInviteeRequest);

            SetupApprovalReviews(
                CreateApprovalReview(
                    approvalId: approvalId,
                    createdBy: "answered-invitee",
                    statusId: ApprovalStatus.Approved));

            // when
            List<Guid> actualRetirableRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualRetirableRequestIds.Should().Equal(
                new[] { unansweredInviteeRequest.Id },
                because: "the person who cast the deciding vote must not have their own "
                    + "invitation retired under the close reason");
        }

        /// <summary>
        /// A review that no longer STANDS does not protect its author's invitation — four cases
        /// in one test, because all four fail the same clause of the same predicate.
        ///
        /// <para>Case (c) — stored at <c>Draft</c> or <c>Submitted</c> — is a decision, not a
        /// coincidence. A review at either status is not an answer: nobody has voted, so its
        /// author has not answered the round, and retiring their invitation under the close
        /// reason is ACCURATE rather than merely consistent with
        /// <c>ActiveReviewerUserIds</c>.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRetireAnInviteeWhoseOnlyReviewNoLongerStandsAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            // (a) Dismissed by a later edit (§9.5) — the re-invitation path that makes
            // RecordedReviewerUserIds the wrong set to exclude by.
            var dismissedReviewerRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = "dismissed-reviewer",
            };

            // (b) Withdrawn — soft-deleted.
            var withdrawnReviewerRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = "withdrawn-reviewer",
            };

            // (c-i) Stored at Draft — corrupt per ToReviewVerdict's own comment, and not an
            // answer: nobody has voted, so retiring this invitation is accurate rather than
            // incidental.
            var corruptStatusReviewerRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = "corrupt-status-reviewer",
            };

            // (c-ii) Stored at Submitted — awaiting a decision, not itself one. Same reasoning as
            // Draft: nobody has voted yet, so retiring this invitation is accurate.
            var submittedStatusReviewerRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = "submitted-status-reviewer",
            };

            SetupApprovalReviewRequests(
                dismissedReviewerRequest,
                withdrawnReviewerRequest,
                corruptStatusReviewerRequest,
                submittedStatusReviewerRequest);

            SetupApprovalReviews(
                CreateApprovalReview(
                    approvalId: approvalId,
                    createdBy: "dismissed-reviewer",
                    statusId: ApprovalStatus.Dismissed),

                CreateApprovalReview(
                    approvalId: approvalId,
                    createdBy: "withdrawn-reviewer",
                    statusId: ApprovalStatus.Approved,
                    isDeleted: true),

                CreateApprovalReview(
                    approvalId: approvalId,
                    createdBy: "corrupt-status-reviewer",
                    statusId: ApprovalStatus.Draft),

                CreateApprovalReview(
                    approvalId: approvalId,
                    createdBy: "submitted-status-reviewer",
                    statusId: ApprovalStatus.Submitted));

            // when
            List<Guid> actualRetirableRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualRetirableRequestIds.Should().BeEquivalentTo(
                new[]
                {
                    dismissedReviewerRequest.Id,
                    withdrawnReviewerRequest.Id,
                    corruptStatusReviewerRequest.Id,
                    submittedStatusReviewerRequest.Id,
                },
                because: "a review that no longer stands - dismissed, withdrawn, or stored at a "
                    + "status nobody could have voted from, whether Draft or Submitted - does "
                    + "not protect its author's invitation");
        }

        /// <summary>
        /// A standing review with a BLANK author carries no identity to exclude anybody by
        /// (finding 3) — the exclusion carries the same blank filter
        /// <c>ActiveReviewerUserIds</c> does, so the two cannot drift.
        /// </summary>
        [Fact]
        public async Task ShouldNotExcludeAnyoneWhenTheStandingReviewersAuthorIsBlankAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            var blankRequestedUserRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = string.Empty,
            };

            var namedInviteeRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = "named-invitee",
            };

            SetupApprovalReviewRequests(blankRequestedUserRequest, namedInviteeRequest);

            SetupApprovalReviews(
                CreateApprovalReview(
                    approvalId: approvalId,
                    createdBy: string.Empty,
                    statusId: ApprovalStatus.Approved));

            // when
            List<Guid> actualRetirableRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualRetirableRequestIds.Should().BeEquivalentTo(
                new[] { blankRequestedUserRequest.Id, namedInviteeRequest.Id },
                because: "a standing review with a blank author carries no identity to exclude "
                    + "anybody by, including a request whose own RequestedUserId is itself blank");
        }

        /// <summary>
        /// The round-scoping conjunct on the exclusion's own subquery
        /// (<c>approvalReview.ApprovalId == approvalId</c>), pinned separately from
        /// <see cref="ShouldExcludeAnInviteeWithAStandingReviewFromTheRetirableSetAsync"/> because
        /// that test's single approval round cannot distinguish "the review was matched by
        /// author" from "the review was matched by author AND round" — both readings return the
        /// same answer when there is only one round in play. A reviewer's STANDING review
        /// belongs to a round other than the one being closed, and must not reach across rounds to
        /// protect an invitation on this one; only an answer to THIS round counts as having
        /// answered it.
        /// </summary>
        [Fact]
        public async Task ShouldNotExcludeAnInviteeWhoseStandingReviewIsOnADifferentApprovalAsync()
        {
            // given: the same person is invited on the round under test, and separately holds a
            // standing Approved review recorded against a DIFFERENT round entirely
            Guid approvalId = Guid.NewGuid();
            Guid otherApprovalId = Guid.NewGuid();

            var crossRoundReviewerRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = "cross-round-reviewer",
            };

            SetupApprovalReviewRequests(crossRoundReviewerRequest);

            SetupApprovalReviews(
                CreateApprovalReview(
                    approvalId: otherApprovalId,
                    createdBy: "cross-round-reviewer",
                    statusId: ApprovalStatus.Approved));

            // when
            List<Guid> actualRetirableRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: approvalId,
                    cancellationToken: default);

            // then
            actualRetirableRequestIds.Should().Equal(
                new[] { crossRoundReviewerRequest.Id },
                because: "a standing review recorded against a DIFFERENT round never answers " +
                    "THIS round, so it must not protect this round's invitation from retirement");
        }

        [Fact]
        public async Task ShouldFindNoRetirableRequestsWhenNobodyWasAskedAsync()
        {
            // given: a round nobody was invited to, which is the ordinary case — most reviews are
            // recorded by people who were never formally asked. The caller must be able to tell
            // "nothing outstanding" from a failure, because the close proceeds either way.
            SetupApprovalReviewRequests();

            // when
            List<Guid> actualRetirableRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: Guid.NewGuid(),
                    cancellationToken: default);

            // then
            actualRetirableRequestIds.Should().BeEmpty(
                because: "a round nobody was invited to has nothing to retire, and that is an " +
                    "empty answer rather than an error");
        }
    }
}
