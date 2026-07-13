// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Clients.Users.Exceptions;
using Moq;
using Xeptions;

namespace G2H.Security.Client.Tests.Unit.Clients.Users
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
                    innerException: (validationException.InnerException as Xeption)!,
                    data: validationException.InnerException?.Data!);

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
                    innerException: (dependencyException.InnerException as Xeption)!,
                    data: dependencyException.InnerException?.Data!);

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
