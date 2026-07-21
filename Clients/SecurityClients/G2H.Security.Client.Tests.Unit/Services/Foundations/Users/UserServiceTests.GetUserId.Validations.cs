// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Users.Exceptions;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Users
{
    public partial class UserServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnGetUserIdIfClaimsPrincipalIsNullAndLogItAsync()
        {
            // given
            ClaimsPrincipal? nullClaimsPrincipal = null;

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
                userService.GetUserIdAsync(nullClaimsPrincipal!);

            UserValidationException actualUserValidationException =
                await Assert.ThrowsAsync<UserValidationException>(getUserIdTask.AsTask);

            // then
            actualUserValidationException.Should()
                .BeEquivalentTo(expectedUserValidationException);
        }
    }
}
