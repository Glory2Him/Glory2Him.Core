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

using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Models.Orchestrations.Audits.Exceptions;
using G2H.Security.Client.Tests.Unit.Models;

namespace G2H.Security.Client.Tests.Unit.Services.Orchestrations.Audits
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