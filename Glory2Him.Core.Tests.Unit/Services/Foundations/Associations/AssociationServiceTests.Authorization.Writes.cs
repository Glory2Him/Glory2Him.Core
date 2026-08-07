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
using Glory2Him.Core.Models.Events.Foundations;
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

            // not the generic pin message: the status is guarded by the carve-out rule, which
            // permits Draft <-> Submitted for an eligible caller and refuses everything else,
            // so it reports against the STORED status rather than against a field name
            invalidAssociationException.AddData(
                key: nameof(Association.ApprovalStatus),
                values: "Value is not the same as storage approval status");

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

        public static TheoryData<ApprovalStatus, ApprovalStatus> SubmissionTransitions() =>
            new TheoryData<ApprovalStatus, ApprovalStatus>
            {
                { ApprovalStatus.Draft, ApprovalStatus.Submitted },
                { ApprovalStatus.Submitted, ApprovalStatus.Draft }
            };

        [Theory]
        [MemberData(nameof(SubmissionTransitions))]
        public async Task ShouldWriteTheSubmissionStatusOnModifyWhenTheOwnerMovesItAsync(
            ApprovalStatus storedStatus,
            ApprovalStatus requestedStatus)
        {
            // given: the carve-out's POSITIVE case (design §9.2 rules 4-6). Every other test
            // around the status asserts a refusal, so the carve-out could be deleted outright
            // — pinning the status unconditionally — and the suite would stay green while
            // nobody could submit anything for review.
            //
            // The owner is what unlocks it: ValidateUserCanModifyStorageAssociationAsync
            // returns the carve-out flag out of the same ownership check it already performs.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string ownerUserId = GetRandomString();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association inputAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, ownerUserId);

            inputAssociation.CreatedBy = ownerUserId;
            inputAssociation.ApprovalStatus = storedStatus;

            Association storageAssociation = inputAssociation.DeepClone();
            storageAssociation.UpdatedWhen = storageAssociation.CreatedWhen;

            inputAssociation.ApprovalStatus = requestedStatus;

            Association savedAssociation = null;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(ownerUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    inputAssociation.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    inputAssociation, storageAssociation))
                        .ReturnsAsync(inputAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association entity, CancellationToken _) =>
                        {
                            savedAssociation = entity.DeepClone();

                            return entity;
                        });

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Modified))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            // when
            Association actual = await this.associationService.ModifyAssociationAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then: the row actually SAVED carries the new status — asserting only on the
            // returned entity would pass on a service that validated the move and then
            // dropped it
            savedAssociation.Should().NotBeNull();
            savedAssociation.ApprovalStatus.Should().Be(requestedStatus);
            actual.ApprovalStatus.Should().Be(requestedStatus);

            // the carve-out moves the status and nothing else — publication is approve's
            savedAssociation.IsPublished.Should().Be(storageAssociation.IsPublished);
            savedAssociation.PublishDate.Should().Be(storageAssociation.PublishDate);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(
                    inputAssociation, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfANonOwnerMovesTheSubmissionStatusAsync()
        {
            // given: the other half of the carve-out. A Tag-Reviewer holds write permission on
            // the row and may amend it, and must still never move the status (§8.6 HR-3) — so
            // the flag has to come from OWNERSHIP, not from passing the write gate.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.TagReviewer);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string reviewerUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            Association invalidAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, reviewerUserId);

            invalidAssociation.EntityAType = EntityType.BibleReference;
            invalidAssociation.EntityAScope = Scope.ThisVersionOnly;
            invalidAssociation.EntityAContentType = null;
            invalidAssociation.EntityBType = EntityType.Tag;
            invalidAssociation.EntityBScope = Scope.ThisVersionOnly;
            invalidAssociation.EntityBContentType = null;
            invalidAssociation.CreatedBy = ownerUserId;
            invalidAssociation.ApprovalStatus = ApprovalStatus.Draft;

            Association storageAssociation = invalidAssociation.DeepClone();
            storageAssociation.UpdatedWhen = storageAssociation.CreatedWhen;

            invalidAssociation.ApprovalStatus = ApprovalStatus.Submitted;

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.ApprovalStatus),
                values: "Value is not the same as storage approval status");

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

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        public async Task ShouldAcceptAContributableApprovalStatusOnAddAsync(
            ApprovalStatus contributableStatus)
        {
            // given: the positive half of the rule. Design §9.7.1 rule 1 says a row is written
            // with "the ApprovalStatus the caller asked for — Submitted on the common path,
            // Draft when saving work in progress", so narrowing the rule to Draft-only would
            // break the documented common path. Without this test that narrowing is invisible.
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association inputAssociation =
                CreateAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            inputAssociation.ApprovalStatus = contributableStatus;

            SetupAddPathBrokers(inputAssociation, randomDateTimeOffset);

            // when
            Association actualAssociation =
                await this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.ApprovalStatus.Should().Be(contributableStatus);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(inputAssociation, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // ── The endpoint veto on every write surface ─────────────────────────────────

        [Fact]
        public async Task ShouldBlockHardRemoveWhenTheCallerIsGloballyReadOnlyAndLogItAsync()
        {
            // given: the global half of the hard-remove gate. The endpoint half is pinned by
            // the Tag-ReadOnly test below, but "Tag-ReadOnly" is a different string from
            // "ReadOnly" — so that test is caught by the endpoint veto and says nothing about
            // this branch. Without this, the site-wide contribution ban could be dropped from
            // the destructive surface with the suite green.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin, Roles.ReadOnly);

            Guid someAssociationId = Guid.NewGuid();

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is blocked from contributing content item associations.");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: unauthorizedAssociationException);

            // when
            ValueTask<Association> hardRemoveTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actual =
                await Assert.ThrowsAsync<AssociationValidationException>(hardRemoveTask.AsTask);

            // then: rejected before the row is even read
            actual.Should().BeEquivalentTo(expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteAssociationAsync(It.IsAny<Association>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

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

            // nothing on the entity is altered, and that is not an oversight: every non-audit
            // field an Association carries now belongs to a narrow operation and is pinned
            // against storage here. What this test asserts is the GATE — that a Tag-Reviewer
            // who is not the owner is admitted to the modify path at all.
            Association expectedAssociation = inputAssociation.DeepClone();

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

            // then: against a clone taken before the act, not against the instance the mocks
            // hand back — comparing that instance with itself would pass however the service
            // mangled it
            actual.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(inputAssociation, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
