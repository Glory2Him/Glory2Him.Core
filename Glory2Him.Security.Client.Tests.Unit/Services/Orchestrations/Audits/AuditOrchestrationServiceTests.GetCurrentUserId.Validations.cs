// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Security.Claims;
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
        public async Task ShouldThrowValidationExceptionOnGetCurrentUserIdAsync()
        {
            // given
            Person nullPerson = null;
            ClaimsPrincipal nullClaimsPrincipal = null;
            SecurityConfigurations nullSecurityConfigurations = null;

            InvalidArgumentAuditOrchestrationException invalidArgumentAuditException =
                new InvalidArgumentAuditOrchestrationException(
                    message: "Invalid audit orchestration argument(s), correct the errors and try again.");

            invalidArgumentAuditException.AddData(
                key: "claimsPrincipal",
                values: "Claims principal is required");

            var expectedAuditValidationException =
                new AuditOrchestrationValidationException(
                    message: "Audit orchestration validation error occurred, please try again.",
                    innerException: invalidArgumentAuditException);

            // when
            ValueTask<string> task =
                auditOrchestrationService.GetCurrentUserIdAsync(nullClaimsPrincipal);

            AuditOrchestrationValidationException actualAuditValidationException =
                await Assert.ThrowsAsync<AuditOrchestrationValidationException>(task.AsTask);

            // then
            actualAuditValidationException.Should()
                .BeEquivalentTo(expectedAuditValidationException);


            this.userServiceMock.VerifyNoOtherCalls();
            this.auditServiceMock.VerifyNoOtherCalls();
        }
    }
}