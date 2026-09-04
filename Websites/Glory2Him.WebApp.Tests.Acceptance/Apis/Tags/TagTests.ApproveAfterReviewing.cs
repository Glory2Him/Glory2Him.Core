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
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Models.Tags;
using RESTFulSense.Exceptions;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Tags
{
    /// <summary>
    /// §8.6 regardless-rule 1 at the HTTP boundary: a reviewer who decides. The rule and its one
    /// exemption, side by side, because the exemption is only safe while the rule is seen to
    /// hold for everybody else.
    /// </summary>
    public partial class TagApiTests
    {
        /// <summary>
        /// THE EXEMPTION. An administrator who holds one of the two required reviews still
        /// applies the decision — on a small team they are often the only reviewer, and holding
        /// the bar against them would make every round they touch end in a bypass. Their review
        /// counts like any other: it is one of the two the seeded policy asks for, and the
        /// outcome is a plain approval, not a bypass.
        /// </summary>
        [Fact]
        public async Task ShouldLetAnAdministratorApproveATagTheyReviewedAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            string administratorUserId = Guid.NewGuid().ToString();

            CoreTag submittedTag = await this.apiBroker.InsertSubmittedTagAsync(authorUserId);

            Approval approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                EntityType.Tag, submittedTag.Id, authorUserId);

            ApprovalReview administratorsReview = await this.apiBroker.InsertApprovedReviewAsync(
                approval.Id, administratorUserId);

            ApprovalReview otherReview = await this.apiBroker.InsertApprovedReviewAsync(
                approval.Id, Guid.NewGuid().ToString());

            Tag inputTag = await this.apiBroker.GetTagByIdAsync(submittedTag.Id);
            inputTag.ApprovalStatus = ApprovalStatus.Approved;
            inputTag.IsPublished = true;

            try
            {
                this.apiBroker.ActAs(administratorUserId, Roles.Administrators);

                // when
                Tag actualTag = await this.apiBroker.TransitionTagApprovalAsync(inputTag);

                // then
                actualTag.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
                actualTag.IsApprovedByBypass.Should().BeFalse();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveApprovalReviewAsync(otherReview);
                await this.apiBroker.RemoveApprovalReviewAsync(administratorsReview);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreTagAsync(
                    await this.apiBroker.GetCoreTagByIdAsync(submittedTag.Id));
            }
        }

        /// <summary>
        /// THE RULE, still holding where it should. A publisher who reviewed has spent their vote
        /// on this round and is refused the decision — they refer it to an administrator, or to
        /// another publisher. Refused BEFORE the bypass is considered, so no waiver reaches it.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseAPublisherApprovingATagTheyReviewedAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            string publisherUserId = Guid.NewGuid().ToString();

            CoreTag submittedTag = await this.apiBroker.InsertSubmittedTagAsync(authorUserId);

            Approval approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                EntityType.Tag, submittedTag.Id, authorUserId);

            ApprovalReview publishersReview = await this.apiBroker.InsertApprovedReviewAsync(
                approval.Id, publisherUserId);

            ApprovalReview otherReview = await this.apiBroker.InsertApprovedReviewAsync(
                approval.Id, Guid.NewGuid().ToString());

            Tag inputTag = await this.apiBroker.GetTagByIdAsync(submittedTag.Id);
            inputTag.ApprovalStatus = ApprovalStatus.Approved;
            inputTag.IsPublished = true;

            try
            {
                this.apiBroker.ActAs(publisherUserId, Roles.Publishers);

                // when
                var approveTask = this.apiBroker.TransitionTagApprovalAsync(inputTag).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => approveTask);

                CoreTag storedTag = await this.apiBroker.GetCoreTagByIdAsync(submittedTag.Id);
                storedTag.ApprovalStatus.Should().Be(Glory2Him.Core.Models.Enums.ApprovalStatus.Submitted);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveApprovalReviewAsync(otherReview);
                await this.apiBroker.RemoveApprovalReviewAsync(publishersReview);
                await this.apiBroker.RemoveApprovalAsync(approval);
                await this.apiBroker.RemoveCoreTagAsync(
                    await this.apiBroker.GetCoreTagByIdAsync(submittedTag.Id));
            }
        }
    }
}
