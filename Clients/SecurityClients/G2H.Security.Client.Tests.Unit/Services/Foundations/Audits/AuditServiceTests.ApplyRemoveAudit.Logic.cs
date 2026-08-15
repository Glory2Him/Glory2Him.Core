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
        public async Task ShouldApplyRemoveAuditForDynamicObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = GetRandomString();
            string deletedUserId = GetRandomString();

            dynamic person = new ExpandoObject();
            person.Name = "John Doe";
            person.CreatedBy = createdUserId;
            person.CreatedDate = DateTimeOffset.MinValue;
            person.UpdatedBy = createdUserId;
            person.UpdatedDate = DateTimeOffset.MinValue;
            person.DeletedBy = string.Empty;
            person.DeletedDate = DateTimeOffset.MinValue;
            person.IsDeleted = false;
            person.DeletionReason = (string?)null;

            dynamic expectedResult = new ExpandoObject();
            expectedResult.Name = "John Doe";
            expectedResult.CreatedBy = createdUserId;
            expectedResult.CreatedDate = DateTimeOffset.MinValue;
            expectedResult.UpdatedBy = createdUserId;
            expectedResult.UpdatedDate = DateTimeOffset.MinValue;
            expectedResult.DeletedBy = deletedUserId;
            expectedResult.DeletedDate = currentDateTime;
            expectedResult.IsDeleted = true;
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
            var actualResult = await this.auditService
                .ApplyRemoveAuditValuesAsync(person, deletedUserId, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task ShouldApplyRemoveAuditWithDeletionReasonForDynamicObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = GetRandomString();
            string deletedUserId = GetRandomString();
            string inputDeletionReason = GetRandomString();

            dynamic person = new ExpandoObject();
            person.Name = "John Doe";
            person.CreatedBy = createdUserId;
            person.CreatedDate = DateTimeOffset.MinValue;
            person.UpdatedBy = createdUserId;
            person.UpdatedDate = DateTimeOffset.MinValue;
            person.DeletedBy = string.Empty;
            person.DeletedDate = DateTimeOffset.MinValue;
            person.IsDeleted = false;
            person.DeletionReason = (string?)null;

            dynamic expectedResult = new ExpandoObject();
            expectedResult.Name = "John Doe";
            expectedResult.CreatedBy = createdUserId;
            expectedResult.CreatedDate = DateTimeOffset.MinValue;
            expectedResult.UpdatedBy = createdUserId;
            expectedResult.UpdatedDate = DateTimeOffset.MinValue;
            expectedResult.DeletedBy = deletedUserId;
            expectedResult.DeletedDate = currentDateTime;
            expectedResult.IsDeleted = true;
            expectedResult.DeletionReason = inputDeletionReason;

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
            var actualResult = await this.auditService
                .ApplyRemoveAuditValuesAsync(person, deletedUserId, securityConfigurations, inputDeletionReason);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task ShouldApplyRemoveAuditForNormalObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = GetRandomString();
            string deletedUserId = GetRandomString();

            var person = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
                DeletedBy = string.Empty,
                DeletedWhen = DateTimeOffset.MinValue,
                IsDeleted = false,
                DeletionReason = null,
            };

            var expectedResult = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
                DeletedBy = deletedUserId,
                DeletedWhen = currentDateTime,
                IsDeleted = true,
                DeletionReason = null,
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

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // When
            var actualResult = await this.auditService
                .ApplyRemoveAuditValuesAsync(person, deletedUserId, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task ShouldApplyRemoveAuditWithDeletionReasonForNormalObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = GetRandomString();
            string deletedUserId = GetRandomString();
            string inputDeletionReason = GetRandomString();

            var person = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
                DeletedBy = string.Empty,
                DeletedWhen = DateTimeOffset.MinValue,
                IsDeleted = false,
                DeletionReason = null,
            };

            var expectedResult = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
                DeletedBy = deletedUserId,
                DeletedWhen = currentDateTime,
                IsDeleted = true,
                DeletionReason = inputDeletionReason,
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

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // When
            var actualResult = await this.auditService
                .ApplyRemoveAuditValuesAsync(person, deletedUserId, securityConfigurations, inputDeletionReason);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task ShouldPreserveExistingDeletionReasonWhenNoneSuppliedForNormalObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = GetRandomString();
            string deletedUserId = GetRandomString();
            string existingDeletionReason = GetRandomString();

            var person = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
                DeletedBy = string.Empty,
                DeletedWhen = DateTimeOffset.MinValue,
                IsDeleted = false,
                DeletionReason = existingDeletionReason,
            };

            var expectedResult = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
                DeletedBy = deletedUserId,
                DeletedWhen = currentDateTime,
                IsDeleted = true,
                DeletionReason = existingDeletionReason,
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

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // When
            var actualResult = await this.auditService
                .ApplyRemoveAuditValuesAsync(person, deletedUserId, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task ShouldPreserveExistingDeletionReasonWhenNoneSuppliedForDynamicObjectAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = GetRandomString();
            string deletedUserId = GetRandomString();
            string existingDeletionReason = GetRandomString();

            dynamic person = new ExpandoObject();
            person.Name = "John Doe";
            person.CreatedBy = createdUserId;
            person.CreatedDate = DateTimeOffset.MinValue;
            person.UpdatedBy = createdUserId;
            person.UpdatedDate = DateTimeOffset.MinValue;
            person.DeletedBy = string.Empty;
            person.DeletedDate = DateTimeOffset.MinValue;
            person.IsDeleted = false;
            person.DeletionReason = existingDeletionReason;

            dynamic expectedResult = new ExpandoObject();
            expectedResult.Name = "John Doe";
            expectedResult.CreatedBy = createdUserId;
            expectedResult.CreatedDate = DateTimeOffset.MinValue;
            expectedResult.UpdatedBy = createdUserId;
            expectedResult.UpdatedDate = DateTimeOffset.MinValue;
            expectedResult.DeletedBy = deletedUserId;
            expectedResult.DeletedDate = currentDateTime;
            expectedResult.IsDeleted = true;
            expectedResult.DeletionReason = existingDeletionReason;

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
            var actualResult = await this.auditService
                .ApplyRemoveAuditValuesAsync(person, deletedUserId, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task ShouldOverwriteExistingDeletionReasonWhenOneIsSuppliedAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string createdUserId = GetRandomString();
            string deletedUserId = GetRandomString();
            string staleDeletionReason = GetRandomString();
            string inputDeletionReason = GetRandomString();

            var person = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
                DeletedBy = string.Empty,
                DeletedWhen = DateTimeOffset.MinValue,
                IsDeleted = false,
                DeletionReason = staleDeletionReason,
            };

            var expectedResult = new Person
            {
                Name = "John Doe",
                CreatedBy = createdUserId,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = createdUserId,
                UpdatedWhen = DateTimeOffset.MinValue,
                DeletedBy = deletedUserId,
                DeletedWhen = currentDateTime,
                IsDeleted = true,
                DeletionReason = inputDeletionReason,
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

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // When
            var actualResult = await this.auditService
                .ApplyRemoveAuditValuesAsync(person, deletedUserId, securityConfigurations, inputDeletionReason);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }
    }
}
