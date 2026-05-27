// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Dynamic;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Tests.Unit.Models;
using Moq;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Foundations.Audits
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
                CreatedDatePropertyName = "CreatedDate",
                CreatedDatePropertyType = typeof(DateTimeOffset),
                UpdatedByPropertyName = "UpdatedBy",
                UpdatedByPropertyType = typeof(string),
                UpdatedDatePropertyName = "UpdatedDate",
                UpdatedDatePropertyType = typeof(DateTimeOffset)
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
                CreatedDatePropertyName = "CreatedWhen",
                CreatedDatePropertyType = typeof(DateTimeOffset),
                UpdatedByPropertyName = "UpdatedBy",
                UpdatedByPropertyType = typeof(string),
                UpdatedDatePropertyName = "UpdatedWhen",
                UpdatedDatePropertyType = typeof(DateTimeOffset)
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