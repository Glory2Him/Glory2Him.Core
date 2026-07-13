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

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Users.Exceptions;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnIsUserInRoleIfClaimsPrincipalIsNullAndLogItAsync(
            string? roleName)
        {
            // given
            ClaimsPrincipal? nullClaimsPrincipal = null;
            string? invalidRoleName = roleName;

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
                userService.IsUserInRoleAsync(nullClaimsPrincipal!, invalidRoleName!);

            UserValidationException actualUserValidationException =
                await Assert.ThrowsAsync<UserValidationException>(isUserInRoleTask.AsTask);

            // then
            actualUserValidationException.Should()
                .BeEquivalentTo(expectedUserValidationException);
        }
    }
}
