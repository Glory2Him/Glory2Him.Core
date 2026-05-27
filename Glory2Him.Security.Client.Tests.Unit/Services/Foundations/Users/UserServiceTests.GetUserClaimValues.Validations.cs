// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Collections.Generic;
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
        public async Task ShouldThrowValidationExceptionOnGetUserClaimValuesIfClaimsPrincipalAndTypeIsInvalidAsync(
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
            ValueTask<IReadOnlyList<string>> getUserClaimValueAsyncTask =
                userService.GetUserClaimValuesAsync(nullClaimsPrincipal, invalidClaimType);

            UserValidationException actualUserValidationException =
                await Assert.ThrowsAsync<UserValidationException>(getUserClaimValueAsyncTask.AsTask);

            // then
            actualUserValidationException.Should()
                .BeEquivalentTo(expectedUserValidationException);
        }
    }
}
