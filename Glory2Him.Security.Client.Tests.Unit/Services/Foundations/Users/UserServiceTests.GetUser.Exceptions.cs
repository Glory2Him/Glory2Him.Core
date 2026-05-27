// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Foundations.Users;
using Glory2Him.Security.Client.Models.Foundations.Users.Exceptions;
using Glory2Him.Security.Client.Services.Foundations.Users;
using Moq;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Fact]
        public async Task ShouldThrowServiceExceptionOnGetUserIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();
            var serviceException = new Exception();

            var failedUserServiceException =
                new FailedUserServiceException(
                    message: "Failed user service error occurred, please contact support.",
                    innerException: serviceException);

            var expectedUserServiceException =
                new UserServiceException(
                    message: "User service error occurred, please contact support.",
                    innerException: failedUserServiceException);

            var userServiceMock = new Mock<UserService> { CallBase = true };

            userServiceMock.Setup(broker =>
                broker.ValidateOnGetUser(It.IsAny<ClaimsPrincipal>()))
                    .Throws(serviceException);

            // when
            ValueTask<User> getUserTask =
                userServiceMock.Object.GetUserAsync(someClaimsPrincipal);

            UserServiceException actualUserServiceException =
                await Assert.ThrowsAsync<UserServiceException>(
                    getUserTask.AsTask);

            // then
            actualUserServiceException.Should()
                .BeEquivalentTo(expectedUserServiceException);

            userServiceMock.Verify(broker =>
                broker.ValidateOnGetUser(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }
    }
}
