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
using System.Threading;
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
        public async Task ShouldThrowValidationExceptionOnAddIfBothEndpointsAreTheSameEntityAndLogItAsync()
        {
            // given: an entity associated with itself. Because a non-versioned endpoint's
            // group id is its key id, this one rule also catches two versions of the same
            // content item and a tag paired with itself.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid sharedGroupId = Guid.NewGuid();

            Association invalidAssociation =
                CreateAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            invalidAssociation.EntityAGroupId = sharedGroupId;
            invalidAssociation.EntityBGroupId = sharedGroupId;

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.EntityBGroupId),
                values: $"Value is the same as {nameof(Association.EntityAGroupId)}");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfEndpointTypeIsNotDefinedAndLogItAsync()
        {
            // given: a stale client sending a since-removed member. This has to be reported
            // rather than resolved — an undefined type has no publication model, so the
            // scope derivation would otherwise throw out of a lookup.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association invalidAssociation =
                CreateAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            invalidAssociation.EntityAType = (EntityType)int.MaxValue;

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.EntityAType),
                values: "Value is not a supported entity type");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(EntityType.Tag)]
        [InlineData(EntityType.BibleReference)]
        [InlineData(EntityType.Reaction)]
        public async Task
            ShouldThrowValidationExceptionOnAddIfContentTypeIsSetOnANonContentItemEndpointAndLogItAsync(
                EntityType nonContentItemType)
        {
            // given: a content type on an endpoint that has no sub-classification. This is
            // an authorization input (design §18.6) — a caller who could set it here could
            // claim a content-type-scoped role over an entity that has no content type.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association invalidAssociation =
                CreateAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            invalidAssociation.EntityAType = nonContentItemType;
            invalidAssociation.EntityAKeyId = Guid.NewGuid();
            invalidAssociation.EntityAContentType = ContentType.Testimony;
            invalidAssociation.EntityBType = EntityType.ContentItem;
            invalidAssociation.EntityBKeyId = Guid.NewGuid();
            invalidAssociation.EntityBGroupId = Guid.NewGuid();

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.EntityAContentType),
                values: $"Value is only applicable to a {nameof(EntityType.ContentItem)} endpoint");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfContentTypeIsNotADefinedMemberAndLogItAsync()
        {
            // given: a ContentItem endpoint carrying a content type outside the enum — the
            // structural rule below only rejects a value on the WRONG endpoint type, so
            // without a definedness check this reaches the database as a string
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association invalidAssociation =
                CreateAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            invalidAssociation.EntityBContentType = (ContentType)int.MaxValue;

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.EntityBContentType),
                values: "Value is not a supported content type");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            SetupFailingAddPathBrokers(invalidAssociation, randomUserId, randomDateTimeOffset);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIdExceedsMaxLengthAndLogItAsync()
        {
            // given: the column is nvarchar(255). Without a rule the overflow surfaces as a
            // storage dependency error rather than a validation error the caller can act on.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association invalidAssociation =
                CreateAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            invalidAssociation.UserId = GetRandomStringWithLengthOf(256);

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.UserId),
                values: "Text exceed max length of 255 characters");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            SetupFailingAddPathBrokers(invalidAssociation, randomUserId, randomDateTimeOffset);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfModelVersionExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association invalidAssociation =
                CreateAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            invalidAssociation.ModelVersion = GetRandomStringWithLengthOf(129);

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.ModelVersion),
                values: "Text exceed max length of 128 characters");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            SetupFailingAddPathBrokers(invalidAssociation, randomUserId, randomDateTimeOffset);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // Every endpoint field the general modify pins against storage. Scope sits here with
        // the identity fields now that the state-transition operations have landed: it is not
        // reclassification, but it has its own Publisher/Admin-gated operation (design §9.7.1
        // rule 6), and leaving it writable on modify would route straight around that gate.
        public static TheoryData<string> PinnedEndpointFields() =>
            new TheoryData<string>
            {
                nameof(Association.EntityAType),
                nameof(Association.EntityAKeyId),
                nameof(Association.EntityAGroupId),
                nameof(Association.EntityAContentType),
                nameof(Association.EntityAScope),
                nameof(Association.EntityBType),
                nameof(Association.EntityBKeyId),
                nameof(Association.EntityBGroupId),
                nameof(Association.EntityBContentType),
                nameof(Association.EntityBScope)
            };

        [Theory]
        [MemberData(nameof(PinnedEndpointFields))]
        public async Task ShouldThrowValidationExceptionOnModifyIfAnEndpointFieldWasChangedAndLogItAsync(
            string changedField)
        {
            // given: repointing an association is indistinguishable from deleting one link
            // and creating another — except that it carries the original's approval state
            // and review history across to a pair nobody reviewed
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);

            Association invalidAssociation = randomAssociation;

            // a content type is legal only on a ContentItem endpoint, so the endpoint under
            // test becomes one before the storage row is cloned — otherwise the
            // not-applicable rule fires instead of the storage pin this test is about
            if (changedField == nameof(Association.EntityAContentType))
            {
                invalidAssociation.EntityAType = EntityType.ContentItem;
            }

            Association storageAssociation = invalidAssociation.DeepClone();
            storageAssociation.UpdatedWhen = storageAssociation.CreatedWhen;

            string expectedMessage = ChangeEndpointField(invalidAssociation, changedField);

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: changedField,
                values: expectedMessage);

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    invalidAssociation.Id,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAssociation,
                    storageAssociation))
                    .ReturnsAsync(invalidAssociation);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // The test that used to sit here asserted the OPPOSITE — that the general modify
        // carried a scope change — and it said so itself: "that the general modify is the
        // path carrying the change is temporary … the general modify becomes content-only
        // when the five state-transition operations land." They have landed, so the pin is
        // now deliberate and is covered by PinnedEndpointFields above. The path that carries
        // a scope change is SetAssociationScopeAsync, covered in the Transitions tests.

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfScopeIsNotADefinedMemberAndLogItAsync()
        {
            // given: scope is derived on add but caller-supplied on modify, so it is the one
            // endpoint field where an out-of-range enum can reach the row — and it feeds the
            // PERSISTED effective id, so a bad value moves the row's identity
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);

            Association invalidAssociation = randomAssociation;
            Association storageAssociation = invalidAssociation.DeepClone();
            storageAssociation.UpdatedWhen = storageAssociation.CreatedWhen;

            invalidAssociation.EntityAScope = (Scope)int.MaxValue;

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.EntityAScope),
                values: "Value is not a supported scope");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            SetupFailingModifyPathBrokers(invalidAssociation, storageAssociation, randomUserId, randomDateTimeOffset);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(EntityType.Tag)]
        [InlineData(EntityType.BibleReference)]
        [InlineData(EntityType.Comment)]
        public async Task
            ShouldThrowValidationExceptionOnModifyIfANonVersionedEndpointIsScopedToAllVersionsAndLogItAsync(
                EntityType nonVersionedType)
        {
            // given: a non-versioned entity has exactly one row, so AllVersions cannot mean
            // anything for it. Add derives this away; modify has to defend it, because
            // re-deriving here would overwrite the legitimate narrowing that the set-scope
            // operation exists to perform.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);

            Association invalidAssociation = randomAssociation;
            invalidAssociation.EntityAType = nonVersionedType;
            invalidAssociation.EntityAScope = Scope.ThisVersionOnly;

            Association storageAssociation = invalidAssociation.DeepClone();
            storageAssociation.UpdatedWhen = storageAssociation.CreatedWhen;

            invalidAssociation.EntityAScope = Scope.AllVersions;

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.EntityAScope),
                values: "Value is only applicable to a versioned endpoint");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            SetupFailingModifyPathBrokers(invalidAssociation, storageAssociation, randomUserId, randomDateTimeOffset);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private void SetupFailingAddPathBrokers(
            Association association,
            string actorUserId,
            DateTimeOffset currentDateTimeOffset)
        {
            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(association, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(association);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTimeOffset);
        }

        private void SetupFailingModifyPathBrokers(
            Association association,
            Association storageAssociation,
            string actorUserId,
            DateTimeOffset currentDateTimeOffset)
        {
            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(association, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(association);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    association.Id,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    association,
                    storageAssociation))
                    .ReturnsAsync(association);
        }

        // mutates the field under test to something the storage row does not have, and
        // returns the message the pin is expected to produce for it
        private static string ChangeEndpointField(
            Association association,
            string fieldName)
        {
            switch (fieldName)
            {
                case nameof(Association.EntityAType):
                    association.EntityAType = EntityType.Link;

                    return $"Value is not the same as {nameof(Association.EntityAType)}";

                case nameof(Association.EntityAKeyId):
                    association.EntityAKeyId = Guid.NewGuid();

                    return $"Id is not the same as {nameof(Association.EntityAKeyId)}";

                case nameof(Association.EntityAGroupId):
                    association.EntityAGroupId = Guid.NewGuid();

                    return $"Id is not the same as {nameof(Association.EntityAGroupId)}";

                case nameof(Association.EntityAContentType):
                    association.EntityAContentType = ContentType.Story;

                    return $"Value is not the same as {nameof(Association.EntityAContentType)}";

                // narrowing, not widening: the filler pins both endpoints to AllVersions and
                // both drawn types are versioned, so ThisVersionOnly differs from storage
                // while staying applicable — the pin fires, not the applicability rule
                case nameof(Association.EntityAScope):
                    association.EntityAScope = Scope.ThisVersionOnly;

                    return $"Value is not the same as {nameof(Association.EntityAScope)}";

                case nameof(Association.EntityBType):
                    association.EntityBType = EntityType.Link;

                    return $"Value is not the same as {nameof(Association.EntityBType)}";

                case nameof(Association.EntityBKeyId):
                    association.EntityBKeyId = Guid.NewGuid();

                    return $"Id is not the same as {nameof(Association.EntityBKeyId)}";

                case nameof(Association.EntityBGroupId):
                    association.EntityBGroupId = Guid.NewGuid();

                    return $"Id is not the same as {nameof(Association.EntityBGroupId)}";

                case nameof(Association.EntityBContentType):
                    association.EntityBContentType = ContentType.Story;

                    return $"Value is not the same as {nameof(Association.EntityBContentType)}";

                case nameof(Association.EntityBScope):
                    association.EntityBScope = Scope.ThisVersionOnly;

                    return $"Value is not the same as {nameof(Association.EntityBScope)}";

                default:
                    throw new ArgumentOutOfRangeException(
                        paramName: nameof(fieldName),
                        actualValue: fieldName,
                        message: "Unhandled endpoint field.");
            }
        }
    }
}
