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
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItemSettings;
using Glory2Him.Core.Models.Enums;
using RESTFulSense.Exceptions;
using CoreContentItemSetting = Glory2Him.Core.Models.Foundations.ContentItemSettings.ContentItemSetting;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItemSettings
{
    public partial class ContentItemSettingApiTests
    {
        [Fact]
        public async Task ShouldHardDeleteContentItemSettingByIdAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = await PostRandomContentItemSettingAsync();
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            ContentItemSetting expectedContentItemSetting = inputContentItemSetting;

            try
            {
                // when
                ContentItemSetting deletedContentItemSetting =
                    await this.apiBroker.HardDeleteContentItemSettingByIdAsync(inputContentItemSetting.Id);

                List<ContentItemSetting> actualResult =
                    await this.apiBroker.GetSpecificContentItemSettingByIdAsync(inputContentItemSetting.Id);

                // then
                actualResult.Count().Should().Be(0);
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemSettingByIdAsync(inputContentItemSetting.Id);
            }
        }

        /// <summary>
        /// The same refusal on the hard verb. The invariant is about the row EXISTING, so the
        /// mechanism that removes it is irrelevant — a hard delete is not an escape hatch from
        /// §12.5.2 business rule 5, and the seeded default is left exactly where it was.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseHardDeleteOfADefaultContentItemSettingAsync()
        {
            // given
            CoreContentItemSetting seededDefault =
                await this.apiBroker.GetCoreDefaultContentItemSettingAsync(ContentType.Story);

            // when
            var hardDeleteTask =
                this.apiBroker.HardDeleteContentItemSettingByIdAsync(seededDefault.Id).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => hardDeleteTask);

            CoreContentItemSetting stillLiveDefault =
                await this.apiBroker.GetCoreDefaultContentItemSettingAsync(ContentType.Story);

            stillLiveDefault.Id.Should().Be(seededDefault.Id);
        }
    }
}
