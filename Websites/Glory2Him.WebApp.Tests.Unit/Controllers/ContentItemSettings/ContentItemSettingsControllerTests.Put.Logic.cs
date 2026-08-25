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
using Force.DeepCloner;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ContentItemSettings
{
    public partial class ContentItemSettingsControllerTests
    {
        [Fact]
        public async Task ShouldReturnOkOnPutAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            ContentItemSetting storageContentItemSetting = inputContentItemSetting.DeepClone();
            ContentItemSetting expectedContentItemSetting = storageContentItemSetting.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedContentItemSetting);

            var expectedActionResult =
                new ActionResult<ContentItemSetting>(expectedObjectResult);

            contentItemSettingServiceMock
                .Setup(service => service.ModifyContentItemSettingAsync(inputContentItemSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItemSetting);

            // when
            ActionResult<ContentItemSetting> actualActionResult = await contentItemSettingsController.PutContentItemSettingAsync(randomContentItemSetting, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            contentItemSettingServiceMock
               .Verify(service => service.ModifyContentItemSettingAsync(inputContentItemSetting, It.IsAny<CancellationToken>()),
                   Times.Once);

            contentItemSettingServiceMock.VerifyNoOtherCalls();
        }
    }
}
