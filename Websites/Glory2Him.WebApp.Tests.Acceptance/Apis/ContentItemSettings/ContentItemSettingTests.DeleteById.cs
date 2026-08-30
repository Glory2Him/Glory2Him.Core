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
        public async Task ShouldDeleteContentItemSettingByIdAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = await PostRandomContentItemSettingAsync();
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            ContentItemSetting expectedContentItemSetting = inputContentItemSetting;

            try
            {
                // when
                ContentItemSetting deletedContentItemSetting =
                    await this.apiBroker.DeleteContentItemSettingByIdAsync(inputContentItemSetting.Id);

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
        /// The floor under the default scope (§12.5.2 business rule 5). Every content type must
        /// always have a live default, so the delete endpoint refuses one outright rather than
        /// leaving the type with no resolvable setting at all.
        ///
        /// <para>400 rather than 404: the row is there and every caller may read it — settings are
        /// public — so the answer names the rule instead of pretending the row is missing.</para>
        ///
        /// <para>No arrangement and no teardown, and that is the assertion. The row under test is
        /// the SEEDED default, chosen precisely because a passing run leaves it exactly where it
        /// was; a regression that let the delete through would strip a content type of its default
        /// for every test that followed.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRefuseDeleteOfADefaultContentItemSettingAsync()
        {
            // given
            CoreContentItemSetting seededDefault =
                await this.apiBroker.GetCoreDefaultContentItemSettingAsync(ContentType.Quote);

            // when
            var deleteTask =
                this.apiBroker.DeleteContentItemSettingByIdAsync(seededDefault.Id).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => deleteTask);

            CoreContentItemSetting stillLiveDefault =
                await this.apiBroker.GetCoreDefaultContentItemSettingAsync(ContentType.Quote);

            stillLiveDefault.Id.Should().Be(seededDefault.Id);
        }
    }
}
