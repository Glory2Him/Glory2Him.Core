// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Collections.Generic;
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
        public async Task ShouldThrowValidationExceptionOnGetUserClaimValuesIfValidationErrorOccursAsync(
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
                service.GetUserClaimValuesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
                    .Throws(validationException);

            // when
            ValueTask<IReadOnlyList<string>> getUserClaimValuesTask =
                userClient.GetUserClaimValuesAsync(someClaimsPrincipal, someClaimType);

            UserClientValidationException actualUserClientValidationException =
                await Assert.ThrowsAsync<UserClientValidationException>(
                    getUserClaimValuesTask.AsTask);

            // then
            actualUserClientValidationException.Should()
                .BeEquivalentTo(expectedUserClientValidationException);

            userServiceMock.Verify(service =>
                service.GetUserClaimValuesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnGetUserClaimValuesIfDependencyErrorOccursAsync(
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
                service.GetUserClaimValuesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
                    .Throws(dependencyException);

            // when
            ValueTask<IReadOnlyList<string>> getUserClaimValuesTask =
                userClient.GetUserClaimValuesAsync(someClaimsPrincipal, someClaimType);

            UserClientDependencyException actualUserClientDependencyException =
                await Assert.ThrowsAsync<UserClientDependencyException>(getUserClaimValuesTask.AsTask);

            // then
            actualUserClientDependencyException.Should()
                .BeEquivalentTo(expectedUserClientDependencyException);

            userServiceMock.Verify(service =>
                service.GetUserClaimValuesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnGetUserClaimValuesIfServiceErrorOccursAndLogItAsync()
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
                service.GetUserClaimValuesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
                    .Throws(serviceException);

            // when
            ValueTask<IReadOnlyList<string>> getUserClaimValuesTask =
                userClient.GetUserClaimValuesAsync(someClaimsPrincipal, someClaimType);

            UserClientServiceException actualUserClientServiceException =
                await Assert.ThrowsAsync<UserClientServiceException>(
                    getUserClaimValuesTask.AsTask);

            // then
            actualUserClientServiceException.Should()
                .BeEquivalentTo(expectedUserClientServiceException);

            userServiceMock.Verify(service =>
                service.GetUserClaimValuesAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()),
                    Times.Once);

            userServiceMock.VerifyNoOtherCalls();
        }
    }
}
