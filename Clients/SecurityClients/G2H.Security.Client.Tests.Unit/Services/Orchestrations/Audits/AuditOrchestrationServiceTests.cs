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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Models.Foundations.Audits.Exceptions;
using G2H.Security.Client.Models.Foundations.Users;
using G2H.Security.Client.Models.Foundations.Users.Exceptions;
using G2H.Security.Client.Services.Foundations.Audits;
using G2H.Security.Client.Services.Foundations.Users;
using G2H.Security.Client.Services.Orchestrations.Audits;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace G2H.Security.Client.Tests.Unit.Services.Orchestrations.Audits
{
    public partial class AuditOrchestrationServiceTests
    {
        private readonly Mock<IUserService> userServiceMock;
        private readonly Mock<IAuditService> auditServiceMock;
        private readonly IAuditOrchestrationService auditOrchestrationService;

        public AuditOrchestrationServiceTests()
        {
            this.userServiceMock = new Mock<IUserService>();
            this.auditServiceMock = new Mock<IAuditService>();
            this.auditOrchestrationService = new AuditOrchestrationService(
                userService: userServiceMock.Object,
                auditService: auditServiceMock.Object);
        }

        private static ClaimsPrincipal CreateRandomClaimsPrincipal(bool isAuthenticated = true)
        {
            string securityOid = Guid.NewGuid().ToString();
            string givenName = GetRandomString();
            string surname = GetRandomString();
            string displayName = GetRandomString();
            string email = GetRandomString();
            string jobTitle = GetRandomString();

            List<Claim> claims = new List<Claim>
            {
                new Claim("oid", securityOid),
                new Claim(ClaimTypes.GivenName, GetRandomString()),
                new Claim(ClaimTypes.Surname, GetRandomString()),
                new Claim("displayName", GetRandomString()),
                new Claim(ClaimTypes.Email, GetRandomString()),
                new Claim("jobTitle", GetRandomString()),
                new Claim(ClaimTypes.Name, GetRandomString()),
                new Claim(ClaimTypes.Role, "Users")
            };

            string? authenticationType = isAuthenticated ? "TestScheme" : null;
            var identity = new ClaimsIdentity(claims, authenticationType);
            var principal = new ClaimsPrincipal(identity);

            return principal;
        }

        private User GetUser(ClaimsPrincipal claimsPrincipal)
        {
            return new User(
                userId: claimsPrincipal.FindFirst("oid")?.Value!,
                givenName: claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value!,
                surname: claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value!,
                displayName: claimsPrincipal.FindFirst("displayName")?.Value!,
                email: claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value!,
                jobTitle: claimsPrincipal.FindFirst("jobTitle")?.Value!,
                roles: claimsPrincipal.FindAll(ClaimTypes.Role).Select(role => role.Value).ToList(),
                claims: claimsPrincipal.Claims.ToList());
        }

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        public static TheoryData<Xeption> DependencyValidationExceptions()
        {
            string randomMessage = GetRandomString();
            string exceptionMessage = randomMessage;
            var innerException = new Xeption(exceptionMessage);

            return new TheoryData<Xeption>
            {
                new UserValidationException(
                    message: "User validation errors occurred, please try again",
                    innerException),

                new UserDependencyValidationException(
                    message: "User dependency validation occurred, please try again.",
                    innerException),

                new AuditValidationException(
                    message: "Audit validation errors occurred, please try again.",
                    innerException),

                new AuditDependencyValidationException(
                    message: "Audit dependency validation occurred, please try again.",
                    innerException)
            };
        }

        public static TheoryData<Xeption> DependencyExceptions()
        {
            string randomMessage = GetRandomString();
            string exceptionMessage = randomMessage;
            var innerException = new Xeption(exceptionMessage);

            return new TheoryData<Xeption>
            {
                new UserDependencyException(
                    message: "User dependency error occurred, please contact support.",
                    innerException),

                new UserServiceException(
                    message: "User service error occurred, please contact support.",
                    innerException),

                new AuditDependencyException(
                    message: "Audit dependency error occurred, please contact support.",
                    innerException),

                new AuditServiceException(
                    message: "Audit service error occurred, please contact support.",
                    innerException),
            };
        }

        private static SecurityConfigurations GetSecurityConfigurations()
        {
            return new SecurityConfigurations
            {
                CreatedByPropertyName = "CreatedBy",
                CreatedByPropertyType = typeof(string),
                CreatedWhenPropertyName = "CreatedWhen",
                CreatedWhenPropertyType = typeof(DateTimeOffset),
                UpdatedByPropertyName = "UpdatedBy",
                UpdatedByPropertyType = typeof(string),
                UpdatedWhenPropertyName = "UpdatedWhen",
                UpdatedWhenPropertyType = typeof(DateTimeOffset)
            };
        }
    }
}
