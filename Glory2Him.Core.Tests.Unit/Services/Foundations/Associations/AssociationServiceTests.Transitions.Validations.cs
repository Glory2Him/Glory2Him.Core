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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfAssociationIsNullAsync()
        {
            // given / when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.ApproveAssociationAsync(
                    null, TestContext.Current.CancellationToken));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnApproveIfStatusIsNotAnOutcomeAsync(
            ApprovalStatus notAnOutcome)
        {
            // given: approve owns the whole of IApproval, which makes it the one place these
            // values can be set — so the set it accepts has to be closed. Draft and Submitted
            // are states a row LEAVES here, and Dismissed belongs to a later withdrawal step.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);

            Association decision = CreateApprovalDecision(Guid.NewGuid());
            decision.ApprovalStatus = notAnOutcome;
            decision.IsPublished = false;
            decision.PublishDate = null;

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.ApproveAssociationAsync(
                    decision, TestContext.Current.CancellationToken));

            // refused before storage was touched
            this.storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfPublishedWithoutApprovalAsync()
        {
            // given: publication is a consequence of approval, so a rejected row cannot be
            // published in the same write
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);

            Association decision = CreateRejectionDecision(Guid.NewGuid());
            decision.IsPublished = true;

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.ApproveAssociationAsync(
                    decision, TestContext.Current.CancellationToken));

            this.storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfPublishDateWithoutPublicationAsync()
        {
            // given: a publish date on an unpublished row is a date nothing reads
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);

            Association decision = CreateRejectionDecision(Guid.NewGuid());
            decision.PublishDate = GetRandomDateTimeOffset();

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.ApproveAssociationAsync(
                    decision, TestContext.Current.CancellationToken));

            this.storageBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnApproveIfStoredRowIsNotInReviewAsync(
            ApprovalStatus storedStatus)
        {
            // given: only a row actually in review can be decided. Approving a Draft would skip
            // the submission the whole workflow is built around.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);

            Association storageAssociation = CreateStorageAssociationInStatus(storedStatus);
            Association decision = CreateApprovalDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.ApproveAssociationAsync(
                    decision, TestContext.Current.CancellationToken));

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfCallerHasNoReviewRoleAsync()
        {
            // given: a caller with no review role at all
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association decision = CreateApprovalDecision(storageAssociation.Id);

            SetupStorageRead(storageAssociation);

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.ApproveAssociationAsync(
                    decision, TestContext.Current.CancellationToken));

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApproveIfAssociationIsNotFoundAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            Association decision = CreateApprovalDecision(Guid.NewGuid());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    decision.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association)null);

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.ApproveAssociationAsync(
                    decision, TestContext.Current.CancellationToken));
        }

        // Every transition is a write, so every transition is subject to the global block. A
        // globally read-only caller may not add, modify or remove anything anywhere — and a
        // state transition is not an exception to that just because it touches one field.
        public static TheoryData<string> TransitionNames() =>
            new TheoryData<string>
            {
                "Submit", "Approve", "Sort", "SetConfidence", "SetScope"
            };

        [Theory]
        [MemberData(nameof(TransitionNames))]
        public async Task ShouldThrowValidationExceptionOnTransitionIfCallerIsGloballyReadOnlyAsync(
            string transitionName)
        {
            // given: the global veto sits ABOVE every role, so even an Admin holding it is
            // refused — and refused before storage is read at all
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.Admin, Roles.Publisher, Roles.Reviewer, Roles.ReadOnly);

            Association storageAssociation = CreateApprovableStorageAssociation();
            SetupStorageRead(storageAssociation);

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await InvokeTransitionAsync(transitionName, storageAssociation));

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectAssociationByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmitIfStoredRowIsNotDraftAsync()
        {
            // given: re-submitting a row already in review would reset a review in flight
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation =
                CreateStorageAssociationInStatus(ApprovalStatus.Submitted);

            SetupStorageRead(storageAssociation);
            SetupActor(GetRandomString());

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.SubmitAssociationAsync(
                    new Association { Id = storageAssociation.Id },
                    TestContext.Current.CancellationToken));

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSetConfidenceIfCallerIsTheOwnerAsync()
        {
            // given: THE point of the operation. A contributor who could set the confidence in
            // their own association to 10 defeats scoring entirely — this is the one gate in
            // the service where being the owner makes a caller LESS able, not more.
            string actorUserId = GetRandomString();

            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher, Roles.Admin);

            Association storageAssociation = CreateSubmittableStorageAssociation();
            storageAssociation.CreatedBy = actorUserId;

            SetupStorageRead(storageAssociation);
            SetupActor(actorUserId);

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.SetAssociationConfidenceAsync(
                    CreateConfidenceDecision(storageAssociation.Id),
                    TestContext.Current.CancellationToken));

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSetConfidenceIfProvenanceIsPartialAsync()
        {
            // given: a score attributed to a model with no batch behind it cannot be retracted
            // by a sweep over that batch
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association decision = CreateConfidenceDecision(Guid.NewGuid());
            decision.SourceBatchId = null;

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.SetAssociationConfidenceAsync(
                    decision, TestContext.Current.CancellationToken));

            this.storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSetScopeIfCallerIsNotPublisherOrAdminAsync()
        {
            // given: this restriction is load-bearing, not policy. A scope change does not
            // re-open approval, and the stated reason it need not is that only the people who
            // would be re-approving it can make one. A Reviewer is deliberately not enough.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);

            Association storageAssociation = CreateSubmittableStorageAssociation();
            SetupStorageRead(storageAssociation);

            Association decision = new Association
            {
                Id = storageAssociation.Id,
                EntityAScope = storageAssociation.EntityAScope,
                EntityBScope = storageAssociation.EntityBScope
            };

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.SetAssociationScopeAsync(
                    decision, TestContext.Current.CancellationToken));

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSetScopeIfThePairIsAlreadyOccupiedAsync()
        {
            // given: a scope toggle recomputes the effective id, which moves the row's position
            // in UX_Associations_Pair and can land it on a key another row already holds.
            // "Just toggle a flag" reads like it cannot fail, and it can.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateSubmittableStorageAssociation();
            storageAssociation.EntityAScope = Scope.ThisVersionOnly;
            SetupStorageRead(storageAssociation);

            // the row already sitting on the key the toggle would move onto: same endpoint
            // types, same user, and AllVersions on A so its effective id is the group id
            Association occupyingAssociation = CreateRandomAssociation();
            occupyingAssociation.IsDeleted = false;
            occupyingAssociation.EntityAType = storageAssociation.EntityAType;
            occupyingAssociation.EntityBType = storageAssociation.EntityBType;
            occupyingAssociation.UserId = storageAssociation.UserId;
            occupyingAssociation.EntityAScope = Scope.AllVersions;
            occupyingAssociation.EntityAGroupId = storageAssociation.EntityAGroupId;
            occupyingAssociation.EntityBScope = storageAssociation.EntityBScope;
            occupyingAssociation.EntityBGroupId = storageAssociation.EntityBGroupId;
            occupyingAssociation.EntityBKeyId = storageAssociation.EntityBKeyId;
            WithDatabaseComputedEffectiveIds(occupyingAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Association> { occupyingAssociation }.AsQueryable());

            Association decision = new Association
            {
                Id = storageAssociation.Id,
                EntityAScope = Scope.AllVersions,
                EntityBScope = storageAssociation.EntityBScope
            };

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.SetAssociationScopeAsync(
                    decision, TestContext.Current.CancellationToken));

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSortIfTheAnchorIsTheAssociationItselfAsync()
        {
            // given: positioning a row relative to itself is a no-op the caller did not mean
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid associationId = Guid.NewGuid();

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.SortAssociationAsync(
                    new Association { Id = associationId },
                    new Association { Id = associationId },
                    SortPosition.After,
                    TestContext.Current.CancellationToken));

            this.storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSortIfTheAnchorHasNoSortOrderAsync()
        {
            // given: an unpositioned anchor gives nothing to position relative to
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);

            Association storageAssociation = CreateSubmittableStorageAssociation();
            Association anchorAssociation = CreateRandomAssociation();
            anchorAssociation.SortOrder = null;

            SetupStorageRead(storageAssociation);
            SetupStorageRead(anchorAssociation);

            // when / then
            await Assert.ThrowsAsync<AssociationValidationException>(async () =>
                await this.associationService.SortAssociationAsync(
                    new Association { Id = storageAssociation.Id },
                    new Association { Id = anchorAssociation.Id },
                    SortPosition.Before,
                    TestContext.Current.CancellationToken));

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private void SetupStorageRead(Association storageAssociation) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssociation);

        private void SetupActor(string actorUserId) =>
            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

        private ValueTask<Association> InvokeTransitionAsync(
            string transitionName,
            Association storageAssociation)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            return transitionName switch
            {
                "Submit" => this.associationService.SubmitAssociationAsync(
                    new Association { Id = storageAssociation.Id }, cancellationToken),

                "Approve" => this.associationService.ApproveAssociationAsync(
                    CreateApprovalDecision(storageAssociation.Id), cancellationToken),

                "Sort" => this.associationService.SortAssociationAsync(
                    new Association { Id = storageAssociation.Id },
                    new Association { Id = Guid.NewGuid() },
                    SortPosition.After,
                    cancellationToken),

                "SetConfidence" => this.associationService.SetAssociationConfidenceAsync(
                    CreateConfidenceDecision(storageAssociation.Id), cancellationToken),

                _ => this.associationService.SetAssociationScopeAsync(
                    new Association
                    {
                        Id = storageAssociation.Id,
                        EntityAScope = storageAssociation.EntityAScope,
                        EntityBScope = storageAssociation.EntityBScope
                    },
                    cancellationToken)
            };
        }
    }
}
