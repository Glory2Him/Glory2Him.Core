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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalSettings
{
    public partial class ApprovalSettingsControllerTests
    {
        [Fact]
        public async Task ShouldReturnCreatedOnPostAsync()
        {
            // given
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();
            ApprovalSetting inputApprovalSetting = randomApprovalSetting;
            ApprovalSetting addedApprovalSetting = inputApprovalSetting.DeepClone();
            ApprovalSetting expectedApprovalSetting = addedApprovalSetting.DeepClone();

            var expectedObjectResult =
                new CreatedObjectResult(expectedApprovalSetting);

            var expectedActionResult =
                new ActionResult<ApprovalSetting>(expectedObjectResult);

            approvalSettingServiceMock
                .Setup(service => service.AddApprovalSettingAsync(inputApprovalSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(addedApprovalSetting);

            // when
            ActionResult<ApprovalSetting> actualActionResult = await approvalSettingsController.PostApprovalSettingAsync(randomApprovalSetting, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            approvalSettingServiceMock
               .Verify(service => service.AddApprovalSettingAsync(inputApprovalSetting, It.IsAny<CancellationToken>()),
                   Times.Once);

            approvalSettingServiceMock.VerifyNoOtherCalls();
        }
    }
}
