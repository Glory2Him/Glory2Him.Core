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
        public async Task ShouldApplyModifyAuditForDynamicObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = GetRandomString();
            string modifiedUserId = GetRandomString();

            dynamic person = new ExpandoObject();
            person.Name = "John Doe";
            person.CreatedBy = createdUserId;
            person.CreatedDate = DateTimeOffset.MinValue;
            person.UpdatedBy = createdUserId;
            person.UpdatedDate = DateTimeOffset.MinValue;

            dynamic expectedResult = new ExpandoObject();
            expectedResult.Name = "John Doe";
            expectedResult.CreatedBy = createdUserId;
            expectedResult.CreatedDate = DateTimeOffset.MinValue;
            expectedResult.UpdatedBy = modifiedUserId;
            expectedResult.UpdatedDate = currentDateTime;

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

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // When
            var actualResult = await this.auditService
                .ApplyModifyAuditValuesAsync(person, modifiedUserId, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task ShouldApplyModifyAuditForNormalObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = GetRandomString();
            string modifiedUserId = GetRandomString();

            var person = new Person
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
                UpdatedBy = modifiedUserId,
                UpdatedWhen = currentDateTime,
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
                .ApplyModifyAuditValuesAsync(person, modifiedUserId, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }
    }
}