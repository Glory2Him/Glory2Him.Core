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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    /// <summary>
    /// The tip derivation, asked as a boolean rather than by materialising the group.
    ///
    /// <para>What the SERVICE owns: the contribution gate, the group-id validation, and passing
    /// the caller's group, version and token straight through. WHICH rows count as higher — live
    /// only, same group, strictly greater — is a storage predicate, proved against a real
    /// catalogue in <c>LinkNarrowReadTests</c>.</para>
    ///
    /// <para><b>The read is UNFILTERED, and that is the point.</b> It replaced a derivation taken
    /// from the caller-facing collection read, where a contributor who could not SEE a newer
    /// sibling was told their row was the tip and edited it in place. A version question is
    /// structural: a lineage is not re-shaped by a row being invisible to the person asking.</para>
    /// </summary>
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldReportAHigherVersionExistsWhenStorageDoesAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid inputGroupId = Guid.NewGuid();
            int inputVersion = GetRandomNumber();

            // keyed on the caller's own values, so a service that passed anything else through
            // would fall to the unstubbed default of false and fail below
            this.storageBrokerMock.Setup(broker =>
                broker.ExistsHigherLiveLinkVersionInGroupAsync(
                    inputGroupId, inputVersion, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // when
            bool actualResult =
                await this.linkService.CheckHigherLinkVersionExistsAsync(
                    inputGroupId,
                    inputVersion,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Should().BeTrue();

            this.storageBrokerMock.Verify(broker =>
                broker.ExistsHigherLiveLinkVersionInGroupAsync(
                    inputGroupId, inputVersion, It.IsAny<CancellationToken>()),
                Times.Once);

            // and NOT through the caller-facing collection read, which is what made the answer
            // depend on who was asking
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinksByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldReportNoHigherVersionWhenStorageDoesAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.ExistsHigherLiveLinkVersionInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // when
            bool actualResult =
                await this.linkService.CheckHigherLinkVersionExistsAsync(
                    Guid.NewGuid(),
                    version: 1,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Should().BeFalse();
        }

        /// <summary>
        /// The token has to reach the database call — the whole reason this read exists in the
        /// storage layer rather than as an Any() over a materialised group.
        /// </summary>
        [Fact]
        public async Task ShouldPassTheCancellationTokenToTheStorageBrokerOnCheckHigherVersionAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid inputGroupId = Guid.NewGuid();
            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.storageBrokerMock.Setup(broker =>
                broker.ExistsHigherLiveLinkVersionInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // when
            await this.linkService.CheckHigherLinkVersionExistsAsync(
                inputGroupId,
                version: 4,
                inputCancellationToken);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.ExistsHigherLiveLinkVersionInGroupAsync(
                    inputGroupId, 4, inputCancellationToken),
                Times.Once);
        }

        /// <summary>
        /// The gate is the read's only authorization check, and it reads over the UNFILTERED
        /// store — so if it goes, the shape of a lineage is readable by a blocked caller.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnCheckHigherVersionIfCallerIsNotAuthenticatedAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };

            // when
            ValueTask<bool> checkTask =
                this.linkService.CheckHigherLinkVersionExistsAsync(
                    Guid.NewGuid(),
                    version: 1,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(checkTask.AsTask);

            // then
            actualException.InnerException.Should().BeOfType<UnauthorizedLinkException>();

            this.storageBrokerMock.Verify(broker =>
                broker.ExistsHigherLiveLinkVersionInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// An unresolved group would key the read on Guid.Empty and answer "no higher version",
        /// which the modify path reads as "this row is the tip" — the most dangerous possible
        /// default for a caller bug to take.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnCheckHigherVersionIfGroupIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            // when
            ValueTask<bool> checkTask =
                this.linkService.CheckHigherLinkVersionExistsAsync(
                    Guid.Empty,
                    version: 1,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(checkTask.AsTask);

            // then
            actualException.InnerException.Should().BeOfType<InvalidLinkException>();

            this.storageBrokerMock.Verify(broker =>
                broker.ExistsHigherLiveLinkVersionInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
