// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindPersonalIfAssociationIsNullAndLogItAsync()
        {
            // given
            Association nullAssociation = null;

            var nullAssociationException =
                new NullAssociationException(message: "Content item association is null.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: nullAssociationException);

            // when
            ValueTask<PersonalAssociationMatch?> findTask =
                this.associationService.FindPersonalAssociationAsync(
                    nullAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(findTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindPersonalIfUserIdIsNullAndLogItAsync()
        {
            // given: a null UserId is an editorial row, which has no personal key
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association editorialRequest = CreatePersonalLookupRequest(GetRandomString());
            editorialRequest.UserId = null;

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: nameof(Association.UserId),
                value: "Text is required");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            // when
            ValueTask<PersonalAssociationMatch?> findTask =
                this.associationService.FindPersonalAssociationAsync(
                    editorialRequest,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(findTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(editorialRequest),
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(InvalidPersonalLookupEndpoints))]
        public async Task ShouldThrowValidationExceptionOnFindPersonalIfAnEndpointIsInvalidAndLogItAsync(
            string invalidField,
            string expectedMessage)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association invalidRequest = InvalidateEndpointField(
                CreatePersonalLookupRequest(GetRandomString()),
                invalidField);

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: invalidField,
                value: expectedMessage);

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            // when
            ValueTask<PersonalAssociationMatch?> findTask =
                this.associationService.FindPersonalAssociationAsync(
                    invalidRequest,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(findTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(invalidRequest),
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        public static TheoryData<string, string> InvalidPersonalLookupEndpoints() =>
            new TheoryData<string, string>
            {
                { nameof(Association.EntityAKeyId), "Id is required" },
                { nameof(Association.EntityBKeyId), "Id is required" },
                { nameof(Association.EntityAType), "Value is not a supported entity type" },
                { nameof(Association.EntityBType), "Value is not a supported entity type" }
            };

        // an empty key, or a type outside EntityType — a stale client sending a removed member
        private static Association InvalidateEndpointField(Association request, string field)
        {
            var undefinedEntityType = (EntityType)(-1);

            switch (field)
            {
                case nameof(Association.EntityAKeyId):
                    request.EntityAKeyId = Guid.Empty;
                    break;

                case nameof(Association.EntityBKeyId):
                    request.EntityBKeyId = Guid.Empty;
                    break;

                case nameof(Association.EntityAType):
                    request.EntityAType = undefinedEntityType;
                    break;

                case nameof(Association.EntityBType):
                    request.EntityBType = undefinedEntityType;
                    break;
            }

            return request;
        }
    }
}
