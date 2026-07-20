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
                UpdatedWhen = currentDateTime,
                DeletedBy = GetRandomString(),
                DeletedWhen = currentDateTime,
                IsDeleted = true,
                DeletionReason = GetRandomString()
            };

            Person storagePerson = new Person
            {
                Name = GetRandomString(),
                CreatedBy = GetRandomString(),
                CreatedWhen = currentDateTime.AddDays(-1),
                UpdatedBy = GetRandomString(),
                UpdatedWhen = currentDateTime.AddDays(-1),
                DeletedBy = null,
                DeletedWhen = DateTimeOffset.MinValue,
                IsDeleted = false,
                DeletionReason = null
            };

            Person updatedPerson = new Person
            {
                Name = inputPerson.Name,
                CreatedBy = storagePerson.CreatedBy,
                CreatedWhen = storagePerson.CreatedWhen,
                UpdatedBy = inputPerson.UpdatedBy,
                UpdatedWhen = inputPerson.UpdatedWhen,
                DeletedBy = storagePerson.DeletedBy,
                DeletedWhen = storagePerson.DeletedWhen,
                IsDeleted = storagePerson.IsDeleted,
                DeletionReason = storagePerson.DeletionReason
            };

            Person expectedResult = updatedPerson.DeepClone();

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
            Person actualResult = await this.securityClient.Audits
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    inputPerson,
                    storagePerson,
                    inputSecurityConfigurations);

            // Then
            actualResult.Should().BeEquivalentTo(expectedResult);
        }
    }
}
