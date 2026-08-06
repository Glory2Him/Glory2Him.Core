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
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        // ── The approval state is not a caller's to assert ───────────────────────────

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalStateWasChangedAndLogItAsync()
        {
            // given: THE escalation this suite exists to prevent. A caller holding only
            // Tag-Reviewer now passes the write gate on any association with a Tag endpoint
            // — that is the point of endpoint-derived authorization. If the general modify
            // still carried IApproval, that same caller could take a stranger's pending
            // association and publish it, approving content nobody with authority over the
            // other endpoint ever saw.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.TagReviewer);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string attackerUserId = GetRandomString();
            string victimUserId = GetRandomString();

            Association invalidAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, attackerUserId);

            invalidAssociation.EntityAType = EntityType.BibleReference;
            invalidAssociation.EntityAScope = Scope.ThisVersionOnly;
            invalidAssociation.EntityAContentType = null;
            invalidAssociation.EntityBType = EntityType.Tag;
            invalidAssociation.EntityBScope = Scope.ThisVersionOnly;
            invalidAssociation.EntityBContentType = null;
            invalidAssociation.CreatedBy = victimUserId;

            Association storageAssociation = invalidAssociation.DeepClone();
            storageAssociation.UpdatedWhen = storageAssociation.CreatedWhen;

            invalidAssociation.ApprovalStatus = ApprovalStatus.Approved;
            invalidAssociation.IsPublished = true;
            invalidAssociation.PublishDate = randomDateTimeOffset;

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.ApprovalStatus),
                values: $"Value is not the same as {nameof(Association.ApprovalStatus)}");

            invalidAssociationException.AddData(
                key: nameof(Association.IsPublished),
                values: $"Value is not the same as {nameof(Association.IsPublished)}");

            invalidAssociationException.AddData(
                key: nameof(Association.PublishDate),
                values: $"Date is not the same as {nameof(Association.PublishDate)}");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidAssociationException);

            SetupFailingModifyPathBrokers(
                invalidAssociation, storageAssociation, invalidAssociation.UpdatedBy, randomDateTimeOffset);

            // when
            ValueTask<Association> modifyTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actual =
                await Assert.ThrowsAsync<AssociationValidationException>(modifyTask.AsTask);

            // then
            actual.Should().BeEquivalentTo(expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        public static TheoryData<ApprovalStatus> VerdictApprovalStatuses() =>
            new TheoryData<ApprovalStatus>
            {
                ApprovalStatus.Approved,
                ApprovalStatus.Rejected,
                ApprovalStatus.Dismissed
            };

        [Theory]
        [MemberData(nameof(VerdictApprovalStatuses))]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalStatusIsAVerdictAndLogItAsync(
            ApprovalStatus verdictStatus)
        {
            // given: a verdict is the approval workflow's to record. Without this rule any
            // authenticated caller — no roles at all — could insert a row that is already
            // Approved, skipping the workflow rather than bypassing it.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association invalidAssociation =
                CreateAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            invalidAssociation.ApprovalStatus = verdictStatus;

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.ApprovalStatus),
                values: $"Value must be {nameof(ApprovalStatus.Draft)} " +
                    $"or {nameof(ApprovalStatus.Submitted)} on add");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidAssociationException);

            SetupFailingAddPathBrokers(invalidAssociation, randomUserId, randomDateTimeOffset);

            // when
            ValueTask<Association> addTask =
                this.associationService.AddAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actual =
                await Assert.ThrowsAsync<AssociationValidationException>(addTask.AsTask);

            // then
            actual.Should().BeEquivalentTo(expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfPublicationWasAssertedAndLogItAsync()
        {
            // given: a role-less caller publishing their own row on the way in. This was
            // reachable before the rule landed and is the simplest form of the same hole.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association invalidAssociation =
                CreateAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            invalidAssociation.IsPublished = true;
            invalidAssociation.PublishDate = randomDateTimeOffset;

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.IsPublished),
                values: "Value is not allowed on add");

            invalidAssociationException.AddData(
                key: nameof(Association.PublishDate),
                values: "Date is not allowed on add");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidAssociationException);

            SetupFailingAddPathBrokers(invalidAssociation, randomUserId, randomDateTimeOffset);

            // when
            ValueTask<Association> addTask =
                this.associationService.AddAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actual =
                await Assert.ThrowsAsync<AssociationValidationException>(addTask.AsTask);

            // then
            actual.Should().BeEquivalentTo(expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ── The endpoint veto on every write surface ─────────────────────────────────

        [Fact]
        public async Task ShouldBlockRemoveWhenAnEndpointIsBannedAndLogItAsync()
        {
            // given: the post-read half of the split gate — the half that motivated
            // splitting it. The caller OWNS the row, so the ownership check passes and this
            // veto is the only thing standing between them and the soft delete.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.TagReadOnly);

            string actorUserId = GetRandomString();
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.EntityAType = EntityType.BibleReference;
            storageAssociation.EntityAScope = Scope.ThisVersionOnly;
            storageAssociation.EntityBType = EntityType.Tag;
            storageAssociation.EntityBScope = Scope.ThisVersionOnly;
            storageAssociation.IsDeleted = false;
            storageAssociation.CreatedBy = actorUserId;

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is blocked from contributing content item associations.");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: unauthorizedAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(storageAssociation.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<Association> removeTask =
                this.associationService.RemoveAssociationByIdAsync(
                    storageAssociation.Id,
                    cancellationToken: TestContext.Current.CancellationToken);

            AssociationValidationException actual =
                await Assert.ThrowsAsync<AssociationValidationException>(removeTask.AsTask);

            // then
            actual.Should().BeEquivalentTo(expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldBlockRemoveOfAnAlreadyDeletedRowWhenAnEndpointIsBannedAsync()
        {
            // given: the veto must sit ABOVE the idempotent already-deleted short-circuit,
            // or a blocked caller gets a success response instead of a refusal
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.TagReadOnly);

            string actorUserId = GetRandomString();
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.EntityAType = EntityType.BibleReference;
            storageAssociation.EntityAScope = Scope.ThisVersionOnly;
            storageAssociation.EntityBType = EntityType.Tag;
            storageAssociation.EntityBScope = Scope.ThisVersionOnly;
            storageAssociation.IsDeleted = true;
            storageAssociation.CreatedBy = actorUserId;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(storageAssociation.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<Association> removeTask =
                this.associationService.RemoveAssociationByIdAsync(
                    storageAssociation.Id,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<AssociationValidationException>(removeTask.AsTask);
        }

        [Fact]
        public async Task ShouldBlockHardRemoveWhenAnEndpointIsBannedAndLogItAsync()
        {
            // given: hard removal is the destructive surface and was the only write with no
            // contribution gate at all — a block that stops the reversible takedown but not
            // the irreversible one is the wrong way round
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin, Roles.TagReadOnly);

            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.EntityAType = EntityType.BibleReference;
            storageAssociation.EntityAScope = Scope.ThisVersionOnly;
            storageAssociation.EntityBType = EntityType.Tag;
            storageAssociation.EntityBScope = Scope.ThisVersionOnly;
            storageAssociation.IsDeleted = false;

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is blocked from contributing content item associations.");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: unauthorizedAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(storageAssociation.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociation);

            // when
            ValueTask<Association> hardRemoveTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actual =
                await Assert.ThrowsAsync<AssociationValidationException>(hardRemoveTask.AsTask);

            // then
            actual.Should().BeEquivalentTo(expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteAssociationAsync(It.IsAny<Association>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldBlockModifyWhenAnEndpointIsBannedAndLogItAsync()
        {
            // given: the modify path's endpoint veto, unpinned until now
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.TagReadOnly);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            Association invalidAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);

            invalidAssociation.EntityAType = EntityType.BibleReference;
            invalidAssociation.EntityAScope = Scope.ThisVersionOnly;
            invalidAssociation.EntityBType = EntityType.Tag;
            invalidAssociation.EntityBScope = Scope.ThisVersionOnly;

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is blocked from contributing content item associations.");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: unauthorizedAssociationException);

            // when
            ValueTask<Association> modifyTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actual =
                await Assert.ThrowsAsync<AssociationValidationException>(modifyTask.AsTask);

            // then
            actual.Should().BeEquivalentTo(expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldAllowModifyWhenAScopedReviewRoleMatchesAnEndpointAsync()
        {
            // given: the endpoint-derived WRITE permission. Reverting
            // ValidateUserCanModifyStorageAssociationAsync to the old global-only check must
            // turn this red — a Tag-Reviewer who is not the owner may edit the content of a
            // Tag association, which is the capability this PR exists to grant.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.TagReviewer);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string reviewerUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            Association inputAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, reviewerUserId);

            inputAssociation.EntityAType = EntityType.BibleReference;
            inputAssociation.EntityAScope = Scope.ThisVersionOnly;
            inputAssociation.EntityAContentType = null;
            inputAssociation.EntityBType = EntityType.Tag;
            inputAssociation.EntityBScope = Scope.ThisVersionOnly;
            inputAssociation.EntityBContentType = null;
            inputAssociation.CreatedBy = ownerUserId;

            Association storageAssociation = inputAssociation.DeepClone();
            storageAssociation.UpdatedWhen = storageAssociation.CreatedWhen;

            inputAssociation.ConfidenceReason = GetRandomString();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputAssociation.UpdatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(inputAssociation.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    inputAssociation, storageAssociation))
                    .ReturnsAsync(inputAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(inputAssociation, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inputAssociation);

            // when
            Association actual = await this.associationService.ModifyAssociationAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then
            actual.Should().BeEquivalentTo(inputAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(inputAssociation, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
