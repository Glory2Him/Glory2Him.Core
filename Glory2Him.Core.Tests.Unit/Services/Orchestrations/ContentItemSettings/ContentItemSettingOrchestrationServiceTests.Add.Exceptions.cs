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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Orchestrations.ContentItemSettings.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItemSettings
{
    public partial class ContentItemSettingOrchestrationServiceTests
    {
        // THE FOUNDATION'S ANSWERS KEEP THEIR CATEGORY. The exposer maps this service's exceptions
        // to the status codes it once mapped the foundation's to, so a validation failure that
        // arrived as a dependency failure — or the reverse — would silently move a 400 to a 424.
        [Theory]
        [MemberData(nameof(ContentItemSettingValidationExceptions))]
        public async Task ShouldThrowValidationExceptionOnAddIfFoundationValidationErrorOccursAndLogItAsync(
            Xeption foundationException)
        {
            // given
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentItemId = null;

            var expectedException =
                new ContentItemSettingOrchestrationValidationException(
                    message: "Content item setting orchestration validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)
                        ?? foundationException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationValidationException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ContentItemSettingDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddIfFoundationDependencyValidationErrorOccursAndLogItAsync(
            Xeption foundationException)
        {
            // given
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentItemId = null;

            var expectedException =
                new ContentItemSettingOrchestrationDependencyValidationException(
                    message: "Content item setting orchestration dependency validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)
                        ?? foundationException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationDependencyValidationException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ContentItemSettingDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddIfFoundationDependencyErrorOccursAndLogItAsync(
            Xeption foundationException)
        {
            // given
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentItemId = null;

            var expectedException =
                new ContentItemSettingOrchestrationDependencyException(
                    message: "Content item setting orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)
                        ?? foundationException);

            this.contentItemSettingServiceMock.Setup(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationDependencyException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
