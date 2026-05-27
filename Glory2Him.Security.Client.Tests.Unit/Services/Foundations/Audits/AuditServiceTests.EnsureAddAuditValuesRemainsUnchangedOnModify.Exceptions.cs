// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Dynamic;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Models.Foundations.Audits.Exceptions;
using Glory2Him.Security.Client.Services.Foundations.Audits;
using Moq;

namespace Glory2Him.Security.Client.Tests.Unit.Services.Foundations.Audits
{
    public partial class AuditServiceTests
    {
        [Fact]
        public async Task ShouldThrowServiceExceptionOnEnsureAddAuditValuesRemainsUnchangedIfServiceErrorOccursAsync()
        {
            // given
            dynamic someObject = new ExpandoObject();
            someObject.Name = "John Doe";
            someObject.CreatedBy = string.Empty;
            someObject.CreatedDate = DateTimeOffset.MinValue;
            someObject.UpdatedBy = string.Empty;
            someObject.UpdatedDate = DateTimeOffset.MinValue;

            dynamic someStorageObject = new ExpandoObject();
            someObject.Name = "John Doe";
            someObject.CreatedBy = string.Empty;
            someObject.CreatedDate = DateTimeOffset.MinValue;
            someObject.UpdatedBy = string.Empty;
            someObject.UpdatedDate = DateTimeOffset.MinValue;

            string someUserId = GetRandomString();
            var serviceException = new Exception();

            var someSecurityConfigurations = new SecurityConfigurations
            {
                CreatedByPropertyName = "CreatedBy",
                CreatedByPropertyType = typeof(string),
                CreatedDatePropertyName = "CreatedDate",
                CreatedDatePropertyType = typeof(DateTimeOffset),
                UpdatedByPropertyName = "UpdatedBy",
                UpdatedByPropertyType = typeof(string),
                UpdatedDatePropertyName = "UpdatedDate",
                UpdatedDatePropertyType = typeof(DateTimeOffset)
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
            ValueTask<ExpandoObject> ensureAddAuditValuesRemainsUnchangedOnModifyTask =
                auditServiceMock.Object.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    someObject,
                    someStorageObject,
                    someSecurityConfigurations);

            AuditServiceException actualAuditServiceException =
                await Assert.ThrowsAsync<AuditServiceException>(
                    ensureAddAuditValuesRemainsUnchangedOnModifyTask.AsTask);

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