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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Storages.Identity;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Foundations.IdentityUsers.Exceptions;
using Glory2Him.Core.Services.Foundations.IdentityUsers;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.IdentityUsers
{
    /// <summary>
    /// The read-only identity-store foundation (design 12.7.1).
    ///
    /// <para>Its whole contract is the RULES it applies around the read - fail closed on an empty
    /// tier, normalise the names, categorise failures - because the query itself lives in the
    /// broker where EF belongs. So these tests assert what the broker is ASKED, not what a
    /// database would answer.</para>
    /// </summary>
    public class IdentityUserServiceTests
    {
        private readonly Mock<IIdentityCoreStorageBroker> identityCoreStorageBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IIdentityUserService identityUserService;

        public IdentityUserServiceTests()
        {
            this.identityCoreStorageBrokerMock = new Mock<IIdentityCoreStorageBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.identityUserService = new IdentityUserService(
                identityCoreStorageBroker: this.identityCoreStorageBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        public static TheoryData<string[]> EmptyRoleNameSets() =>
            new TheoryData<string[]>
            {
                null,
                new string[0],
                new[] { null, string.Empty, "   " },
            };

        /// <summary>
        /// The fail-closed rule, and the most important thing this service does. An empty tier
        /// means the caller composed the role names wrongly; returning everybody would turn a
        /// composition bug into a directory dump, which is the worst possible failure for a
        /// user-enumeration surface. The broker is not even reached.
        /// </summary>
        [Theory]
        [MemberData(nameof(EmptyRoleNameSets))]
        public async Task ShouldReturnNoUsersWhenNoUsableRoleNamesAreGivenAsync(string[] roleNames)
        {
            // when
            IReadOnlyList<IdentityUser> members =
                await this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                    roleNames,
                    TestContext.Current.CancellationToken);

            // then
            members.Should().BeEmpty();

            this.identityCoreStorageBrokerMock.Verify(broker =>
                broker.SelectIdentityUsersInRolesAsync(
                    It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.identityCoreStorageBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Names are upper-cased and trimmed before they reach the broker, which is what makes the
        /// match independent of the host normalizer and of however a caller spelled the tier.
        /// Duplicates collapse, so a subject appearing twice does not widen the query.
        /// </summary>
        [Fact]
        public async Task ShouldNormalizeAndDeduplicateRoleNamesBeforeReadingAsync()
        {
            // given
            IReadOnlyList<string> capturedRoleNames = null;

            this.identityCoreStorageBrokerMock.Setup(broker =>
                broker.SelectIdentityUsersInRolesAsync(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<IReadOnlyList<string>, CancellationToken>(
                            (roleNames, token) => capturedRoleNames = roleNames)
                        .ReturnsAsync(new List<IdentityUser>());

            // when
            await this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                new[] { "  Tag-Reviewers  ", "tag-reviewers", "TAG-PUBLISHERS", null, "  " },
                TestContext.Current.CancellationToken);

            // then
            capturedRoleNames.Should().BeEquivalentTo(new[] { "TAG-REVIEWERS", "TAG-PUBLISHERS" });
        }

        [Fact]
        public async Task ShouldReturnWhateverTheBrokerReportsAsync()
        {
            // given
            var expectedMembers = new List<IdentityUser>
            {
                new IdentityUser { Id = Guid.NewGuid(), UserName = "someone" },
            };

            this.identityCoreStorageBrokerMock.Setup(broker =>
                broker.SelectIdentityUsersInRolesAsync(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedMembers);

            // when
            IReadOnlyList<IdentityUser> members =
                await this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                    new[] { "Tag-Reviewers" },
                    TestContext.Current.CancellationToken);

            // then
            members.Should().BeEquivalentTo(expectedMembers);
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnSqlErrorAndLogItAsync()
        {
            // given
            SqlException sqlException =
                (SqlException)System.Runtime.CompilerServices.RuntimeHelpers
                    .GetUninitializedObject(typeof(SqlException));

            this.identityCoreStorageBrokerMock.Setup(broker =>
                broker.SelectIdentityUsersInRolesAsync(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<IReadOnlyList<IdentityUser>> retrieveTask =
                this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                    new[] { "Tag-Reviewers" },
                    TestContext.Current.CancellationToken);

            // then: the security database being unreachable is a CRITICAL dependency failure,
            // not an ordinary one - the invitation flow cannot decide eligibility without it.
            await Assert.ThrowsAsync<IdentityUserDependencyException>(retrieveTask.AsTask);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Xeptions.Xeption>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnTimeoutAndLogItAsync()
        {
            // given
            this.identityCoreStorageBrokerMock.Setup(broker =>
                broker.SelectIdentityUsersInRolesAsync(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new OperationCanceledException());

            // when
            ValueTask<IReadOnlyList<IdentityUser>> retrieveTask =
                this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                    new[] { "Tag-Reviewers" },
                    TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<IdentityUserDependencyException>(retrieveTask.AsTask);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.IsAny<Xeptions.Xeption>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldRethrowWhenCancellationIsRequestedAsync()
        {
            // given: a GENUINE cancellation passes through rather than being categorised as a
            // timeout - reporting an abandoned request as a storage fault would page somebody
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<IReadOnlyList<IdentityUser>> retrieveTask =
                this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                    new[] { "Tag-Reviewers" },
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAnyAsync<OperationCanceledException>(retrieveTask.AsTask);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnUnexpectedErrorAndLogItAsync()
        {
            // given
            this.identityCoreStorageBrokerMock.Setup(broker =>
                broker.SelectIdentityUsersInRolesAsync(
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception());

            // when
            ValueTask<IReadOnlyList<IdentityUser>> retrieveTask =
                this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                    new[] { "Tag-Reviewers" },
                    TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<IdentityUserServiceException>(retrieveTask.AsTask);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.IsAny<Xeptions.Xeption>()),
                Times.Once);
        }
    }
}
