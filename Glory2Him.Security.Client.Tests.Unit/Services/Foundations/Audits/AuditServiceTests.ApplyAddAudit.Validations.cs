// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Models.Foundations.Audits.Exceptions;
using Glory2Him.Security.Client.Tests.Unit.Models;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Foundations.Audits
{
    public partial class AuditServiceTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnApplyAddAuditIfNullObjectsFoundAsync(
            string invalidInput)
        {
            // given
            Person nullPerson = null;
            string invalidUserId = invalidInput;
            SecurityConfigurations nullSecurityConfigurations = null;

            InvalidArgumentAuditException invalidArgumentAuditException = new InvalidArgumentAuditException(
                message: "Invalid audit argument(s), correct the errors and try again.");

            invalidArgumentAuditException.AddData(
                key: "entity",
                values: "Entity is required");

            invalidArgumentAuditException.AddData(
                key: "userId",
                values: "Text is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations),
                values: "Entity is required");

            var expectedAuditValidationException =
                new AuditValidationException(
                    message: "Audit validation errors occurred, please try again.",
                    innerException: invalidArgumentAuditException);

            // when
            ValueTask<Person> applyAddAuditTask =
                auditService.ApplyAddAuditValuesAsync(nullPerson, invalidUserId, nullSecurityConfigurations);

            AuditValidationException actualAuditValidationException =
                await Assert.ThrowsAsync<AuditValidationException>(applyAddAuditTask.AsTask);

            // then
            actualAuditValidationException.Should()
                .BeEquivalentTo(expectedAuditValidationException);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnApplyAddAuditIfConfigurationNotPopulatedFoundAsync(
            string invalidInput)
        {
            // given
            Person inputPerson = new Person();
            string inputUserId = GetRandomString();
            SecurityConfigurations invalidSecurityConfigurations = new SecurityConfigurations
            {
                CreatedByPropertyName = invalidInput,
                CreatedByPropertyType = typeof(DateTime),
                CreatedDatePropertyName = invalidInput,
                CreatedDatePropertyType = typeof(string),
                UpdatedByPropertyName = invalidInput,
                UpdatedByPropertyType = typeof(DateTimeOffset),
                UpdatedDatePropertyName = invalidInput,
                UpdatedDatePropertyType = typeof(string)
            };

            InvalidArgumentAuditException invalidArgumentAuditException = new InvalidArgumentAuditException(
                message: "Invalid audit argument(s), correct the errors and try again.");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.CreatedByPropertyName),
                values: "Text is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.CreatedByPropertyType),
                values: "A type of String / Guid / Long is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.CreatedDatePropertyName),
                values: "Text is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.CreatedDatePropertyType),
                values: "A type of DateTime / DateTimeOffset is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedByPropertyName),
                values: "Text is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedByPropertyType),
                values: "A type of String / Guid / Long is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedDatePropertyName),
                values: "Text is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedDatePropertyType),
                values: "A type of DateTime / DateTimeOffset is required");

            var expectedAuditValidationException =
                new AuditValidationException(
                    message: "Audit validation errors occurred, please try again.",
                    innerException: invalidArgumentAuditException);

            // when
            ValueTask<Person> applyAddAuditTask =
                auditService.ApplyAddAuditValuesAsync(inputPerson, inputUserId, invalidSecurityConfigurations);

            AuditValidationException actualAuditValidationException =
                await Assert.ThrowsAsync<AuditValidationException>(applyAddAuditTask.AsTask);

            // then
            actualAuditValidationException.Should()
                .BeEquivalentTo(expectedAuditValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApplyAddAuditIfEntityDoesNotHaveAuditPropsAsync()
        {
            // given
            Person inputPerson = new Person();
            string inputUserId = GetRandomString();
            SecurityConfigurations inputSecurityConfigurations = new SecurityConfigurations
            {
                CreatedByPropertyName = "CreatedByUser",
                CreatedByPropertyType = typeof(string),
                CreatedDatePropertyName = "CreatedAt",
                CreatedDatePropertyType = typeof(DateTime),
                UpdatedByPropertyName = "UpdatedByUser",
                UpdatedByPropertyType = typeof(string),
                UpdatedDatePropertyName = "UpdatedAt",
                UpdatedDatePropertyType = typeof(DateTime)
            };

            InvalidArgumentAuditException invalidArgumentAuditException = new InvalidArgumentAuditException(
                message: "Invalid audit argument(s), correct the errors and try again.");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.CreatedByPropertyName),
                values:
                    $"Property '{inputSecurityConfigurations.CreatedByPropertyName}' not found, " +
                    $"not settable, or not assignable from " +
                    $"'{inputSecurityConfigurations.CreatedByPropertyType.Name}' " +
                    $"on entity '{typeof(Person).Name}'.");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.CreatedDatePropertyName),
                values:
                    $"Property '{inputSecurityConfigurations.CreatedDatePropertyName}' not found, " +
                    $"not settable, or not assignable from " +
                    $"'{inputSecurityConfigurations.CreatedDatePropertyType.Name}' " +
                    $"on entity '{typeof(Person).Name}'.");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedByPropertyName),
                values:
                    $"Property '{inputSecurityConfigurations.UpdatedByPropertyName}' not found, " +
                    $"not settable, or not assignable from " +
                    $"'{inputSecurityConfigurations.UpdatedByPropertyType.Name}' " +
                    $"on entity '{typeof(Person).Name}'.");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedDatePropertyName),
                values:
                    $"Property '{inputSecurityConfigurations.UpdatedDatePropertyName}' not found, " +
                    $"not settable, or not assignable from " +
                    $"'{inputSecurityConfigurations.UpdatedDatePropertyType.Name}' " +
                    $"on entity '{typeof(Person).Name}'.");

            var expectedAuditValidationException =
                new AuditValidationException(
                    message: "Audit validation errors occurred, please try again.",
                    innerException: invalidArgumentAuditException);

            // when
            ValueTask<Person> applyAddAuditTask =
                auditService.ApplyAddAuditValuesAsync(inputPerson, inputUserId, inputSecurityConfigurations);

            AuditValidationException actualAuditValidationException =
                await Assert.ThrowsAsync<AuditValidationException>(applyAddAuditTask.AsTask);

            // then
            actualAuditValidationException.Should()
                .BeEquivalentTo(expectedAuditValidationException);
        }
    }
}