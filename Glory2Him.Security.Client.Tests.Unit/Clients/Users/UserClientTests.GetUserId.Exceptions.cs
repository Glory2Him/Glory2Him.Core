// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients.Users.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Security.Client.Tests.Unit.Clients.Users
{
    public partial class UserClientTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldThrowValidationExceptionOnGetUserIdIfValidationErrorOccursAsync(
            Xeption validationException)
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();

            var expectedUserClientValidationException =
                new UserClientValidationException(
                    message: "User client validation error occurred, fix errors and try again.",
                    innerException: validationException.InnerException as Xeption,
                    data: validationException.InnerException.Data);

            userServiceMock.Setup(service =>
                service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                    .Throws(validationException);

            // when
            ValueTask<string> getUserIdTask =
                userClient.GetUserIdAsync(someClaimsPrincipal);

            UserClientValidationException actualUserClientValidationException =
                await Assert.ThrowsAsync<UserClientValidationException>(
                    getUserIdTask.AsTask);

            // then
            actualUserClientValidationException.Should()
                .BeEquivalentTo(expectedUserClientValidationException);

            userServiceMock.Verify(service =>
                service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnGetUserIdIfDependencyErrorOccursAsync(
            Xeption dependencyException)
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();

            var expectedUserClientDependencyException =
                new UserClientDependencyException(
                    message: "User client dependency error occurred, please contact support.",
                    innerException: dependencyException.InnerException as Xeption,
                    data: dependencyException.InnerException.Data);

            userServiceMock.Setup(service =>
                service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                    .Throws(dependencyException);

            // when
            ValueTask<string> getUserIdTask =
                userClient.GetUserIdAsync(someClaimsPrincipal);

            UserClientDependencyException actualUserClientDependencyException =
                await Assert.ThrowsAsync<UserClientDependencyException>(getUserIdTask.AsTask);

            // then
            actualUserClientDependencyException.Should()
                .BeEquivalentTo(expectedUserClientDependencyException);

            userServiceMock.Verify(service =>
                service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }


        [Fact]
        public async Task ShouldThrowServiceExceptionOnGetUserIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();
            var serviceException = new Exception(message: GetRandomString());

            var expectedUserClientServiceException =
                new UserClientServiceException(
                    message: "User client service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            userServiceMock.Setup(service =>
                service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                    .Throws(serviceException);

            // when
            ValueTask<string> getUserIdTask =
                userClient.GetUserIdAsync(someClaimsPrincipal);

            UserClientServiceException actualUserClientServiceException =
                await Assert.ThrowsAsync<UserClientServiceException>(
                    getUserIdTask.AsTask);

            // then
            actualUserClientServiceException.Should()
                .BeEquivalentTo(expectedUserClientServiceException);

            userServiceMock.Verify(service =>
                service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }
    }
}
