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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievingApprovalSettingReviewerRoleByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ApprovalSettingReviewerRole>? nullEnvelope = null;

            var invalidApprovalSettingReviewerRoleEventException =
                new InvalidApprovalSettingReviewerRoleEventException(
                    message: "Invalid approval setting reviewer role event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> onRetrieveTask =
                this.approvalSettingReviewerRoleService.OnRetrievingApprovalSettingReviewerRoleByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    onRetrieveTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughNotFoundValidationExceptionOnRetrievingApprovalSettingReviewerRoleByIdEventAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalSettingReviewerRole>
            {
                Content = new ApprovalSettingReviewerRole { Id = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope.Content.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((ApprovalSettingReviewerRole?)null);

            // when
            ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> onRetrieveTask =
                this.approvalSettingReviewerRoleService.OnRetrievingApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    onRetrieveTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped —
            // the substrate wrapper must not double-wrap it.
            actualApprovalSettingReviewerRoleValidationException.InnerException
                .Should().BeOfType<NotFoundApprovalSettingReviewerRoleException>();

            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
