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
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Tests.Acceptance.Models;

namespace G2H.Security.Client.Tests.Clients.Audits
{
    public partial class AuditClientTests
    {
        [Fact]
        public async Task ShouldEnsureAddAuditValuesRemainsUnchangedOnModifyAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;

            Person inputPerson = new Person
            {
                Name = GetRandomString(),
                CreatedBy = GetRandomString(),
                CreatedWhen = currentDateTime,
                UpdatedBy = GetRandomString(),
                UpdatedWhen = currentDateTime
            };

            Person storagePerson = new Person
            {
                Name = GetRandomString(),
                CreatedBy = GetRandomString(),
                CreatedWhen = currentDateTime.AddDays(-1),
                UpdatedBy = GetRandomString(),
                UpdatedWhen = currentDateTime.AddDays(-1)
            };

            Person updatedPerson = new Person
            {
                Name = inputPerson.Name,
                CreatedBy = storagePerson.CreatedBy,
                CreatedWhen = storagePerson.CreatedWhen,
                UpdatedBy = inputPerson.UpdatedBy,
                UpdatedWhen = inputPerson.UpdatedWhen
            };

            Person expectedResult = updatedPerson.DeepClone();

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
            Person actualResult = await this.securityClient.Audits
                .EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    inputPerson,
                    storagePerson,
                    inputSecurityConfigurations);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult);
        }
    }
}