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
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Models.Foundations.Audits.Exceptions;
using G2H.Security.Client.Tests.Unit.Models;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Audits
{
    public partial class AuditServiceTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnApplyAddAuditIfNullObjectsFoundAsync(
            string? invalidInput)
        {
            // given
            Person? nullPerson = null;
            string? invalidUserId = invalidInput;
            SecurityConfigurations? nullSecurityConfigurations = null;

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
            ValueTask<Person?> applyAddAuditTask =
                auditService.ApplyAddAuditValuesAsync(nullPerson, invalidUserId!, nullSecurityConfigurations!);

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
            string? invalidInput)
        {
            // given
            Person inputPerson = new Person();
            string inputUserId = GetRandomString();
            SecurityConfigurations invalidSecurityConfigurations = new SecurityConfigurations
            {
                CreatedByPropertyName = invalidInput!,
                CreatedByPropertyType = typeof(DateTime),
                CreatedWhenPropertyName = invalidInput!,
                CreatedWhenPropertyType = typeof(string),
                UpdatedByPropertyName = invalidInput!,
                UpdatedByPropertyType = typeof(DateTimeOffset),
                UpdatedWhenPropertyName = invalidInput!,
                UpdatedWhenPropertyType = typeof(string)
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
                key: nameof(SecurityConfigurations.CreatedWhenPropertyName),
                values: "Text is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.CreatedWhenPropertyType),
                values: "A type of DateTime / DateTimeOffset is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedByPropertyName),
                values: "Text is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedByPropertyType),
                values: "A type of String / Guid / Long is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedWhenPropertyName),
                values: "Text is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedWhenPropertyType),
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
                CreatedWhenPropertyName = "CreatedAt",
                CreatedWhenPropertyType = typeof(DateTime),
                UpdatedByPropertyName = "UpdatedByUser",
                UpdatedByPropertyType = typeof(string),
                UpdatedWhenPropertyName = "UpdatedAt",
                UpdatedWhenPropertyType = typeof(DateTime)
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
                key: nameof(SecurityConfigurations.CreatedWhenPropertyName),
                values:
                    $"Property '{inputSecurityConfigurations.CreatedWhenPropertyName}' not found, " +
                    $"not settable, or not assignable from " +
                    $"'{inputSecurityConfigurations.CreatedWhenPropertyType.Name}' " +
                    $"on entity '{typeof(Person).Name}'.");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedByPropertyName),
                values:
                    $"Property '{inputSecurityConfigurations.UpdatedByPropertyName}' not found, " +
                    $"not settable, or not assignable from " +
                    $"'{inputSecurityConfigurations.UpdatedByPropertyType.Name}' " +
                    $"on entity '{typeof(Person).Name}'.");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.UpdatedWhenPropertyName),
                values:
                    $"Property '{inputSecurityConfigurations.UpdatedWhenPropertyName}' not found, " +
                    $"not settable, or not assignable from " +
                    $"'{inputSecurityConfigurations.UpdatedWhenPropertyType.Name}' " +
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
