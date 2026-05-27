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
        public async Task ShouldThrowValidationExceptionOnIsUserInRoleIfClaimsPrincipalIsNullAndLogItAsync(
            string roleName)
        {
            // given
            ClaimsPrincipal nullClaimsPrincipal = null;
            string invalidRoleName = roleName;

            InvalidArgumentUserException invalidArgumentUserException = new InvalidArgumentUserException(
                message: "Invalid user argument(s), correct the errors and try again.");

            invalidArgumentUserException.AddData(
                key: nameof(ClaimsPrincipal),
                values: "ClaimsPrincipal is required");

            invalidArgumentUserException.AddData(
                key: "RoleName",
                values: "Text is required");

            var expectedUserValidationException =
                new UserValidationException(
                    message: "User validation errors occurred, please try again.",
                    innerException: invalidArgumentUserException);

            // when
            ValueTask<bool> isUserInRoleTask =
                userService.IsUserInRoleAsync(nullClaimsPrincipal, invalidRoleName);

            UserValidationException actualUserValidationException =
                await Assert.ThrowsAsync<UserValidationException>(isUserInRoleTask.AsTask);

            // then
            actualUserValidationException.Should()
                .BeEquivalentTo(expectedUserValidationException);
        }
    }
}
