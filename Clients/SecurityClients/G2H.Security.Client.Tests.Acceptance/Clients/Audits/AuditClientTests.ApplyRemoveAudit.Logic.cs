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
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Tests.Acceptance.Models;

namespace G2H.Security.Client.Tests.Clients.Audits
{
    public partial class AuditClientTests
    {
        [Theory]
        [InlineData("username", true)]
        [InlineData("username", false)]
        [InlineData("", false)]
        public async Task ShouldApplyRemoveAuditForDynamicObjectAsync(string userId, bool isAuthenticated)
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal(isAuthenticated, userId);
            ClaimsPrincipal inputClaimsPrincipal = randomClaimsPrincipal;

            string securityUserId = isAuthenticated
                ? userId
                : string.IsNullOrEmpty(userId)
                    ? "anonymous" : userId;

            var inputPerson = new Person
            {
                Name = GetRandomString(),
                CreatedBy = GetRandomString(),
                CreatedWhen = DateTimeOffset.UtcNow.AddMinutes(-1),
                UpdatedBy = GetRandomString(),
                UpdatedWhen = DateTimeOffset.UtcNow.AddMinutes(-1),
                DeletedBy = null,
                DeletedWhen = DateTimeOffset.MinValue
            };

            var updatedPerson = inputPerson.DeepClone();
            updatedPerson.DeletedBy = securityUserId;

            var expectedResult = updatedPerson;

            var inputSecurityConfigurations = new SecurityConfigurations
            {
                DeletedByPropertyName = "DeletedBy",
                DeletedByPropertyType = typeof(string),
                DeletedWhenPropertyName = "DeletedWhen",
                DeletedWhenPropertyType = typeof(DateTimeOffset)
            };

            // When
            var actualResult = await this.securityClient.Audits
                .ApplyRemoveAuditValuesAsync(inputPerson, inputClaimsPrincipal, inputSecurityConfigurations);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult, options =>
                options.Excluding(ctx => ctx.Path == "DeletedWhen"));

            actualResult.DeletedWhen.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }
    }
}