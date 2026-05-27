// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Foundations.Users.Exceptions;
using Glory2Him.Security.Client.Services.Foundations.Users;
using Moq;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Fact]
        public async Task ShouldThrowServiceExceptionOnUserHasClaimTypeIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();
            string someClaimType = GetRandomString();
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
                broker.ValidateOnUserHasClaimType(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
                    .Throws(serviceException);

            // when
            ValueTask<bool> userHasClaimTypeTask =
                userServiceMock.Object.UserHasClaimAsync(someClaimsPrincipal, someClaimType);

            UserServiceException actualUserServiceException =
                await Assert.ThrowsAsync<UserServiceException>(
                    userHasClaimTypeTask.AsTask);

            // then
            actualUserServiceException.Should()
                .BeEquivalentTo(expectedUserServiceException);

            userServiceMock.Verify(broker =>
                broker.ValidateOnUserHasClaimType(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnUserHasClaimTypeWithValueIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();
            string someClaimType = GetRandomString();
            string someClaimValue = GetRandomString();
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
                broker.ValidateOnUserHasClaimType(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Throws(serviceException);

            // when
            ValueTask<bool> userHasClaimTypeTask =
                userServiceMock.Object.UserHasClaimAsync(someClaimsPrincipal, someClaimType, someClaimValue);

            UserServiceException actualUserServiceException =
                await Assert.ThrowsAsync<UserServiceException>(
                    userHasClaimTypeTask.AsTask);

            // then
            actualUserServiceException.Should()
                .BeEquivalentTo(expectedUserServiceException);

            userServiceMock.Verify(broker =>
                broker.ValidateOnUserHasClaimType(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }
    }
}
