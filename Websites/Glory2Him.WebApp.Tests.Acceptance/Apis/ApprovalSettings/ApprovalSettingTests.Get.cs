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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalSettings;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalSettings
{
    public partial class ApprovalSettingApiTests
    {
        [Fact]
        public async Task ShouldGetAllApprovalSettingsAsync()
        {
            // given
            List<ApprovalSetting> randomApprovalSettings = await PostRandomApprovalSettingsAsync();
            List<ApprovalSetting> expectedApprovalSettings = randomApprovalSettings;

            // when
            List<ApprovalSetting> actualApprovalSettings = await this.apiBroker.GetAllApprovalSettingsAsync();

            // then
            try
            {
                foreach (ApprovalSetting expectedApprovalSetting in expectedApprovalSettings)
                {
                    ApprovalSetting actualApprovalSetting =
                        actualApprovalSettings.Single(approvalSetting => approvalSetting.Id == expectedApprovalSetting.Id);

                    actualApprovalSetting.Should().BeEquivalentTo(expectedApprovalSetting, options => options
                        .Excluding(property => property.CreatedBy)
                        .Excluding(property => property.CreatedWhen)
                        .Excluding(property => property.UpdatedBy)
                        .Excluding(property => property.UpdatedWhen));
                }
            }
            finally
            {
                // Cleanup is driven off what was POSTED, not off what the read returned, runs
                // even when an assertion throws, and removes the row rather than soft-deleting
                // it. Deleting inside the assertion loop left every row the loop had not reached
                // yet, and going through the API left a soft-deleted row behind either way.
                foreach (ApprovalSetting postedApprovalSetting in randomApprovalSettings)
                {
                    await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(postedApprovalSetting.Id);
                }
            }
        }
    }
}
