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
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Tests.Unit.Models;
using Moq;

namespace G2H.Security.Client.Tests.Unit.Services.Orchestrations.Audits
{
    public partial class AuditOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldEnsureOtherAuditValuesRemainsUnchangedOnRemoveForExpandoObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            Person inputPerson = new Person { Name = GetRandomString() };
            Person storagePerson = new Person { Name = GetRandomString() };
            Person updatedPerson = new Person { Name = GetRandomString() };
            Person expectedResult = updatedPerson;

            var securityConfigurations = new SecurityConfigurations
            {
                CreatedByPropertyName = "CreatedBy",
                CreatedByPropertyType = typeof(string),
                CreatedWhenPropertyName = "CreatedDate",
                CreatedWhenPropertyType = typeof(DateTimeOffset),
                UpdatedByPropertyName = "UpdatedBy",
                UpdatedByPropertyType = typeof(string),
                UpdatedWhenPropertyName = "UpdatedDate",
                UpdatedWhenPropertyType = typeof(DateTimeOffset)
            };

            this.auditServiceMock.Setup(service =>
                service.EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync(
                    inputPerson,
                    storagePerson,
                    securityConfigurations))
                .ReturnsAsync(updatedPerson);

            // When
            dynamic actualResult = await this.auditOrchestrationService
                .EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync(
                    inputPerson,
                    storagePerson,
                    securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);

            this.auditServiceMock.Verify(service =>
                service.EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync(
                    inputPerson,
                    storagePerson,
                    securityConfigurations),
                        Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
            this.auditServiceMock.VerifyNoOtherCalls();
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

            var updatedPerson = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
            };

            Person expectedResult = updatedPerson;

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

            this.auditServiceMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync(
                    inputPerson,
                    storagePerson,
                    securityConfigurations))
                        .ReturnsAsync(updatedPerson);

            // When
            var actualResult = await this.auditOrchestrationService
                .EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync(
                    inputPerson,
                    storagePerson,
                    securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);

            this.auditServiceMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync(
                    inputPerson,
                    storagePerson,
                    securityConfigurations),
                        Times.Once);

            this.userServiceMock.VerifyNoOtherCalls();
            this.auditServiceMock.VerifyNoOtherCalls();
        }
    }
}
