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
        [InlineData("username", true, null)]
        [InlineData("username", false, null)]
        [InlineData("", false, null)]
        [InlineData("username", true, "User requested deletion")]
        public async Task ShouldApplyRemoveAuditForDynamicObjectAsync(
            string userId, bool isAuthenticated, string? deletionReason)
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
                DeletedBy = string.Empty,
                DeletedWhen = DateTimeOffset.MinValue,
                IsDeleted = false,
                DeletionReason = null,
            };

            var updatedPerson = inputPerson.DeepClone();
            updatedPerson.DeletedBy = securityUserId;
            updatedPerson.IsDeleted = true;
            updatedPerson.DeletionReason = deletionReason;

            var expectedResult = updatedPerson;

            var inputSecurityConfigurations = new SecurityConfigurations
            {
                CreatedByPropertyName = "CreatedBy",
                CreatedByPropertyType = typeof(string),
                CreatedWhenPropertyName = "CreatedWhen",
                CreatedWhenPropertyType = typeof(DateTimeOffset),
                UpdatedByPropertyName = "UpdatedBy",
                UpdatedByPropertyType = typeof(string),
                UpdatedWhenPropertyName = "UpdatedWhen",
                UpdatedWhenPropertyType = typeof(DateTimeOffset),
                DeletedByPropertyName = "DeletedBy",
                DeletedByPropertyType = typeof(string),
                DeletedWhenPropertyName = "DeletedWhen",
                DeletedWhenPropertyType = typeof(DateTimeOffset),
                IsDeletedPropertyName = "IsDeleted",
                IsDeletedPropertyType = typeof(bool),
                DeletionReasonPropertyName = "DeletionReason",
                DeletionReasonPropertyType = typeof(string)
            };

            // When
            var actualResult = await this.securityClient.Audits
                .ApplyRemoveAuditValuesAsync(
                    inputPerson,
                    inputClaimsPrincipal,
                    inputSecurityConfigurations,
                    deletionReason);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult, options =>
                options.Excluding(ctx => ctx.Path == "DeletedWhen"));

            actualResult.DeletedWhen.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ShouldPreserveDeletionReasonAlreadyOnEntityWhenNoneSuppliedAsync()
        {
            // Given
            ClaimsPrincipal randomClaimsPrincipal = CreateRandomClaimsPrincipal(true, "username");
            ClaimsPrincipal inputClaimsPrincipal = randomClaimsPrincipal;
            string existingDeletionReason = GetRandomString();

            var inputPerson = new Person
            {
                Name = GetRandomString(),
                CreatedBy = GetRandomString(),
                CreatedWhen = DateTimeOffset.UtcNow.AddMinutes(-1),
                UpdatedBy = GetRandomString(),
                UpdatedWhen = DateTimeOffset.UtcNow.AddMinutes(-1),
                DeletedBy = string.Empty,
                DeletedWhen = DateTimeOffset.MinValue,
                IsDeleted = false,
                DeletionReason = existingDeletionReason,
            };

            var updatedPerson = inputPerson.DeepClone();
            updatedPerson.DeletedBy = "username";
            updatedPerson.IsDeleted = true;
            updatedPerson.DeletionReason = existingDeletionReason;

            var expectedResult = updatedPerson;

            var inputSecurityConfigurations = new SecurityConfigurations
            {
                CreatedByPropertyName = "CreatedBy",
                CreatedByPropertyType = typeof(string),
                CreatedWhenPropertyName = "CreatedWhen",
                CreatedWhenPropertyType = typeof(DateTimeOffset),
                UpdatedByPropertyName = "UpdatedBy",
                UpdatedByPropertyType = typeof(string),
                UpdatedWhenPropertyName = "UpdatedWhen",
                UpdatedWhenPropertyType = typeof(DateTimeOffset),
                DeletedByPropertyName = "DeletedBy",
                DeletedByPropertyType = typeof(string),
                DeletedWhenPropertyName = "DeletedWhen",
                DeletedWhenPropertyType = typeof(DateTimeOffset),
                IsDeletedPropertyName = "IsDeleted",
                IsDeletedPropertyType = typeof(bool),
                DeletionReasonPropertyName = "DeletionReason",
                DeletionReasonPropertyType = typeof(string)
            };

            // When: the reason rides on the entity and no reason argument is supplied
            var actualResult = await this.securityClient.Audits
                .ApplyRemoveAuditValuesAsync(
                    inputPerson,
                    inputClaimsPrincipal,
                    inputSecurityConfigurations);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult, options =>
                options.Excluding(ctx => ctx.Path == "DeletedWhen"));

            actualResult.DeletionReason.Should().Be(existingDeletionReason);
            actualResult.DeletedWhen.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }
    }
}
