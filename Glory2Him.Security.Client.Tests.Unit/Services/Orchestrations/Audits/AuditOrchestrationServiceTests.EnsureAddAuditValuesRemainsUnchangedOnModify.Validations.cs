// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Models.Orchestrations.Audits.Exceptions;
using Glory2Him.Security.Client.Tests.Unit.Models;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Orchestrations.Audits
{
    public partial class AuditOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnEnsureAddAuditValuesIfNullsFoundAsync()
        {
            // given
            Person nullInputPerson = null;
            Person nullStoragePerson = null;
            SecurityConfigurations nullSecurityConfigurations = null;

            InvalidArgumentAuditOrchestrationException invalidArgumentAuditException =
                new InvalidArgumentAuditOrchestrationException(
                message: "Invalid audit orchestration argument(s), correct the errors and try again.");

            invalidArgumentAuditException.AddData(
                key: "entity",
                values: "Entity is required");

            invalidArgumentAuditException.AddData(
                key: "storageEntity",
                values: "Entity is required");

            invalidArgumentAuditException.AddData(
                key: "securityConfigurations",
                values: "Entity is required");

            var expectedAuditOrchestrationValidationException =
                new AuditOrchestrationValidationException(
                    message: "Audit orchestration validation error occurred, please try again.",
                    innerException: invalidArgumentAuditException);

            // when
            ValueTask<Person> task =
                auditOrchestrationService.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    nullInputPerson,
                    nullStoragePerson,
                    nullSecurityConfigurations);

            AuditOrchestrationValidationException actualAuditOrchestrationValidationException =
                await Assert.ThrowsAsync<AuditOrchestrationValidationException>(task.AsTask);

            // then
            actualAuditOrchestrationValidationException.Should()
                .BeEquivalentTo(expectedAuditOrchestrationValidationException);

            this.userServiceMock.VerifyNoOtherCalls();
            this.auditServiceMock.VerifyNoOtherCalls();
        }
    }
}