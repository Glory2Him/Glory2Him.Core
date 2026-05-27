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
        public async Task ShouldApplyAddAuditForDynamicObjectAsync(string userId, bool isAuthenticated)
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal(isAuthenticated, userId);
            ClaimsPrincipal inputClaimsPrincipal = randomClaimsPrincipal;

            string securityUserId = isAuthenticated
                ? userId
                : string.IsNullOrEmpty(userId)
                    ? "anonymous" : userId;

            var inputPerson = new Person { Name = GetRandomString() };
            var updatedPerson = inputPerson.DeepClone();
            updatedPerson.CreatedBy = securityUserId;
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
                .ApplyAddAuditValuesAsync(inputPerson, inputClaimsPrincipal, inputSecurityConfigurations);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult, options =>
                options.Excluding(ctx =>
                    ctx.Path == "CreatedWhen" || ctx.Path == "UpdatedWhen"));

            actualResult.CreatedWhen.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
            actualResult.UpdatedWhen.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }
    }
}