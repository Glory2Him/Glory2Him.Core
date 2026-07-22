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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievingApprovalSettingByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ApprovalSetting>? nullEnvelope = null;

            var invalidApprovalSettingEventException =
                new InvalidApprovalSettingEventException(
                    message: "Invalid approval setting event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingEventException);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onRetrieveTask =
                this.approvalSettingService.OnRetrievingApprovalSettingByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    onRetrieveTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughNotFoundValidationExceptionOnRetrievingApprovalSettingByIdEventAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalSetting>
            {
                Content = new ApprovalSetting { Id = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    requestEnvelope.Content.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((ApprovalSetting?)null);

            // when
            ValueTask<EventEnvelope<ApprovalSetting>?> onRetrieveTask =
                this.approvalSettingService.OnRetrievingApprovalSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    onRetrieveTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped —
            // the substrate wrapper must not double-wrap it.
            actualApprovalSettingValidationException.InnerException
                .Should().BeOfType<NotFoundApprovalSettingException>();

            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
