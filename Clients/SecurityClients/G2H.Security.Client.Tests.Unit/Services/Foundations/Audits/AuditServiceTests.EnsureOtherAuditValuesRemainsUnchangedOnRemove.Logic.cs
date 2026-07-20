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
using System.Dynamic;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Tests.Unit.Models;
using Moq;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Audits
{
    public partial class AuditServiceTests
    {
        [Fact]
        public async Task ShouldEnsureOtherAuditValuesRemainsUnchangedOnRemoveForExpandoObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = "CreatedUser";
            string modifiedUserId = "ModifiedUser";

            dynamic inputPerson = new ExpandoObject();
            inputPerson.Name = "John Doe";
            inputPerson.CreatedBy = modifiedUserId;
            inputPerson.CreatedDate = currentDateTime;
            inputPerson.UpdatedBy = modifiedUserId;
            inputPerson.UpdatedDate = currentDateTime;
            inputPerson.DeletedBy = string.Empty;
            inputPerson.DeletedDate = DateTimeOffset.MinValue;
            inputPerson.IsDeleted = false;
            inputPerson.DeletionReason = (string?)null;

            dynamic storagePerson = new ExpandoObject();
            storagePerson.Name = "John Doe";
            storagePerson.CreatedBy = createdUserId;
            storagePerson.CreatedDate = DateTimeOffset.MinValue;
            storagePerson.UpdatedBy = createdUserId;
            storagePerson.UpdatedDate = DateTimeOffset.MinValue;
            storagePerson.DeletedBy = (string?)null;
            storagePerson.DeletedDate = DateTimeOffset.MinValue;
            storagePerson.IsDeleted = false;
            storagePerson.DeletionReason = (string?)null;

            dynamic expectedResult = new ExpandoObject();
            expectedResult.Name = "John Doe";
            expectedResult.CreatedBy = createdUserId;
            expectedResult.CreatedDate = DateTimeOffset.MinValue;
            expectedResult.UpdatedBy = createdUserId;
            expectedResult.UpdatedDate = DateTimeOffset.MinValue;
            expectedResult.DeletedBy = string.Empty;
            expectedResult.DeletedDate = DateTimeOffset.MinValue;
            expectedResult.IsDeleted = false;
            expectedResult.DeletionReason = (string?)null;

            var securityConfigurations = new SecurityConfigurations
            {
                CreatedByPropertyName = "CreatedBy",
                CreatedByPropertyType = typeof(string),
                CreatedWhenPropertyName = "CreatedDate",
                CreatedWhenPropertyType = typeof(DateTimeOffset),
                UpdatedByPropertyName = "UpdatedBy",
                UpdatedByPropertyType = typeof(string),
                UpdatedWhenPropertyName = "UpdatedDate",
                UpdatedWhenPropertyType = typeof(DateTimeOffset),
                DeletedByPropertyName = "DeletedBy",
                DeletedByPropertyType = typeof(string),
                DeletedWhenPropertyName = "DeletedDate",
                DeletedWhenPropertyType = typeof(DateTimeOffset),
                IsDeletedPropertyName = "IsDeleted",
                IsDeletedPropertyType = typeof(bool),
                DeletionReasonPropertyName = "DeletionReason",
                DeletionReasonPropertyType = typeof(string)
            };

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // When
            dynamic actualResult = await this.auditService
                .EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync(inputPerson, storagePerson, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task ShouldEnsureOtherAuditValuesRemainsUnchangedOnRemoveForObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = GetRandomString();
            string modifiedUserId = GetRandomString();

            var inputPerson = new Person
            {
                Name = "John Doe",
                CreatedBy = modifiedUserId,
                CreatedWhen = currentDateTime,
                UpdatedBy = modifiedUserId,
                UpdatedWhen = currentDateTime,
            };

            var storagePerson = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
            };

            var expectedResult = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
            };

            var securityConfigurations = new SecurityConfigurations
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

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // When
            var actualResult = await this.auditService
                .EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync(inputPerson, storagePerson, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }
    }
}
