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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.BibleReferences;
using CoreApprovalStatus = Glory2Him.Core.Models.Enums.ApprovalStatus;
using CoreBibleReference = Glory2Him.Core.Models.Foundations.BibleReferences.BibleReference;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.BibleReferences
{
    public partial class BibleReferenceApiTests
    {
        [Fact]
        public async Task ShouldTransitionBibleReferenceApprovalAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            string reviewerUserId = Guid.NewGuid().ToString();

            CoreBibleReference submittedBibleReference =
                await this.apiBroker.InsertSubmittedBibleReferenceAsync(authorUserId);

            Approval approval =
                await this.apiBroker.InsertSubmittedApprovalAsync(
                    EntityType.BibleReference, submittedBibleReference.Id, authorUserId);

            ApprovalReview approvalReview =
                await this.apiBroker.InsertApprovedReviewAsync(approval.Id, reviewerUserId);

            BibleReference inputBibleReference = await this.apiBroker.GetBibleReferenceByIdAsync(submittedBibleReference.Id);
            inputBibleReference.ApprovalStatus = ApprovalStatus.Approved;
            inputBibleReference.IsPublished = true;

            try
            {
                // when
                BibleReference actualBibleReference = await this.apiBroker.TransitionBibleReferenceApprovalAsync(inputBibleReference);

                // then
                actualBibleReference.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
                actualBibleReference.IsPublished.Should().BeTrue();
                actualBibleReference.IsApprovedByBypass.Should().BeFalse();

                CoreBibleReference storedBibleReference = await this.apiBroker.GetCoreBibleReferenceByIdAsync(submittedBibleReference.Id);
                storedBibleReference.ApprovalStatus.Should().Be(CoreApprovalStatus.Approved);
                storedBibleReference.IsPublished.Should().BeTrue();
            }
            finally
            {
                // In FK order, and outside the assertions — the arranged rows have no owning
                // endpoint, so a failure here would orphan an Approval and an ApprovalReview in
                // a database nothing else resets.
                await this.apiBroker.RemoveApprovalReviewAsync(approvalReview);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreBibleReferenceAsync(
                    await this.apiBroker.GetCoreBibleReferenceByIdAsync(submittedBibleReference.Id));
            }
        }

        [Fact]
        public async Task ShouldRejectBibleReferenceAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreBibleReference submittedBibleReference =
                await this.apiBroker.InsertSubmittedBibleReferenceAsync(authorUserId);

            Approval approval =
                await this.apiBroker.InsertSubmittedApprovalAsync(
                    EntityType.BibleReference, submittedBibleReference.Id, authorUserId);

            BibleReference inputBibleReference = await this.apiBroker.GetBibleReferenceByIdAsync(submittedBibleReference.Id);
            inputBibleReference.ApprovalStatus = ApprovalStatus.Rejected;

            try
            {
                // when
                BibleReference actualBibleReference = await this.apiBroker.TransitionBibleReferenceApprovalAsync(inputBibleReference);

                // then
                actualBibleReference.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
                actualBibleReference.IsPublished.Should().BeFalse();

                CoreBibleReference storedBibleReference = await this.apiBroker.GetCoreBibleReferenceByIdAsync(submittedBibleReference.Id);
                storedBibleReference.ApprovalStatus.Should().Be(CoreApprovalStatus.Rejected);
            }
            finally
            {
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreBibleReferenceAsync(
                    await this.apiBroker.GetCoreBibleReferenceByIdAsync(submittedBibleReference.Id));
            }
        }
    }
}
