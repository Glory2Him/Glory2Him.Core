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
        [Fact]
        public async Task ShouldThrowValidationExceptionOnGetUserIdIfClaimsPrincipalIsNullAndLogItAsync()
        {
            // given
            ClaimsPrincipal nullClaimsPrincipal = null;

            InvalidArgumentUserException invalidArgumentUserException = new InvalidArgumentUserException(
                message: "Invalid user argument(s), correct the errors and try again.");

            invalidArgumentUserException.AddData(
                key: nameof(ClaimsPrincipal),
                values: "ClaimsPrincipal is required");

            var expectedUserValidationException =
                new UserValidationException(
                    message: "User validation errors occurred, please try again.",
                    innerException: invalidArgumentUserException);

            // when
            ValueTask<string> getUserIdTask =
                userService.GetUserIdAsync(nullClaimsPrincipal);

            UserValidationException actualUserValidationException =
                await Assert.ThrowsAsync<UserValidationException>(getUserIdTask.AsTask);

            // then
            actualUserValidationException.Should()
                .BeEquivalentTo(expectedUserValidationException);
        }
    }
}
