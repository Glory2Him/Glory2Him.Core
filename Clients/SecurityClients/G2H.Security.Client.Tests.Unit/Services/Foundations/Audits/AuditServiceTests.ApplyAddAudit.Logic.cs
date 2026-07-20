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
        public async Task ShouldApplyAddAuditForDynamicObjectAsync()
        {
            // given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string userId = GetRandomString();

            dynamic inputPerson = new ExpandoObject();
            inputPerson.Name = "John Doe";
            inputPerson.CreatedBy = string.Empty;
            inputPerson.CreatedDate = DateTimeOffset.MinValue;
            inputPerson.UpdatedBy = string.Empty;
            inputPerson.UpdatedDate = DateTimeOffset.MinValue;
            inputPerson.DeletedBy = string.Empty;
            inputPerson.DeletedDate = DateTimeOffset.MinValue;
            inputPerson.IsDeleted = false;
            inputPerson.DeletionReason = (string?)null;

            dynamic expectedResult = new ExpandoObject();
            expectedResult.Name = "John Doe";
            expectedResult.CreatedBy = userId;
            expectedResult.CreatedDate = currentDateTime;
            expectedResult.UpdatedBy = userId;
            expectedResult.UpdatedDate = currentDateTime;
            expectedResult.DeletedBy = (string?)null;
            expectedResult.DeletedDate = (object?)null;
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

            // when
            dynamic actualResult = await this.auditService
                .ApplyAddAuditValuesAsync(inputPerson, userId, securityConfigurations);

            // then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task ShouldApplyAddAuditForNormalObjectAsync()
        {
            // given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string userId = GetRandomString();

            var inputPerson = new Person
            {
                Name = "John Doe",
                CreatedBy = string.Empty,
                CreatedWhen = DateTimeOffset.MinValue,
                UpdatedBy = string.Empty,
                UpdatedWhen = DateTimeOffset.MinValue,
                DeletedBy = "tampered",
                DeletedWhen = DateTimeOffset.MaxValue,
                IsDeleted = true,
                DeletionReason = "tampered",
            };

            var expectedResult = new Person
            {
                Name = "John Doe",
                CreatedBy = userId,
                CreatedWhen = currentDateTime,
                UpdatedBy = userId,
                UpdatedWhen = currentDateTime,
                DeletedBy = null,
                DeletedWhen = null,
                IsDeleted = false,
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

            // when
            var actualResult = await this.auditService
                .ApplyAddAuditValuesAsync(inputPerson, userId, securityConfigurations);

            // then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task ShouldApplyAddAuditForDynamicObjectWithoutSoftDeleteAsync()
        {
            // given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            string userId = GetRandomString();

            dynamic inputPerson = new ExpandoObject();
            inputPerson.Name = "John Doe";
            inputPerson.CreatedBy = string.Empty;
            inputPerson.CreatedDate = DateTimeOffset.MinValue;
            inputPerson.UpdatedBy = string.Empty;
            inputPerson.UpdatedDate = DateTimeOffset.MinValue;

            dynamic expectedResult = new ExpandoObject();
            expectedResult.Name = "John Doe";
            expectedResult.CreatedBy = userId;
            expectedResult.CreatedDate = currentDateTime;
            expectedResult.UpdatedBy = userId;
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

            // when
            dynamic actualResult = await this.auditService
                .ApplyAddAuditValuesAsync(inputPerson, userId, securityConfigurations);

            // then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }
    }
}
