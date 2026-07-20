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

using System;
using System.Security.Claims;
using G2H.Security.Client.Models.Foundations.Users.Exceptions;

namespace G2H.Security.Client.Services.Foundations.Users
{
    internal partial class UserService
    {
        virtual internal void ValidateOnGetUser(ClaimsPrincipal claimsPrincipal)
        {
            Validate((Rule: IsInvalid(claimsPrincipal), Parameter: nameof(ClaimsPrincipal)));
        }

        virtual internal void ValidateOnGetUserId(ClaimsPrincipal claimsPrincipal)
        {
            Validate((Rule: IsInvalid(claimsPrincipal), Parameter: nameof(ClaimsPrincipal)));
        }

        virtual internal void ValidateOnIsUserInRole(ClaimsPrincipal claimsPrincipal, string roleName)
        {
            Validate(
                (Rule: IsInvalid(roleName), Parameter: "RoleName"),
                (Rule: IsInvalid(claimsPrincipal), Parameter: nameof(ClaimsPrincipal)));
        }

        virtual internal void ValidateOnUserHasClaimType(ClaimsPrincipal claimsPrincipal, string claimType)
        {
            Validate(
                (Rule: IsInvalid(claimType), Parameter: "Type"),
                (Rule: IsInvalid(claimsPrincipal), Parameter: nameof(ClaimsPrincipal)));
        }

        virtual internal void ValidateOnGetUserClaimValue(ClaimsPrincipal claimsPrincipal, string claimType)
        {
            Validate(
                (Rule: IsInvalid(claimType), Parameter: "Type"),
                (Rule: IsInvalid(claimsPrincipal), Parameter: nameof(ClaimsPrincipal)));
        }

        virtual internal void ValidateOnUserHasClaimType(
            ClaimsPrincipal claimsPrincipal,
            string claimType,
            string claimValue)
        {
            Validate(
                (Rule: IsInvalid(claimType), Parameter: "Type"),
                (Rule: IsInvalid(claimValue), Parameter: "Value"),
                (Rule: IsInvalid(claimsPrincipal), Parameter: nameof(ClaimsPrincipal)));
        }

        virtual internal void ValidateOnIsUserAuthenticated(ClaimsPrincipal claimsPrincipal)
        {
            Validate((Rule: IsInvalid(claimsPrincipal), Parameter: nameof(ClaimsPrincipal)));
        }

        private static dynamic IsInvalid(string? text) => new
        {
            Condition = String.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };

        private static dynamic IsInvalid(ClaimsPrincipal claimsPrincipal) => new
        {
            Condition = claimsPrincipal == null,
            Message = "ClaimsPrincipal is required"
        };

        private static void Validate(params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidArgumentUserException =
                new InvalidArgumentUserException(
                    message: "Invalid user argument(s), correct the errors and try again.");

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidArgumentUserException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidArgumentUserException.ThrowIfContainsErrors();
        }
    }
}
