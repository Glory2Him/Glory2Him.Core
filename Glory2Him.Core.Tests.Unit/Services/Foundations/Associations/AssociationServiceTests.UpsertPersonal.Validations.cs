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
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfAssociationIsNullAndLogItAsync()
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
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    nullAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(upsertTask.AsTask);

            // then: refused before the envelope is minted
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
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfUserIdIsNullAndLogItAsync()
        {
            // given: a signed-in reader sends a row whose UserId is null — an editorial row, which
            // this member never reaches: the repoint exception is personal-only (§ARC16.2.2)
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association editorialRequest = CreatePersonalUpsertRequest(readerUserId);
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

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    editorialRequest,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(upsertTask.AsTask);

            // then: refused as invalid before the caller is resolved, and storage never asked
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
        [MemberData(nameof(InvalidPersonalUpsertEndpoints))]
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfAnEndpointIsInvalidAndLogItAsync(
            string invalidField,
            string expectedMessage)
        {
            // given: the reader holds a row on the item, so an upsert that reached storage would
            // find something to write
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association invalidRequest = InvalidatePersonalUpsertEndpoint(
                CreatePersonalUpsertRequest(readerUserId),
                invalidField);

            Association readersRow =
                CreateStoredPersonalRow(CreatePersonalUpsertRequest(readerUserId), isDeleted: false);

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: invalidField,
                value: expectedMessage);

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(
                new[] { readersRow },
                TestContext.Current.CancellationToken);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    invalidRequest,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(upsertTask.AsTask);

            // then: refused as the add refuses it, naming the field, and storage never asked
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

        [Theory]
        [MemberData(nameof(ApprovalStatesOnANewPersonalRow))]
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfTheNewRowCarriesApprovalStateAndLogItAsync(
            string invalidField,
            object invalidValue,
            string expectedMessage)
        {
            // given: the reader has no row, so the request would be inserted, and a contribution
            // is created unpublished at Draft or Submitted - publication and a verdict are the
            // approval workflow's to record
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association upsertRequest = SetApprovalState(
                CreatePersonalUpsertRequest(readerUserId),
                invalidField,
                invalidValue);

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: invalidField,
                value: expectedMessage);

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(
                CreateRandomAssociations(),
                TestContext.Current.CancellationToken);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(upsertTask.AsTask);

            // then: the create is refused, naming the field, and nothing is stamped or written
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(upsertRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalUpsertLookupAsked(TestContext.Current.CancellationToken, Times.Once());

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
        [InlineData(nameof(Association.EntityAScope))]
        [InlineData(nameof(Association.EntityBScope))]
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfAnEndpointScopeIsUndefinedAndLogItAsync(
            string invalidField)
        {
            // given: the reader's row was taken down. A scope outside the enum keys the host on its
            // key id rather than its group, so the lookup would miss that row and insert a second
            // one beside it, answering Created where a takedown answers TakenDown (§DOM4.10 rules 6
            // and 7). Either endpoint may be A once canonical order is restored, so both are refused.
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association invalidRequest = InvalidateEndpointField(
                CreatePersonalUpsertRequest(readerUserId),
                invalidField);

            Association takenDownRow =
                CreateStoredPersonalRow(CreatePersonalUpsertRequest(readerUserId), isDeleted: true);

            takenDownRow.DeletedBy = $"moderator-{Guid.NewGuid()}";

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: invalidField,
                value: "Value is not a supported scope");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(
                new[] { takenDownRow },
                TestContext.Current.CancellationToken);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    invalidRequest,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(upsertTask.AsTask);

            // then: refused before the row is resolved, naming the field
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

        // every approval state a new row may not carry, with the add's message for it
        public static TheoryData<string, object, string> ApprovalStatesOnANewPersonalRow() =>
            new TheoryData<string, object, string>
            {
                {
                    nameof(Association.ApprovalStatus),
                    ApprovalStatus.Approved,
                    "Value must be Draft or Submitted on add"
                },
                {
                    nameof(Association.ApprovalStatus),
                    ApprovalStatus.Rejected,
                    "Value must be Draft or Submitted on add"
                },
                {
                    nameof(Association.ApprovalStatus),
                    ApprovalStatus.Dismissed,
                    "Value must be Draft or Submitted on add"
                },
                {
                    nameof(Association.IsPublished),
                    true,
                    "Value is not allowed on add"
                },
                {
                    nameof(Association.PublishDate),
                    GetRandomDateTimeOffset(),
                    "Date is not allowed on add"
                }
            };

        private static Association SetApprovalState(Association request, string field, object value)
        {
            switch (field)
            {
                case nameof(Association.ApprovalStatus):
                    request.ApprovalStatus = (ApprovalStatus)value;
                    break;

                case nameof(Association.IsPublished):
                    request.IsPublished = (bool)value;
                    break;

                case nameof(Association.PublishDate):
                    request.PublishDate = (DateTimeOffset)value;
                    break;
            }

            return request;
        }

        public static TheoryData<string, string> InvalidPersonalUpsertEndpoints() =>
            new TheoryData<string, string>
            {
                { nameof(Association.EntityAKeyId), "Id is required" },
                { nameof(Association.EntityBKeyId), "Id is required" },
                { nameof(Association.EntityAType), "Value is not a supported entity type" },
                { nameof(Association.EntityBType), "Value is not a supported entity type" },
                { nameof(Association.EntityBGroupId), "Value is the same as EntityAGroupId" }
            };

        // an empty key or a type outside its enum, as the personal lookup's tests break them, or
        // the far end in the host's own group
        private static Association InvalidatePersonalUpsertEndpoint(Association request, string field)
        {
            if (field == nameof(Association.EntityBGroupId))
            {
                request.EntityBGroupId = request.EntityAGroupId;

                return request;
            }

            return InvalidateEndpointField(request, field);
        }
    }
}
