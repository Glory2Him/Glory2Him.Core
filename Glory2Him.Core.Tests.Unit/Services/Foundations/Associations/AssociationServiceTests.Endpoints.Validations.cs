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

        public static TheoryData<string> ReclassifiableEndpointFields() =>
            new TheoryData<string>
            {
                nameof(Association.EntityAType),
                nameof(Association.EntityAKeyId),
                nameof(Association.EntityAGroupId),
                nameof(Association.EntityBType),
                nameof(Association.EntityBKeyId),
                nameof(Association.EntityBGroupId)
            };

        [Theory]
        [MemberData(nameof(ReclassifiableEndpointFields))]
        public async Task ShouldThrowValidationExceptionOnModifyIfAnEndpointWasRepointedAndLogItAsync(
            string repointedField)
        {
            // given: repointing an association is indistinguishable from deleting one link
            // and creating another — except that it carries the original's approval state
            // and review history across to a pair nobody reviewed
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);

            Association invalidAssociation = randomAssociation;
            Association storageAssociation = invalidAssociation.DeepClone();
            storageAssociation.UpdatedWhen = storageAssociation.CreatedWhen;

            string expectedMessage = RepointEndpointField(invalidAssociation, repointedField);

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: repointedField,
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

        [Fact]
        public async Task ShouldNotPinEndpointScopeAgainstStorageOnModifyAsync()
        {
            // given: scope is the one endpoint field that may move after creation — a
            // versioned endpoint can be narrowed to a single version or widened back. This
            // test exists to prove the reclassification pin does not over-reach onto it.
            //
            // That the *general* modify is the path carrying the change is temporary: design
            // §9.7.1 rule 6 gives scope its own Publisher/Admin-gated operation publishing
            // `-Scoped`, and the general modify becomes content-only when the five
            // state-transition operations land.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);

            Association inputAssociation = randomAssociation;
            Association storageAssociation = inputAssociation.DeepClone();
            storageAssociation.UpdatedWhen = storageAssociation.CreatedWhen;
            storageAssociation.EntityAScope = Scope.ThisVersionOnly;

            inputAssociation.EntityAScope = Scope.AllVersions;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    inputAssociation.Id,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    inputAssociation,
                    storageAssociation))
                    .ReturnsAsync(inputAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    inputAssociation,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inputAssociation);

            // when
            Association actualAssociation =
                await this.associationService.ModifyAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.EntityAScope.Should().Be(Scope.AllVersions);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(
                    inputAssociation,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // mutates the field under test to something the storage row does not have, and
        // returns the message the pin is expected to produce for it
        private static string RepointEndpointField(
            Association association,
            string fieldName)
        {
            switch (fieldName)
            {
                case nameof(Association.EntityAType):
                    association.EntityAType = EntityType.Comment;

                    return $"Value is not the same as {nameof(Association.EntityAType)}";

                case nameof(Association.EntityAKeyId):
                    association.EntityAKeyId = Guid.NewGuid();

                    return $"Id is not the same as {nameof(Association.EntityAKeyId)}";

                case nameof(Association.EntityAGroupId):
                    association.EntityAGroupId = Guid.NewGuid();

                    return $"Id is not the same as {nameof(Association.EntityAGroupId)}";

                case nameof(Association.EntityBType):
                    association.EntityBType = EntityType.Comment;

                    return $"Value is not the same as {nameof(Association.EntityBType)}";

                case nameof(Association.EntityBKeyId):
                    association.EntityBKeyId = Guid.NewGuid();

                    return $"Id is not the same as {nameof(Association.EntityBKeyId)}";

                case nameof(Association.EntityBGroupId):
                    association.EntityBGroupId = Guid.NewGuid();

                    return $"Id is not the same as {nameof(Association.EntityBGroupId)}";

                default:
                    throw new ArgumentOutOfRangeException(
                        paramName: nameof(fieldName),
                        actualValue: fieldName,
                        message: "Unhandled endpoint field.");
            }
        }
    }
}
