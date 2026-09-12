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
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.ApprovalReviewRequests
{
    /// <summary>
    /// Proves <see cref="IAccessBroker.FindRetirableApprovalReviewRequestIdsAsync"/> — §7.9 rule
    /// 8's gather — against a real catalogue.
    ///
    /// <para>The unit suite settles the predicate over an in-memory queryable, which is where a
    /// dropped conjunct shows up most legibly. What only a real database can say is that the
    /// predicate TRANSLATES at all: it is composed onto a live <c>IQueryable</c> off the storage
    /// broker and never evaluated in memory in production, so LINQ-to-Objects passing is not
    /// evidence that SQL Server was ever asked the same question. Same reasoning as the narrow
    /// reads this fixture already hosts.</para>
    ///
    /// <para>The <c>AccessBroker</c> is constructed over the fixture's REAL storage broker rather
    /// than mocked, because a mock is exactly the thing that would let a non-translatable
    /// predicate pass.</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class RetirableApprovalReviewRequestReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly IAccessBroker accessBroker;
        private readonly List<Approval> seededApprovals;
        private readonly List<ApprovalReviewRequest> seededApprovalReviewRequests;
        private readonly List<ApprovalReview> seededApprovalReviews;

        public RetirableApprovalReviewRequestReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.accessBroker = new AccessBroker(broker.StorageBroker);
            this.seededApprovals = new List<Approval>();
            this.seededApprovalReviewRequests = new List<ApprovalReviewRequest>();
            this.seededApprovalReviews = new List<ApprovalReview>();
        }

        [Fact]
        public async Task ShouldFindOnlyTheOutstandingRequestsOfTheClosingRoundAsync()
        {
            // given: a closing round with one outstanding invitation and one already gone, and a
            // DIFFERENT round whose invitation must survive — that last row is the whole reason
            // the ApprovalId conjunct has to reach SQL rather than being applied afterwards
            Approval closingApproval = await SeedApprovalAsync(ApprovalStatus.Approved);
            Approval otherApproval = await SeedApprovalAsync(ApprovalStatus.Submitted);

            ApprovalReviewRequest outstandingRequest =
                await SeedApprovalReviewRequestAsync(closingApproval.Id, isDeleted: false);

            ApprovalReviewRequest alreadyGoneRequest =
                await SeedApprovalReviewRequestAsync(closingApproval.Id, isDeleted: true);

            ApprovalReviewRequest otherRoundsRequest =
                await SeedApprovalReviewRequestAsync(otherApproval.Id, isDeleted: false);

            // when
            List<Guid> actualRetirableRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: closingApproval.Id,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualRetirableRequestIds.Should().Equal(new[] { outstandingRequest.Id });
            actualRetirableRequestIds.Should().NotContain(alreadyGoneRequest.Id);
            actualRetirableRequestIds.Should().NotContain(otherRoundsRequest.Id);
        }

        // The empty-round case lives in the unit suite alone, deliberately. Nothing about it
        // depends on a real catalogue — no index, no collation, no three-valued logic — so
        // repeating it here would buy a second assertion of the same fact at the price of a
        // database round trip.

        /// <summary>
        /// Proves the §12.5.4 business rule 4(ii) exclusion against a real catalogue. The unit
        /// suite settles it over an in-memory queryable; what only SQL Server can say is that the
        /// composed predicate TRANSLATES at all. This is exactly the failure
        /// <c>ToReviewVerdict</c> would cause if the exclusion were written to call it inside a
        /// <c>Where</c> clause rather than being expressed directly on <c>StatusId</c>.
        /// </summary>
        [Fact]
        public async Task ShouldExcludeAnAnsweredReviewersRequestFromTheRetirableSetAsync()
        {
            // given
            Approval closingApproval = await SeedApprovalAsync(ApprovalStatus.Approved);

            ApprovalReviewRequest answeredInviteeRequest =
                await SeedApprovalReviewRequestAsync(closingApproval.Id, isDeleted: false);

            ApprovalReviewRequest unansweredInviteeRequest =
                await SeedApprovalReviewRequestAsync(closingApproval.Id, isDeleted: false);

            await SeedApprovalReviewAsync(
                closingApproval.Id,
                createdBy: answeredInviteeRequest.RequestedUserId,
                statusId: ApprovalStatus.Approved);

            // when
            List<Guid> actualRetirableRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: closingApproval.Id,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualRetirableRequestIds.Should().Equal(new[] { unansweredInviteeRequest.Id });
            actualRetirableRequestIds.Should().NotContain(answeredInviteeRequest.Id);
        }

        /// <summary>
        /// The exclusion's blank-author filter must read a whitespace-only author as blank the
        /// same way <c>ActiveReviewerUserIds</c> does, and only a real database can say whether it
        /// does: SQL Server's <c>LTRIM</c>/<c>RTRIM</c> strip only the space character, so a
        /// tab-only author survives <c>.Trim() != string.Empty</c> once that expression is
        /// translated to SQL even though C#'s own <c>.Trim()</c> (and
        /// <c>string.IsNullOrWhiteSpace</c>) would call it blank. The in-memory unit suite cannot
        /// see this divergence because LINQ-to-Objects evaluates the same <c>.Trim()</c> C# would
        /// run anywhere else.
        /// </summary>
        [Fact]
        public async Task ShouldNotExcludeAnInviteeWhoseStandingReviewsAuthorIsWhitespaceOnlyAsync()
        {
            // given
            const string whitespaceOnlyAuthor = "\t";
            Approval closingApproval = await SeedApprovalAsync(ApprovalStatus.Approved);

            ApprovalReviewRequest whitespaceAuthoredRequest = await SeedApprovalReviewRequestAsync(
                closingApproval.Id,
                isDeleted: false,
                requestedUserId: whitespaceOnlyAuthor);

            await SeedApprovalReviewAsync(
                closingApproval.Id,
                createdBy: whitespaceOnlyAuthor,
                statusId: ApprovalStatus.Approved);

            // when
            List<Guid> actualRetirableRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: closingApproval.Id,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualRetirableRequestIds.Should().Equal(
                new[] { whitespaceAuthoredRequest.Id },
                because: "a standing review with a whitespace-only author carries no identity to "
                    + "exclude anybody by, the same as one with an empty-string author");
        }

        private async Task<Approval> SeedApprovalAsync(ApprovalStatus approvalStatus)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approval = new Approval
            {
                Id = Guid.NewGuid(),

                // Association rather than the zero member, so a dropped key conjunct anywhere
                // downstream cannot match by defaulting to ContentItem.
                EntityType = EntityType.Association,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = approvalStatus,
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
            };

            await this.broker.SeedAsync(approval);
            this.seededApprovals.Add(approval);

            return approval;
        }

        private async Task<ApprovalReviewRequest> SeedApprovalReviewRequestAsync(
            Guid approvalId,
            bool isDeleted,
            string requestedUserId = null)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approvalReviewRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = requestedUserId ?? Guid.NewGuid().ToString(),
                RequestedUserDisplayName = "Seeded Invitee",
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                IsDeleted = isDeleted,
                DeletedBy = isDeleted ? actorUserId : null,
                DeletedWhen = isDeleted ? now : null,
                DeletionReason = isDeleted ? "seeded" : null,
            };

            await this.broker.SeedAsync(approvalReviewRequest);
            this.seededApprovalReviewRequests.Add(approvalReviewRequest);

            return approvalReviewRequest;
        }

        private async Task<ApprovalReview> SeedApprovalReviewAsync(
            Guid approvalId,
            string createdBy,
            ApprovalStatus statusId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approvalReview = new ApprovalReview
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                StatusId = statusId,
                CreatedBy = createdBy,
                CreatedWhen = now,
                UpdatedBy = createdBy,
                UpdatedWhen = now,
            };

            await this.broker.SeedAsync(approvalReview);
            this.seededApprovalReviews.Add(approvalReview);

            return approvalReview;
        }

        // Requests and reviews first: they carry the FK, and the approvals cannot go while they
        // point at one.
        public void Dispose()
        {
            this.broker.ClearAsync(this.seededApprovalReviewRequests)
                .AsTask().GetAwaiter().GetResult();

            this.broker.ClearAsync(this.seededApprovalReviews)
                .AsTask().GetAwaiter().GetResult();

            this.broker.ClearAsync(this.seededApprovals).AsTask().GetAwaiter().GetResult();
        }
    }
}
