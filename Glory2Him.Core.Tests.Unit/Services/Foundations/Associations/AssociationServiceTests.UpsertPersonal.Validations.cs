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
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
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
        [MemberData(nameof(PairsWithNoHostOnEndpointA))]
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfThePairHasNoHostOnEndpointAAndLogItAsync(
            string pair)
        {
            // given: the reader's own upsert of an otherwise well-formed pair, so every earlier
            // refusal lets it through to this one, which is asked once canonical order is restored.
            // The store holds the reader's row keyed on the reaction, so an upsert that reached it
            // would move the tag (Backend/Foundations/AssociationService.md §8 rule 1; §DOM4.10
            // rule 9).
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association noHostRequest = CreatePairWithNoHostOnEndpointA(readerUserId, pair);
            noHostRequest.ApprovalStatus = ApprovalStatus.Submitted;

            Association readersRow = CreateStoredPersonalRow(
                CreatePairWithNoHostOnEndpointA(readerUserId, "ReactionThenTag"),
                isDeleted: false);

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: nameof(Association.EntityAType),
                value: "Value is a personal entity type, which cannot be endpoint A");

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
                    noHostRequest,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(upsertTask.AsTask);

            // then: refused naming EntityAType, storage never asked, nothing written, no fact
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(noHostRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.identifierBrokerMock.VerifyNoOtherCalls();
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

        [Theory]
        [InlineData(false, nameof(Association.EntityAScope), Scope.ThisVersionOnly)]
        [InlineData(true, nameof(Association.EntityBScope), Scope.ThisVersionOnly)]
        [InlineData(false, nameof(Association.EntityBScope), Scope.AllVersions)]
        [InlineData(true, nameof(Association.EntityAScope), Scope.AllVersions)]
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfAnEndpointScopeIsNotItsTypesAndLogItAsync(
            bool isReversed,
            string invalidField,
            Scope wrongScope)
        {
            // given: the content item under ThisVersionOnly, or the reaction under AllVersions, on
            // whichever side the request names it. A content item keyed on its key id misses the
            // reader's row and takes the none arm; a reaction under AllVersions is a row §DOM4.5
            // rule 1 forbids (§2 rule 9).
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association request = CreatePersonalUpsertRequest(readerUserId);
            Association invalidRequest = isReversed ? ReverseEndpoints(request) : request;

            if (invalidField == nameof(Association.EntityAScope))
            {
                invalidRequest.EntityAScope = wrongScope;
            }
            else
            {
                invalidRequest.EntityBScope = wrongScope;
            }

            Association takenDownRow =
                CreateStoredPersonalRow(CreatePersonalUpsertRequest(readerUserId), isDeleted: true);

            takenDownRow.DeletedBy = $"moderator-{Guid.NewGuid()}";

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: invalidField,
                value: "Value is not the scope its endpoint's type takes");

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

            // then: refused before the row is resolved, naming the field the request carries
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
        [InlineData(nameof(Association.EntityAGroupId))]
        [InlineData(nameof(Association.EntityBGroupId))]
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfAnEndpointGroupIsEmptyAndLogItAsync(
            string invalidField)
        {
            // given: under AllVersions the host's group is its effective id, so an empty one keys the
            // lookup on no host; the reaction's group is what a repoint writes onto the stored row
            // (§2 rule 9). Either endpoint may be A once canonical order is restored.
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association invalidRequest = InvalidateEndpointField(
                CreatePersonalUpsertRequest(readerUserId),
                invalidField);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    invalidRequest,
                    TestContext.Current.CancellationToken);

            // then: refused before the row is resolved, naming the field
            await VerifyUpsertRefusedBeforeTheLookupAsync(
                upsertTask,
                invalidRequest,
                readerUserId,
                invalidField,
                expectedMessage: "Id is required");
        }

        [Theory]
        [InlineData(true, nameof(Association.EntityAGroupId))]
        [InlineData(false, nameof(Association.EntityBGroupId))]
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfANonVersionedEndpointsGroupIsNotItsKeyAndLogItAsync(
            bool isReversed,
            string invalidField)
        {
            // given: the reaction, on whichever side the request names it, carrying a group other
            // than its key. A non-versioned endpoint's group is its key (§DOM4.5 rule 2), and the
            // repoint would otherwise write the wrong one onto the stored row (§2 rule 9).
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association request = CreatePersonalUpsertRequest(readerUserId);
            Association invalidRequest = isReversed ? ReverseEndpoints(request) : request;

            if (invalidField == nameof(Association.EntityAGroupId))
            {
                invalidRequest.EntityAGroupId = Guid.NewGuid();
            }
            else
            {
                invalidRequest.EntityBGroupId = Guid.NewGuid();
            }

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    invalidRequest,
                    TestContext.Current.CancellationToken);

            // then: refused before the row is resolved, naming the field the request carries
            await VerifyUpsertRefusedBeforeTheLookupAsync(
                upsertTask,
                invalidRequest,
                readerUserId,
                invalidField,
                expectedMessage: "Value is not the key of its non-versioned endpoint");
        }

        // the request is refused as invalid naming one field, before the caller's id is asked or the
        // row resolved, and nothing is read, written or announced
        private async Task VerifyUpsertRefusedBeforeTheLookupAsync(
            ValueTask<PersonalAssociationUpsert> upsertTask,
            Association invalidRequest,
            string readerUserId,
            string invalidField,
            string expectedMessage)
        {
            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: invalidField,
                value: expectedMessage);

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(upsertTask.AsTask);

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
        [MemberData(nameof(InvalidNewPersonalRows))]
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfTheNewRowIsInvalidAndLogItAsync(
            string invalidity,
            string invalidField,
            string expectedMessage)
        {
            // given: the reader has no row, so the request would be inserted, and it breaks one of
            // the add's rules that no earlier check asks. The new row is validated as the add
            // validates one, after canonical order is restored, so a content type names the field
            // the new row stores it in (§2 rules 1 and 4).
            string readerUserId = invalidity == nameof(Association.UserId)
                ? GetRandomStringWithLengthOf(256)
                : GetRandomString();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association upsertRequest = MakeNewPersonalRowInvalid(
                CreatePersonalUpsertRequest(readerUserId),
                invalidity);

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

            // then: the create is refused, naming the field, and nothing is minted, stamped or
            // inserted
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
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // each rule of the add that no earlier check of the upsert asks, save the empty Id the
        // create arm mints over (§2 rule 10), with the field and the add's message for it
        public static TheoryData<string, string, string> InvalidNewPersonalRows() =>
            new TheoryData<string, string, string>
            {
                {
                    "EntityAContentTypeNotApplicable",
                    nameof(Association.EntityAContentType),
                    "Value is only applicable to a ContentItem endpoint"
                },
                {
                    "EntityAContentTypeUndefined",
                    nameof(Association.EntityAContentType),
                    "Value is not a supported content type"
                },
                {
                    "EntityBContentTypeNotApplicable",
                    nameof(Association.EntityBContentType),
                    "Value is only applicable to a ContentItem endpoint"
                },
                {
                    "EntityBContentTypeUndefined",
                    nameof(Association.EntityBContentType),
                    "Value is not a supported content type"
                },
                {
                    "ReactionNamedAsEndpointACarryingAContentType",
                    nameof(Association.EntityBContentType),
                    "Value is only applicable to a ContentItem endpoint"
                },
                {
                    nameof(Association.UserId),
                    nameof(Association.UserId),
                    "Text exceed max length of 255 characters"
                },
                {
                    nameof(Association.ConfidenceReason),
                    nameof(Association.ConfidenceReason),
                    "Text exceed max length of 500 characters"
                },
                {
                    nameof(Association.ModelVersion),
                    nameof(Association.ModelVersion),
                    "Text exceed max length of 128 characters"
                },
                {
                    "ConfidenceScoreBelowZero",
                    nameof(Association.ConfidenceScore),
                    "Value is not within range of 0 and 10"
                },
                {
                    "ConfidenceScoreAboveTen",
                    nameof(Association.ConfidenceScore),
                    "Value is not within range of 0 and 10"
                }
            };

        // Each pair is otherwise valid for the upsert: every non-versioned endpoint's group is its
        // key and every scope is the one its type takes. Canonical order is ordinal on the type's
        // name, so a BibleReference stays endpoint A beside a reaction or a content item.
        private static Association MakeNewPersonalRowInvalid(Association request, string invalidity)
        {
            var undefinedContentType = (ContentType)int.MaxValue;

            switch (invalidity)
            {
                case "EntityAContentTypeNotApplicable":
                    request.EntityAType = EntityType.BibleReference;
                    request.EntityAGroupId = request.EntityAKeyId;
                    request.EntityAScope = Scope.ThisVersionOnly;
                    request.EntityAContentType = ContentType.Quote;
                    break;

                case "EntityAContentTypeUndefined":
                    request.EntityAContentType = undefinedContentType;
                    break;

                case "EntityBContentTypeNotApplicable":
                    request.EntityBContentType = ContentType.Quote;
                    break;

                case "EntityBContentTypeUndefined":
                    request.EntityBType = EntityType.ContentItem;
                    request.EntityBScope = Scope.AllVersions;
                    request.EntityBGroupId = Guid.NewGuid();
                    request.EntityBContentType = undefinedContentType;
                    request.EntityAType = EntityType.BibleReference;
                    request.EntityAGroupId = request.EntityAKeyId;
                    request.EntityAScope = Scope.ThisVersionOnly;
                    request.EntityAContentType = null;
                    break;

                case "ReactionNamedAsEndpointACarryingAContentType":
                    request.EntityAContentType = null;
                    request.EntityBContentType = ContentType.Quote;
                    request = ReverseEndpoints(request);
                    break;

                case nameof(Association.UserId):
                    break;

                case nameof(Association.ConfidenceReason):
                    request.ConfidenceReason = GetRandomStringWithLengthOf(501);
                    break;

                case nameof(Association.ModelVersion):
                    request.ModelVersion = GetRandomStringWithLengthOf(129);
                    break;

                case "ConfidenceScoreBelowZero":
                    request.ConfidenceScore = -1;
                    break;

                case "ConfidenceScoreAboveTen":
                    request.ConfidenceScore = 11;
                    break;
            }

            return request;
        }

        [Theory]
        [InlineData("CreatedByEmpty")]
        [InlineData("UpdatedByEmpty")]
        [InlineData("CreatedWhenUnset")]
        [InlineData("UpdatedWhenUnset")]
        [InlineData("CreatedByTooLong")]
        [InlineData("UpdatedByTooLong")]
        [InlineData("CreatedByNotTheCaller")]
        [InlineData("UpdatedByNotCreatedBy")]
        [InlineData("UpdatedWhenNotCreatedWhen")]
        [InlineData("CreatedWhenNotRecent")]
        public async Task ShouldThrowValidationExceptionOnUpsertPersonalIfTheNewRowsAuditStampIsInvalidAndLogItAsync(
            string invalidity)
        {
            // given: the reader has no row, and the stamp on the row ApplyAddAuditValuesAsync
            // returns breaks one of the add's audit rules, asked of that row before the insert as
            // the add asks them (§2 rule 4)
            string readerUserId = GetRandomString();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association upsertRequest = CreatePersonalUpsertRequest(readerUserId);

            (Action<Association> breakStamp, (string Field, string Message)[] expectedErrors) =
                BreakNewRowsAuditStamp(invalidity, readerUserId, currentDateTime);

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            foreach ((string field, string message) in expectedErrors)
            {
                invalidAssociationException.UpsertDataList(key: field, value: message);
            }

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

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext))
                    .ReturnsAsync((Association entity, SecurityContext _) =>
                    {
                        Association auditedAssociation =
                            StampAddAudit(entity.DeepClone(), readerUserId, currentDateTime);

                        breakStamp(auditedAssociation);

                        return auditedAssociation;
                    });

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<PersonalAssociationUpsert> upsertTask =
                this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(upsertTask.AsTask);

            // then: the create is refused, naming the field, and nothing is inserted or announced
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext),
                    Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                    Times.Once);

            VerifyPersonalUpsertLookupAsked(TestContext.Current.CancellationToken, Times.Once());

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // How each case breaks the stamp, and every error the add's rules then raise, in the order
        // the add asks them: a stamp broken one way can break a second rule that compares with it.
        private static (Action<Association>, (string, string)[]) BreakNewRowsAuditStamp(
            string invalidity,
            string readerUserId,
            DateTimeOffset currentDateTime)
        {
            string otherUserId = GetRandomString();
            string tooLongUserId = GetRandomStringWithLengthOf(256);
            string notTheSameAsCreatedBy = "Text is not the same as CreatedBy";
            string notTheSameAsCreatedWhen = "Date is not the same as CreatedWhen";

            string NotTheCaller(string createdBy) =>
                $"Expected value to be '{readerUserId}' but found '{createdBy}'.";

            string NotRecent(DateTimeOffset date) =>
                $"Date is not recent. Expected a value between {currentDateTime.AddSeconds(-90)} "
                    + $"and {currentDateTime} but found {date}";

            DateTimeOffset staleDateTime = currentDateTime.AddDays(-1);

            return invalidity switch
            {
                "CreatedByEmpty" => (
                    association => association.CreatedBy = " ",
                    new[]
                    {
                        (nameof(Association.CreatedBy), "Text is required"),
                        (nameof(Association.CreatedBy), NotTheCaller(" ")),
                        (nameof(Association.UpdatedBy), notTheSameAsCreatedBy)
                    }),

                "UpdatedByEmpty" => (
                    association => association.UpdatedBy = " ",
                    new[]
                    {
                        (nameof(Association.UpdatedBy), "Text is required"),
                        (nameof(Association.UpdatedBy), notTheSameAsCreatedBy)
                    }),

                "CreatedWhenUnset" => (
                    association => association.CreatedWhen = default,
                    new[]
                    {
                        (nameof(Association.CreatedWhen), "Date is required"),
                        (nameof(Association.UpdatedWhen), notTheSameAsCreatedWhen),
                        (nameof(Association.CreatedWhen), NotRecent(default))
                    }),

                "UpdatedWhenUnset" => (
                    association => association.UpdatedWhen = default,
                    new[]
                    {
                        (nameof(Association.UpdatedWhen), "Date is required"),
                        (nameof(Association.UpdatedWhen), notTheSameAsCreatedWhen)
                    }),

                "CreatedByTooLong" => (
                    association => association.CreatedBy = tooLongUserId,
                    new[]
                    {
                        (nameof(Association.CreatedBy), "Text exceed max length of 255 characters"),
                        (nameof(Association.CreatedBy), NotTheCaller(tooLongUserId)),
                        (nameof(Association.UpdatedBy), notTheSameAsCreatedBy)
                    }),

                "UpdatedByTooLong" => (
                    association => association.UpdatedBy = tooLongUserId,
                    new[]
                    {
                        (nameof(Association.UpdatedBy), "Text exceed max length of 255 characters"),
                        (nameof(Association.UpdatedBy), notTheSameAsCreatedBy)
                    }),

                "CreatedByNotTheCaller" => (
                    association =>
                    {
                        association.CreatedBy = otherUserId;
                        association.UpdatedBy = otherUserId;
                    },
                    new[]
                    {
                        (nameof(Association.CreatedBy), NotTheCaller(otherUserId))
                    }),

                "UpdatedByNotCreatedBy" => (
                    association => association.UpdatedBy = otherUserId,
                    new[]
                    {
                        (nameof(Association.UpdatedBy), notTheSameAsCreatedBy)
                    }),

                "UpdatedWhenNotCreatedWhen" => (
                    association => association.UpdatedWhen = currentDateTime.AddSeconds(-1),
                    new[]
                    {
                        (nameof(Association.UpdatedWhen), notTheSameAsCreatedWhen)
                    }),

                _ => (
                    association =>
                    {
                        association.CreatedWhen = staleDateTime;
                        association.UpdatedWhen = staleDateTime;
                    },
                    new[]
                    {
                        (nameof(Association.CreatedWhen), NotRecent(staleDateTime))
                    })
            };
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
            // a reaction keyed on the host's group, so its group is still its key and only the
            // one-group rule is broken (§DOM4.5 rule 2)
            if (field == nameof(Association.EntityBGroupId))
            {
                request.EntityBKeyId = request.EntityAGroupId;
                request.EntityBGroupId = request.EntityAGroupId;

                return request;
            }

            return InvalidateEndpointField(request, field);
        }
    }
}
