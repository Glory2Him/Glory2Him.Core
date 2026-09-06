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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItemSettings
{
    public partial class ContentItemSettingOrchestrationServiceTests
    {
        // THE WHOLE POINT OF THIS LAYER. The caller says Devotional; the item is a Quote; the row
        // that reaches the foundation says Quote. Without this, the row's own claim decides which
        // publisher tier may write it, and a Devotional publisher takes a quote's override scope.
        [Fact]
        public async Task ShouldDeriveContentTypeFromTheNamedContentItemOnAddAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomOverrideRequest();
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            Guid contentItemId = inputContentItemSetting.ContentItemId.Value;

            ContentItem storageContentItem =
                CreateContentItemOfType(contentItemId, ActualContentType);

            ContentItemSetting expectedContentItemSetting = inputContentItemSetting;

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.contentItemSettingServiceMock.Setup(service =>
                service.AddContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedContentItemSetting);

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    inputContentItemSetting,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeSameAs(expectedContentItemSetting);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(contentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            // the row the foundation was handed carries the ITEM's type, not the caller's
            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    It.Is<ContentItemSetting>(setting =>
                        setting.ContentType == ActualContentType
                            && setting.ContentItemId == contentItemId),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A DEFAULT NAMES NO ITEM, so there is nothing to resolve and its content type is the whole
        // of what the row declares. Asserted as "the item service is never touched" rather than
        // "the type survived", because a resolve attempt on a null id is the bug worth catching.
        [Fact]
        public async Task ShouldNotResolveAnyContentItemWhenAddingAContentTypeDefaultAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            randomContentItemSetting.ContentItemId = null;
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            ContentItemSetting expectedContentItemSetting = inputContentItemSetting;

            this.contentItemSettingServiceMock.Setup(service =>
                service.AddContentItemSettingAsync(
                    inputContentItemSetting,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedContentItemSetting);

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingOrchestrationService.AddContentItemSettingAsync(
                    inputContentItemSetting,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeSameAs(expectedContentItemSetting);
            actualContentItemSetting.ContentType.Should().Be(CallerClaimedContentType);

            this.contentItemSettingServiceMock.Verify(service =>
                service.AddContentItemSettingAsync(
                    inputContentItemSetting,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
