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
        public async Task ShouldThrowValidationExceptionOnUserHasClaimTypeIfValidationErrorOccursAsync(
            Xeption validationException)
        {
            // given
            string someClaimType = GetRandomString();
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();

            var expectedUserClientValidationException =
                new UserClientValidationException(
                    message: "User client validation error occurred, fix errors and try again.",
                    innerException: validationException.InnerException as Xeption,
                    data: validationException.InnerException.Data);

            userServiceMock.Setup(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
                    .Throws(validationException);

            // when
            ValueTask<bool> isUserAuthenticatedTask =
                userClient.UserHasClaimAsync(someClaimsPrincipal, someClaimType);

            UserClientValidationException actualUserClientValidationException =
                await Assert.ThrowsAsync<UserClientValidationException>(
                    isUserAuthenticatedTask.AsTask);

            // then
            actualUserClientValidationException.Should()
                .BeEquivalentTo(expectedUserClientValidationException);

            userServiceMock.Verify(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnUserHasClaimTypeIfDependencyErrorOccursAsync(
            Xeption dependencyException)
        {
            // given
            string someClaimType = GetRandomString();
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();

            var expectedUserClientDependencyException =
                new UserClientDependencyException(
                    message: "User client dependency error occurred, please contact support.",
                    innerException: dependencyException.InnerException as Xeption,
                    data: dependencyException.InnerException.Data);

            userServiceMock.Setup(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
                    .Throws(dependencyException);

            // when
            ValueTask<bool> isUserAuthenticatedTask =
                userClient.UserHasClaimAsync(someClaimsPrincipal, someClaimType);

            UserClientDependencyException actualUserClientDependencyException =
                await Assert.ThrowsAsync<UserClientDependencyException>(isUserAuthenticatedTask.AsTask);

            // then
            actualUserClientDependencyException.Should()
                .BeEquivalentTo(expectedUserClientDependencyException);

            userServiceMock.Verify(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnUserHasClaimTypeIfServiceErrorOccursAndLogItAsync()
        {
            // given
            string someClaimType = GetRandomString();
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();
            var serviceException = new Exception(message: GetRandomString());

            var expectedUserClientServiceException =
                new UserClientServiceException(
                    message: "User client service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            userServiceMock.Setup(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
                    .Throws(serviceException);

            // when
            ValueTask<bool> isUserAuthenticatedTask =
                userClient.UserHasClaimAsync(someClaimsPrincipal, someClaimType);

            UserClientServiceException actualUserClientServiceException =
                await Assert.ThrowsAsync<UserClientServiceException>(
                    isUserAuthenticatedTask.AsTask);

            // then
            actualUserClientServiceException.Should()
                .BeEquivalentTo(expectedUserClientServiceException);

            userServiceMock.Verify(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldThrowValidationExceptionOnUserHasClaimTypeAndValueIfValidationErrorOccursAsync(
            Xeption validationException)
        {
            // given
            string someClaimType = GetRandomString();
            string someClaimValue = GetRandomString();
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();

            var expectedUserClientValidationException =
                new UserClientValidationException(
                    message: "User client validation error occurred, fix errors and try again.",
                    innerException: validationException.InnerException as Xeption,
                    data: validationException.InnerException.Data);

            userServiceMock.Setup(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Throws(validationException);

            // when
            ValueTask<bool> isUserAuthenticatedTask =
                userClient.UserHasClaimAsync(someClaimsPrincipal, someClaimType, someClaimValue);

            UserClientValidationException actualUserClientValidationException =
                await Assert.ThrowsAsync<UserClientValidationException>(
                    isUserAuthenticatedTask.AsTask);

            // then
            actualUserClientValidationException.Should()
                .BeEquivalentTo(expectedUserClientValidationException);

            userServiceMock.Verify(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnUserHasClaimTypeAndValueIfDependencyErrorOccursAsync(
            Xeption dependencyException)
        {
            // given
            string someClaimType = GetRandomString();
            string someClaimValue = GetRandomString();
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();

            var expectedUserClientDependencyException =
                new UserClientDependencyException(
                    message: "User client dependency error occurred, please contact support.",
                    innerException: dependencyException.InnerException as Xeption,
                    data: dependencyException.InnerException.Data);

            userServiceMock.Setup(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Throws(dependencyException);

            // when
            ValueTask<bool> isUserAuthenticatedTask =
                userClient.UserHasClaimAsync(someClaimsPrincipal, someClaimType, someClaimValue);

            UserClientDependencyException actualUserClientDependencyException =
                await Assert.ThrowsAsync<UserClientDependencyException>(isUserAuthenticatedTask.AsTask);

            // then
            actualUserClientDependencyException.Should()
                .BeEquivalentTo(expectedUserClientDependencyException);

            userServiceMock.Verify(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }


        [Fact]
        public async Task ShouldThrowServiceExceptionOnUserHasClaimTypeAndValueIfServiceErrorOccursAndLogItAsync()
        {
            // given
            string someClaimType = GetRandomString();
            string someClaimValue = GetRandomString();
            ClaimsPrincipal someClaimsPrincipal = new ClaimsPrincipal();
            var serviceException = new Exception(message: GetRandomString());

            var expectedUserClientServiceException =
                new UserClientServiceException(
                    message: "User client service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            userServiceMock.Setup(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Throws(serviceException);

            // when
            ValueTask<bool> isUserAuthenticatedTask =
                userClient.UserHasClaimAsync(someClaimsPrincipal, someClaimType, someClaimValue);

            UserClientServiceException actualUserClientServiceException =
                await Assert.ThrowsAsync<UserClientServiceException>(
                    isUserAuthenticatedTask.AsTask);

            // then
            actualUserClientServiceException.Should()
                .BeEquivalentTo(expectedUserClientServiceException);

            userServiceMock.Verify(service =>
                service.UserHasClaimAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }
    }
}
