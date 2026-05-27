// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Tests.Unit.Models;
using Moq;

namespace Glory2Him.Security.Client.Tests.Clients.Audits
{
    public partial class AuditClientTests
    {
        [Fact]
        public async Task ShouldEnsureAddAuditValuesRemainsUnchangedOnModifyAsync()
        {
            // Given
            DateTimeOffset currentDateTime = DateTime.UtcNow;
            Person inputPerson = new Person { Name = GetRandomString() };
            Person storagePerson = new Person { Name = GetRandomString() };
            Person updatedPerson = new Person { Name = GetRandomString() };
            Person expectedResult = updatedPerson;
            var securityConfigurations = new SecurityConfigurations();

            this.auditOrchestrationServiceMock.Setup(service =>
                service.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    inputPerson,
                    storagePerson,
                    securityConfigurations))
                .ReturnsAsync(updatedPerson);

            // When
            dynamic actualResult = await this.auditClient
                .EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(inputPerson, storagePerson, securityConfigurations);

            // Then
            ((object)actualResult).Should().BeEquivalentTo(expectedResult);
        }
    }
}