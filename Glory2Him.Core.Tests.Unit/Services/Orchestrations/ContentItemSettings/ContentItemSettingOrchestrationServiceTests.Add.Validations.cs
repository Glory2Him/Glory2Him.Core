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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Orchestrations.ContentItemSettings.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItemSettings
{
    public partial class ContentItemSettingOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfContentItemSettingIsNullAndLogItAsync()
        {
            // given
            ContentItemSetting nullContentItemSetting = null;

            var nullContentItemSettingOrchestrationException =
                new NullContentItemSettingOrchestrationException(
                    message: "Content item setting is null.");

            var expectedContentItemSettingOrchestrationValidationException =
                new ContentItemSettingOrchestrationValidationException(
                    message: "Content item setting orchestration validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: nullContentItemSettingOrchestrationException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    nullContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationValidationException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedContentItemSettingOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingOrchestrationValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // AN UNRESOLVABLE ITEM IS NOT THE SAME AS A REFUSED ONE. The item's own service reports a
        // missing OR non-visible row as a validation failure and has already logged the real
        // reason; to this flow the reference simply did not resolve, and the caller is told that
        // and not which of the two applied. Nothing is written.
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfTheNamedContentItemDoesNotResolveAndLogItAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomOverrideRequest();
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            Guid contentItemId = inputContentItemSetting.ContentItemId.Value;

            var notFoundContentItemException =
                new NotFoundContentItemException(
                    message: $"Content item not found with id: {contentItemId}.");

            var contentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemException);

            var notFoundContentItemSettingOrchestrationException =
                new NotFoundContentItemSettingOrchestrationException(
                    message: $"The content item was not found with id: {contentItemId}.");

            var expectedContentItemSettingOrchestrationValidationException =
                new ContentItemSettingOrchestrationValidationException(
                    message: "Content item setting orchestration validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: notFoundContentItemSettingOrchestrationException);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(contentItemValidationException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    inputContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationValidationException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedContentItemSettingOrchestrationValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(contentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingOrchestrationValidationException))),
                Times.Once);

            // NOTHING WAS WRITTEN — the row never reached the foundation
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
