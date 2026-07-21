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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    public partial class ContentItemSettingServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllContentItemSettingsAsync()
        {
            // given
            IQueryable<ContentItemSetting> randomContentItemSettings = CreateRandomContentItemSettings();
            IQueryable<ContentItemSetting> storageContentItemSettings = randomContentItemSettings;
            IQueryable<ContentItemSetting> expectedContentItemSettings = storageContentItemSettings;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemSettingsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItemSettings);

            // when
            IQueryable<ContentItemSetting> actualContentItemSettings =
                await this.contentItemSettingService.RetrieveAllContentItemSettingsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSettings.Should().BeEquivalentTo(expectedContentItemSettings);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
