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
using G2H.Security.Client.Models.Clients.Users.Exceptions;
using G2H.Security.Client.Models.Foundations.Users;
using G2H.Security.Client.Models.Foundations.Users.Exceptions;
using G2H.Security.Client.Services.Foundations.Users;
using Xeptions;

namespace G2H.Security.Client.Clients.Users
{
    internal class UserClient : IUserClient
    {
        private readonly IUserService userService;

        public UserClient(IUserService userService)
        {
            this.userService = userService;
        }

        public async ValueTask<User> GetUserAsync(ClaimsPrincipal claimsPrincipal)
        {
            try
            {
                return await userService.GetUserAsync(claimsPrincipal);
            }
            catch (UserValidationException userValidationException)
            {
                throw CreateUserClientValidationException(
                    userValidationException.InnerException as Xeption);
            }
            catch (UserDependencyValidationException userDependencyValidationException)
            {
                throw CreateUserClientValidationException(
                    userDependencyValidationException.InnerException as Xeption);
            }
            catch (UserDependencyException userDependencyException)
            {
                throw CreateUserClientDependencyException(
                    userDependencyException.InnerException as Xeption);
            }
            catch (UserServiceException userServiceException)
            {
                throw CreateUserClientDependencyException(
                    userServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateUserClientServiceException(exception);
            }
        }

        public async ValueTask<string> GetUserIdAsync(ClaimsPrincipal claimsPrincipal)
        {
            try
            {
                return await this.userService.GetUserIdAsync(claimsPrincipal);
            }
            catch (UserValidationException userValidationException)
            {
                throw CreateUserClientValidationException(
                    userValidationException.InnerException as Xeption);
            }
            catch (UserDependencyValidationException userDependencyValidationException)
            {
                throw CreateUserClientValidationException(
                    userDependencyValidationException.InnerException as Xeption);
            }
            catch (UserDependencyException userDependencyException)
            {
                throw CreateUserClientDependencyException(
                    userDependencyException.InnerException as Xeption);
            }
            catch (UserServiceException userServiceException)
            {
                throw CreateUserClientDependencyException(
                    userServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateUserClientServiceException(exception);
            }
        }

        public async ValueTask<bool> IsUserAuthenticatedAsync(ClaimsPrincipal claimsPrincipal)
        {
            try
            {
                return await userService.IsUserAuthenticatedAsync(claimsPrincipal);
            }
            catch (UserValidationException userValidationException)
            {
                throw CreateUserClientValidationException(
                    userValidationException.InnerException as Xeption);
            }
            catch (UserDependencyValidationException userDependencyValidationException)
            {
                throw CreateUserClientValidationException(
                    userDependencyValidationException.InnerException as Xeption);
            }
            catch (UserDependencyException userDependencyException)
            {
                throw CreateUserClientDependencyException(
                    userDependencyException.InnerException as Xeption);
            }
            catch (UserServiceException userServiceException)
            {
                throw CreateUserClientDependencyException(
                    userServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateUserClientServiceException(exception);
            }
        }

        public async ValueTask<bool> IsUserInRoleAsync(ClaimsPrincipal claimsPrincipal, string roleName)
        {
            try
            {
                return await userService.IsUserInRoleAsync(claimsPrincipal, roleName);
            }
            catch (UserValidationException userValidationException)
            {
                throw CreateUserClientValidationException(
                    userValidationException.InnerException as Xeption);
            }
            catch (UserDependencyValidationException userDependencyValidationException)
            {
                throw CreateUserClientValidationException(
                    userDependencyValidationException.InnerException as Xeption);
            }
            catch (UserDependencyException userDependencyException)
            {
                throw CreateUserClientDependencyException(
                    userDependencyException.InnerException as Xeption);
            }
            catch (UserServiceException userServiceException)
            {
                throw CreateUserClientDependencyException(
                    userServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateUserClientServiceException(exception);
            }
        }

        public async ValueTask<bool> UserHasClaimAsync(
            ClaimsPrincipal claimsPrincipal,
            string type,
            string value)
        {
            try
            {
                return await userService.UserHasClaimAsync(claimsPrincipal, type, value);
            }
            catch (UserValidationException userValidationException)
            {
                throw CreateUserClientValidationException(
                    userValidationException.InnerException as Xeption);
            }
            catch (UserDependencyValidationException userDependencyValidationException)
            {
                throw CreateUserClientValidationException(
                    userDependencyValidationException.InnerException as Xeption);
            }
            catch (UserDependencyException userDependencyException)
            {
                throw CreateUserClientDependencyException(
                    userDependencyException.InnerException as Xeption);
            }
            catch (UserServiceException userServiceException)
            {
                throw CreateUserClientDependencyException(
                    userServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateUserClientServiceException(exception);
            }
        }

        public async ValueTask<bool> UserHasClaimAsync(ClaimsPrincipal claimsPrincipal, string type)
        {
            try
            {
                return await userService.UserHasClaimAsync(claimsPrincipal, type);
            }
            catch (UserValidationException userValidationException)
            {
                throw CreateUserClientValidationException(
                    userValidationException.InnerException as Xeption);
            }
            catch (UserDependencyValidationException userDependencyValidationException)
            {
                throw CreateUserClientValidationException(
                    userDependencyValidationException.InnerException as Xeption);
            }
            catch (UserDependencyException userDependencyException)
            {
                throw CreateUserClientDependencyException(
                    userDependencyException.InnerException as Xeption);
            }
            catch (UserServiceException userServiceException)
            {
                throw CreateUserClientDependencyException(
                    userServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateUserClientServiceException(exception);
            }
        }

        public async ValueTask<string> GetUserClaimValueAsync(ClaimsPrincipal claimsPrincipal, string type)
        {
            try
            {
                return await userService.GetUserClaimValueAsync(claimsPrincipal, type);
            }
            catch (UserValidationException userValidationException)
            {
                throw CreateUserClientValidationException(
                    userValidationException.InnerException as Xeption);
            }
            catch (UserDependencyValidationException userDependencyValidationException)
            {
                throw CreateUserClientValidationException(
                    userDependencyValidationException.InnerException as Xeption);
            }
            catch (UserDependencyException userDependencyException)
            {
                throw CreateUserClientDependencyException(
                    userDependencyException.InnerException as Xeption);
            }
            catch (UserServiceException userServiceException)
            {
                throw CreateUserClientDependencyException(
                    userServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateUserClientServiceException(exception);
            }
        }

        public async ValueTask<IReadOnlyList<string>> GetUserClaimValuesAsync(
            ClaimsPrincipal claimsPrincipal,
            string type)
        {
            try
            {
                return await userService.GetUserClaimValuesAsync(claimsPrincipal, type);
            }
            catch (UserValidationException userValidationException)
            {
                throw CreateUserClientValidationException(
                    userValidationException.InnerException as Xeption);
            }
            catch (UserDependencyValidationException userDependencyValidationException)
            {
                throw CreateUserClientValidationException(
                    userDependencyValidationException.InnerException as Xeption);
            }
            catch (UserDependencyException userDependencyException)
            {
                throw CreateUserClientDependencyException(
                    userDependencyException.InnerException as Xeption);
            }
            catch (UserServiceException userServiceException)
            {
                throw CreateUserClientDependencyException(
                    userServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateUserClientServiceException(exception);
            }
        }

        private static UserClientValidationException CreateUserClientValidationException(Xeption innerException)
        {
            return new UserClientValidationException(
                message: "User client validation error occurred, fix errors and try again.",
                innerException,
                data: innerException.Data);
        }

        private static UserClientDependencyException CreateUserClientDependencyException(Xeption innerException)
        {
            return new UserClientDependencyException(
                message: "User client dependency error occurred, please contact support.",
                innerException,
                data: innerException.Data);
        }

        private static UserClientServiceException CreateUserClientServiceException(Exception innerException)
        {
            return new UserClientServiceException(
                message: "User client service error occurred, please contact support.",
                innerException,
                data: innerException.Data);
        }
    }
}
