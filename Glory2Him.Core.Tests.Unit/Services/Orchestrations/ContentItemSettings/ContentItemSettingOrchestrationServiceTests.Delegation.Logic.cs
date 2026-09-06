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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItemSettings
{
    // THE FIVE VERBS THAT COORDINATE NOTHING. Asserted rather than assumed: a pass-through that
    // drops an argument — a deletion reason, a cancellation token, the id itself — is precisely
    // the failure a delegating layer can hide, and it would look like the foundation misbehaving.
    //
    // Each also asserts the ITEM service is never touched. Deriving anything here would be wrong:
    // a modify cannot move a row's scope (the foundation pins both fields against storage) and a
    // removal reads the stored row, whose type was derived when it was created.
    public partial class ContentItemSettingOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldDelegateModifyToTheFoundationUnchangedAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            ContentItemSetting expectedContentItemSetting = inputContentItemSetting;

            this.contentItemSettingServiceMock.Setup(service =>
                service.ModifyContentItemSettingAsync(
                    inputContentItemSetting,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedContentItemSetting);

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingOrchestrationService.ModifyContentItemSettingAsync(
                    inputContentItemSetting,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeSameAs(expectedContentItemSetting);

            this.contentItemSettingServiceMock.Verify(service =>
                service.ModifyContentItemSettingAsync(
                    inputContentItemSetting,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDelegateRetrieveAllToTheFoundationUnchangedAsync()
        {
            // given
            IQueryable<ContentItemSetting> expectedContentItemSettings =
                new[] { CreateRandomContentItemSetting() }.AsQueryable();

            this.contentItemSettingServiceMock.Setup(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentItemSettings);

            // when
            IQueryable<ContentItemSetting> actualContentItemSettings =
                await this.contentItemSettingOrchestrationService
                    .RetrieveAllContentItemSettingsAsync(TestContext.Current.CancellationToken);

            // then
            actualContentItemSettings.Should().BeSameAs(expectedContentItemSettings);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RetrieveAllContentItemSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDelegateRetrieveByIdToTheFoundationUnchangedAsync()
        {
            // given
            Guid inputContentItemSettingId = GetRandomId();
            ContentItemSetting expectedContentItemSetting = CreateRandomContentItemSetting();

            this.contentItemSettingServiceMock.Setup(service =>
                service.RetrieveContentItemSettingByIdAsync(
                    inputContentItemSettingId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedContentItemSetting);

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingOrchestrationService
                    .RetrieveContentItemSettingByIdAsync(
                        inputContentItemSettingId,
                        TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeSameAs(expectedContentItemSetting);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RetrieveContentItemSettingByIdAsync(
                    inputContentItemSettingId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // THE DELETION REASON IS CARRIED, not defaulted away. It is the one argument on these five
        // that a pass-through could silently lose while every other assertion still passed.
        [Fact]
        public async Task ShouldDelegateRemoveByIdWithItsDeletionReasonAsync()
        {
            // given
            Guid inputContentItemSettingId = GetRandomId();
            string inputDeletionReason = GetRandomString();
            ContentItemSetting expectedContentItemSetting = CreateRandomContentItemSetting();

            this.contentItemSettingServiceMock.Setup(service =>
                service.RemoveContentItemSettingByIdAsync(
                    inputContentItemSettingId,
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedContentItemSetting);

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingOrchestrationService
                    .RemoveContentItemSettingByIdAsync(
                        inputContentItemSettingId,
                        inputDeletionReason,
                        TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeSameAs(expectedContentItemSetting);

            this.contentItemSettingServiceMock.Verify(service =>
                service.RemoveContentItemSettingByIdAsync(
                    inputContentItemSettingId,
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldDelegateHardRemoveByIdToTheFoundationUnchangedAsync()
        {
            // given
            Guid inputContentItemSettingId = GetRandomId();
            ContentItemSetting expectedContentItemSetting = CreateRandomContentItemSetting();

            this.contentItemSettingServiceMock.Setup(service =>
                service.HardRemoveContentItemSettingByIdAsync(
                    inputContentItemSettingId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedContentItemSetting);

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingOrchestrationService
                    .HardRemoveContentItemSettingByIdAsync(
                        inputContentItemSettingId,
                        TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeSameAs(expectedContentItemSetting);

            this.contentItemSettingServiceMock.Verify(service =>
                service.HardRemoveContentItemSettingByIdAsync(
                    inputContentItemSettingId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
