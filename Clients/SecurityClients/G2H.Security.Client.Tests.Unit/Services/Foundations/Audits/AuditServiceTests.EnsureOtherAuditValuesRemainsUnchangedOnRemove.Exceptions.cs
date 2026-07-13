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
using System.Dynamic;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Models.Foundations.Audits.Exceptions;
using G2H.Security.Client.Services.Foundations.Audits;
using Moq;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Audits
{
    public partial class AuditServiceTests
    {
        [Fact]
        public async Task ShouldThrowServiceExceptionOnEnsureOtherAuditValuesRemainsUnchangedOnRemoveIfServiceErrorOccursAsync()
        {
            // given
            dynamic someObject = new ExpandoObject();
            someObject.Name = "John Doe";
            someObject.CreatedBy = string.Empty;
            someObject.CreatedDate = DateTimeOffset.MinValue;
            someObject.UpdatedBy = string.Empty;
            someObject.UpdatedDate = DateTimeOffset.MinValue;

            dynamic someStorageObject = new ExpandoObject();
            someStorageObject.Name = "John Doe";
            someStorageObject.CreatedBy = string.Empty;
            someStorageObject.CreatedDate = DateTimeOffset.MinValue;
            someStorageObject.UpdatedBy = string.Empty;
            someStorageObject.UpdatedDate = DateTimeOffset.MinValue;

            var serviceException = new Exception();

            var someSecurityConfigurations = new SecurityConfigurations
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

            var failedAuditServiceException =
                new FailedAuditServiceException(
                    message: "Failed audit service error occurred, please contact support.",
                    innerException: serviceException);

            var expectedAuditServiceException =
                new AuditServiceException(
                    message: "Audit service error occurred, please contact support.",
                    innerException: failedAuditServiceException);

            Mock<AuditService> auditServiceMock = new Mock<AuditService>(this.dateTimeBrokerMock.Object)
            {
                CallBase = true
            };

            auditServiceMock.Setup(broker =>
                broker.ValidateInputs(
                    It.IsAny<object>(),
                    It.IsAny<object>(),
                    It.IsAny<SecurityConfigurations>()))
                        .Throws(serviceException);

            // when
            ValueTask<ExpandoObject> task =
                auditServiceMock.Object.EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync(
                    someObject,
                    someStorageObject,
                    someSecurityConfigurations);

            AuditServiceException actualAuditServiceException =
                await Assert.ThrowsAsync<AuditServiceException>(task.AsTask);

            // then
            actualAuditServiceException.Should()
                .BeEquivalentTo(expectedAuditServiceException);

            auditServiceMock.Verify(broker =>
                broker.ValidateInputs(
                    It.IsAny<object>(),
                    It.IsAny<object>(),
                    It.IsAny<SecurityConfigurations>()),
                    Times.Once);

            auditServiceMock.VerifyNoOtherCalls();
        }
    }
}
