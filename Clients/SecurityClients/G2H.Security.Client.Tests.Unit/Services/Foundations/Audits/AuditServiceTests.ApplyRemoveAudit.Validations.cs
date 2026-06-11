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
        public async Task ShouldThrowValidationExceptionOnApplyRemoveAuditIfNullObjectsFoundAsync(
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
            ValueTask<Person> applyRemoveAuditTask =
                auditService.ApplyRemoveAuditValuesAsync(nullPerson, invalidUserId, nullSecurityConfigurations);

            AuditValidationException actualAuditValidationException =
                await Assert.ThrowsAsync<AuditValidationException>(applyRemoveAuditTask.AsTask);

            // then
            actualAuditValidationException.Should()
                .BeEquivalentTo(expectedAuditValidationException);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnApplyRemoveAuditIfConfigurationNotPopulatedFoundAsync(
            string invalidInput)
        {
            // given
            Person inputPerson = new Person();
            string inputUserId = GetRandomString();
            SecurityConfigurations invalidSecurityConfigurations = new SecurityConfigurations
            {
                DeletedByPropertyName = invalidInput,
                DeletedByPropertyType = typeof(DateTime),
                DeletedWhenPropertyName = invalidInput,
                DeletedWhenPropertyType = typeof(string)
            };

            InvalidArgumentAuditException invalidArgumentAuditException = new InvalidArgumentAuditException(
                message: "Invalid audit argument(s), correct the errors and try again.");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.DeletedByPropertyName),
                values: "Text is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.DeletedByPropertyType),
                values: "A type of String / Guid / Long is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.DeletedWhenPropertyName),
                values: "Text is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.DeletedWhenPropertyType),
                values: "A type of DateTime / DateTimeOffset is required");

            var expectedAuditValidationException =
                new AuditValidationException(
                    message: "Audit validation errors occurred, please try again.",
                    innerException: invalidArgumentAuditException);

            // when
            ValueTask<Person> applyRemoveAuditTask =
                auditService.ApplyRemoveAuditValuesAsync(inputPerson, inputUserId, invalidSecurityConfigurations);

            AuditValidationException actualAuditValidationException =
                await Assert.ThrowsAsync<AuditValidationException>(applyRemoveAuditTask.AsTask);

            // then
            actualAuditValidationException.Should()
                .BeEquivalentTo(expectedAuditValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApplyRemoveAuditIfEntityDoesNotHaveAuditPropsAsync()
        {
            // given
            Person inputPerson = new Person();
            string inputUserId = GetRandomString();
            SecurityConfigurations inputSecurityConfigurations = new SecurityConfigurations
            {
                DeletedByPropertyName = "DeletedByUser",
                DeletedByPropertyType = typeof(string),
                DeletedWhenPropertyName = "DeletedAt",
                DeletedWhenPropertyType = typeof(DateTime)
            };

            InvalidArgumentAuditException invalidArgumentAuditException = new InvalidArgumentAuditException(
                message: "Invalid audit argument(s), correct the errors and try again.");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.DeletedByPropertyName),
                values:
                    $"Property '{inputSecurityConfigurations.DeletedByPropertyName}' not found, " +
                    $"not settable, or not assignable from " +
                    $"'{inputSecurityConfigurations.DeletedByPropertyType.Name}' " +
                    $"on entity '{typeof(Person).Name}'.");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations.DeletedWhenPropertyName),
                values:
                    $"Property '{inputSecurityConfigurations.DeletedWhenPropertyName}' not found, " +
                    $"not settable, or not assignable from " +
                    $"'{inputSecurityConfigurations.DeletedWhenPropertyType.Name}' " +
                    $"on entity '{typeof(Person).Name}'.");

            var expectedAuditValidationException =
                new AuditValidationException(
                    message: "Audit validation errors occurred, please try again.",
                    innerException: invalidArgumentAuditException);

            // when
            ValueTask<Person> applyRemoveAuditTask =
                auditService.ApplyRemoveAuditValuesAsync(inputPerson, inputUserId, inputSecurityConfigurations);

            AuditValidationException actualAuditValidationException =
                await Assert.ThrowsAsync<AuditValidationException>(applyRemoveAuditTask.AsTask);

            // then
            actualAuditValidationException.Should()
                .BeEquivalentTo(expectedAuditValidationException);
        }
    }
}