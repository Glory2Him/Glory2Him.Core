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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievingApprovalSettingPublisherRoleByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ApprovalSettingPublisherRole>? nullEnvelope = null;

            var invalidApprovalSettingPublisherRoleEventException =
                new InvalidApprovalSettingPublisherRoleEventException(
                    message: "Invalid approval setting publisher role event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingPublisherRoleValidationException =
                new ApprovalSettingPublisherRoleValidationException(
                    message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingPublisherRoleEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onRetrieveTask =
                this.approvalSettingPublisherRoleService.OnRetrievingApprovalSettingPublisherRoleByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    onRetrieveTask.AsTask);

            // then
            actualApprovalSettingPublisherRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingPublisherRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingPublisherRoleValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughNotFoundValidationExceptionOnRetrievingApprovalSettingPublisherRoleByIdEventAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalSettingPublisherRole>
            {
                Content = new ApprovalSettingPublisherRole { Id = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    requestEnvelope.Content.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((ApprovalSettingPublisherRole?)null);

            // when
            ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> onRetrieveTask =
                this.approvalSettingPublisherRoleService.OnRetrievingApprovalSettingPublisherRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingPublisherRoleValidationException actualApprovalSettingPublisherRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingPublisherRoleValidationException>(
                    onRetrieveTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped —
            // the substrate wrapper must not double-wrap it.
            actualApprovalSettingPublisherRoleValidationException.InnerException
                .Should().BeOfType<NotFoundApprovalSettingPublisherRoleException>();

            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
