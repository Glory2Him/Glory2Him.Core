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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Models.Views.Users.Exceptions;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Users
{
    public partial class UsersViewServiceTests
    {
        [Fact]
        public async Task ShouldThrowServiceExceptionAndLogItWhenBrokerFails()
        {
            // given
            var brokerException = new Exception("Identity store unavailable.");

            this.identityBrokerMock.Setup(broker =>
                broker.SelectAllUsers())
                    .Throws(brokerException);

            // when
            Func<Task<List<UserView>>> retrieveAllUsersTask = async () =>
                await this.usersViewService.RetrieveAllUsersAsync();

            // then
            UsersViewServiceException actualException =
                await Assert.ThrowsAsync<UsersViewServiceException>(retrieveAllUsersTask);

            actualException.InnerException
                .Should().BeOfType<FailedUsersViewServiceException>();

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.IsAny<UsersViewServiceException>()),
                    Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
