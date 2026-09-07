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
using Glory2Him.Core.Models.Enums;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    /// <summary>
    /// The duplicate-content probe of §3.4.2.
    ///
    /// <para>What the service owns is the gate, the request envelope, and handing the storage
    /// layer the three values the rule is stated in. The RULE ITSELF — same type and hash, live
    /// rows only, the caller's own group excluded — is a predicate in
    /// <c>IStorageBroker.ExistsContentItemContentAsync</c> now: it had to move for the check to be
    /// awaited with the caller's token instead of enumerating the collection read's live queryable
    /// on the request thread. It is proved against real SQL in
    /// <c>ContentItemNarrowReadTests</c>.</para>
    /// </summary>
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldReportContentAsExistingWhenStorageDoesAsync()
        {
            // given
            ContentType contentType = ContentType.Quote;
            string contentHash = GetRandomString();
            Guid excludedGroupId = Guid.NewGuid();

            // keyed on the caller's own values, so a service that passed anything else through
            // would fall to the unstubbed default of false and fail below
            this.storageBrokerMock.Setup(broker =>
                broker.ExistsContentItemContentAsync(
                    contentType, contentHash, excludedGroupId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // when
            bool actualResult =
                await this.contentItemService.CheckContentItemContentExistsAsync(
                    contentType,
                    contentHash,
                    excludedGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Should().BeTrue();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameCheckRequestAs(contentType, contentHash))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.ExistsContentItemContentAsync(
                    contentType, contentHash, excludedGroupId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReportContentAsNotExistingWhenStorageDoesAsync()
        {
            // given
            ContentType contentType = ContentType.Quote;
            string contentHash = GetRandomString();

            this.storageBrokerMock.Setup(broker =>
                broker.ExistsContentItemContentAsync(
                    It.IsAny<ContentType>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // when
            bool actualResult =
                await this.contentItemService.CheckContentItemContentExistsAsync(
                    contentType,
                    contentHash,
                    excludedGroupId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Should().BeFalse();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameCheckRequestAs(contentType, contentHash))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.ExistsContentItemContentAsync(
                    It.IsAny<ContentType>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// An ABSENT exclusion is not the same as excluding nothing-in-particular: an add has no
        /// group to spare, and a null that arrived as <c>Guid.Empty</c> would key the exclusion on
        /// a group no row belongs to. It has to reach storage as null.
        /// </summary>
        [Fact]
        public async Task ShouldCarryAnAbsentExcludedGroupIdToStorageAsNullAsync()
        {
            // given
            Guid? capturedExcludedGroupId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.ExistsContentItemContentAsync(
                    It.IsAny<ContentType>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((
                            ContentType _,
                            string __,
                            Guid? excludedGroupId,
                            CancellationToken ___) =>
                        {
                            capturedExcludedGroupId = excludedGroupId;

                            return false;
                        });

            // when
            await this.contentItemService.CheckContentItemContentExistsAsync(
                ContentType.Quote,
                GetRandomString(),
                excludedGroupId: null,
                TestContext.Current.CancellationToken);

            // then
            capturedExcludedGroupId.Should().BeNull();
        }

        /// <summary>
        /// The token reaches the database call, which is the point of the narrow read: the
        /// <c>Any(...)</c> this replaced ran on the request thread with the token left behind.
        /// </summary>
        [Fact]
        public async Task ShouldPassTheCancellationTokenToTheStorageBrokerOnCheckContentExistsAsync()
        {
            // given
            ContentType contentType = ContentType.Quote;
            string contentHash = GetRandomString();
            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.storageBrokerMock.Setup(broker =>
                broker.ExistsContentItemContentAsync(
                    It.IsAny<ContentType>(), It.IsAny<string>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // when
            await this.contentItemService.CheckContentItemContentExistsAsync(
                contentType,
                contentHash,
                excludedGroupId: null,
                inputCancellationToken);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.ExistsContentItemContentAsync(
                    contentType, contentHash, null, inputCancellationToken),
                Times.Once);
        }
    }
}
