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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Securities;
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

            foreach (ContentItemSetting contentItemSetting in randomContentItemSettings)
                contentItemSetting.IsDeleted = false;

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

        [Fact]
        public async Task ShouldExcludeDeletedContentItemSettingsOnRetrieveAllAsync()
        {
            // given: a soft-deleted setting drops out of the set for every caller,
            // including an administrator — the collection read never reveals removed rows
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ContentItemSetting liveContentItemSetting = CreateRandomContentItemSetting();
            liveContentItemSetting.IsDeleted = false;
            ContentItemSetting deletedContentItemSetting = CreateRandomContentItemSetting();
            deletedContentItemSetting.IsDeleted = true;

            IQueryable<ContentItemSetting> storageContentItemSettings =
                new[] { liveContentItemSetting, deletedContentItemSetting }.AsQueryable();

            IQueryable<ContentItemSetting> expectedContentItemSettings =
                new[] { liveContentItemSetting }.AsQueryable();

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

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldRetrieveAllNonDeletedContentItemSettingsWhenUserIsNotAuthenticatedAsync(
            SecurityContext anonymousSecurityContext)
        {
            // given: settings render the public site, so an anonymous caller sees the
            // very same non-deleted set an administrator sees
            this.ambientSecurityContext = anonymousSecurityContext;
            ContentItemSetting liveContentItemSetting = CreateRandomContentItemSetting();
            liveContentItemSetting.IsDeleted = false;
            ContentItemSetting deletedContentItemSetting = CreateRandomContentItemSetting();
            deletedContentItemSetting.IsDeleted = true;

            IQueryable<ContentItemSetting> storageContentItemSettings =
                new[] { liveContentItemSetting, deletedContentItemSetting }.AsQueryable();

            IQueryable<ContentItemSetting> expectedContentItemSettings =
                new[] { liveContentItemSetting }.AsQueryable();

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
