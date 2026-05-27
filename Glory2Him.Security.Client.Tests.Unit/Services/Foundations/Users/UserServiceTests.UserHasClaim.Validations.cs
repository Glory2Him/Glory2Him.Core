// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Foundations.Users.Exceptions;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnUserHasClaimTypeIfClaimsPrincipalAndTypeIsInvalidAsync(
            string claimType)
        {
            // given
            ClaimsPrincipal nullClaimsPrincipal = null;
            string invalidClaimType = claimType;

            InvalidArgumentUserException invalidArgumentUserException = new InvalidArgumentUserException(
                message: "Invalid user argument(s), correct the errors and try again.");

            invalidArgumentUserException.AddData(
                key: nameof(ClaimsPrincipal),
                values: "ClaimsPrincipal is required");

            invalidArgumentUserException.AddData(
                key: "Type",
                values: "Text is required");

            var expectedUserValidationException =
                new UserValidationException(
                    message: "User validation errors occurred, please try again.",
                    innerException: invalidArgumentUserException);

            // when
            ValueTask<bool> userHasClaimTypeTask =
                userService.UserHasClaimAsync(nullClaimsPrincipal, invalidClaimType);

            UserValidationException actualUserValidationException =
                await Assert.ThrowsAsync<UserValidationException>(userHasClaimTypeTask.AsTask);

            // then
            actualUserValidationException.Should()
                .BeEquivalentTo(expectedUserValidationException);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnUserHasClaimTypeIfClaimsPrincipalAndTypeAndValueIsInvalidAsync(
            string invalidValue)
        {
            // given
            ClaimsPrincipal nullClaimsPrincipal = null;
            string invalidClaimType = invalidValue;
            string invalidClaimValue = invalidValue;

            InvalidArgumentUserException invalidArgumentUserException = new InvalidArgumentUserException(
                message: "Invalid user argument(s), correct the errors and try again.");

            invalidArgumentUserException.AddData(
                key: nameof(ClaimsPrincipal),
                values: "ClaimsPrincipal is required");

            invalidArgumentUserException.AddData(
                key: "Type",
                values: "Text is required");

            invalidArgumentUserException.AddData(
                key: "Value",
                values: "Text is required");

            var expectedUserValidationException =
                new UserValidationException(
                    message: "User validation errors occurred, please try again.",
                    innerException: invalidArgumentUserException);

            // when
            ValueTask<bool> userHasClaimTypeTask =
                userService.UserHasClaimAsync(nullClaimsPrincipal, invalidClaimType, invalidClaimValue);

            UserValidationException actualUserValidationException =
                await Assert.ThrowsAsync<UserValidationException>(userHasClaimTypeTask.AsTask);

            // then
            actualUserValidationException.Should()
                .BeEquivalentTo(expectedUserValidationException);
        }
    }
}
