// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Tests.Acceptance.Models;

namespace Glory2Him.Security.Client.Tests.Clients.Audits
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
            };

            var updatedPerson = inputPerson.DeepClone();
            updatedPerson.UpdatedBy = securityUserId;

            var expectedResult = updatedPerson;

            var inputSecurityConfigurations = new SecurityConfigurations
            {
                CreatedByPropertyName = "CreatedBy",
                CreatedByPropertyType = typeof(string),
                CreatedDatePropertyName = "CreatedWhen",
                CreatedDatePropertyType = typeof(DateTimeOffset),
                UpdatedByPropertyName = "UpdatedBy",
                UpdatedByPropertyType = typeof(string),
                UpdatedDatePropertyName = "UpdatedWhen",
                UpdatedDatePropertyType = typeof(DateTimeOffset)
            };

            // When
            var actualResult = await this.securityClient.Audits
                .ApplyRemoveAuditValuesAsync(inputPerson, inputClaimsPrincipal, inputSecurityConfigurations);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult, options =>
                options.Excluding(ctx => ctx.Path == "UpdatedWhen"));

            actualResult.UpdatedWhen.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }
    }
}