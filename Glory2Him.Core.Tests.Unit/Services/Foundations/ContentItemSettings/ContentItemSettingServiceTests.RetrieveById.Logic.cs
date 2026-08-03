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
        public async Task ShouldRetrieveContentItemSettingByIdAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            ContentItemSetting storageContentItemSetting = randomContentItemSetting;
            storageContentItemSetting.IsDeleted = false;
            ContentItemSetting expectedContentItemSetting = storageContentItemSetting;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingService.RetrieveContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeEquivalentTo(expectedContentItemSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldRetrieveContentItemSettingByIdWhenUserIsNotAuthenticatedAsync(
            SecurityContext anonymousSecurityContext)
        {
            // given: settings drive anonymous page rendering, so a non-deleted row is
            // readable without authenticating
            this.ambientSecurityContext = anonymousSecurityContext;
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            ContentItemSetting storageContentItemSetting = randomContentItemSetting;
            storageContentItemSetting.IsDeleted = false;
            ContentItemSetting expectedContentItemSetting = storageContentItemSetting;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingService.RetrieveContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeEquivalentTo(expectedContentItemSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldRetrieveContentItemSettingByIdWhenUserIsNotAdminAsync(
            string[] nonAdminRoles)
        {
            // given: no role is required to read a setting — only writing one is gated
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            ContentItemSetting storageContentItemSetting = randomContentItemSetting;
            storageContentItemSetting.IsDeleted = false;
            ContentItemSetting expectedContentItemSetting = storageContentItemSetting;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingService.RetrieveContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeEquivalentTo(expectedContentItemSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    randomContentItemSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
