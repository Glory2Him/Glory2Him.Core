// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Models.Foundations.Audits.Exceptions;
using Glory2Him.Security.Client.Tests.Unit.Models;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Foundations.Audits
{
    public partial class AuditServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnEnsureAddAuditValuesIfNullsFoundAsync()
        {
            // given
            Person nullInputPerson = null;
            Person nullStoragePerson = null;
            SecurityConfigurations nullSecurityConfigurations = null;

            InvalidArgumentAuditException invalidArgumentAuditException = new InvalidArgumentAuditException(
                message: "Invalid audit argument(s), correct the errors and try again.");

            invalidArgumentAuditException.AddData(
                key: "entity",
                values: "Entity is required");

            invalidArgumentAuditException.AddData(
                key: "storageEntity",
                values: "Entity is required");

            invalidArgumentAuditException.AddData(
                key: nameof(SecurityConfigurations),
                values: "Entity is required");

            var expectedAuditValidationException =
                new AuditValidationException(
                    message: "Audit validation errors occurred, please try again.",
                    innerException: invalidArgumentAuditException);

            // when
            ValueTask<Person> applyAddAuditTask =
                auditService.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    nullInputPerson,
                    nullStoragePerson,
                    nullSecurityConfigurations);

            AuditValidationException actualAuditValidationException =
                await Assert.ThrowsAsync<AuditValidationException>(applyAddAuditTask.AsTask);

            // then
            actualAuditValidationException.Should()
                .BeEquivalentTo(expectedAuditValidationException);
        }
    }
}