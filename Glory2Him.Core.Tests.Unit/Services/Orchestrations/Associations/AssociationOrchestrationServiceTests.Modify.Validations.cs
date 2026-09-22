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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        /// <summary>
        /// PINNED MEANS REFUSED, NOT ABSORBED. §APR7.5.1 rule 4 pins every non-audit property
        /// against storage, and the foundation enforces that with one rule per field feeding
        /// <c>InvalidAssociationException</c> — it does not quietly return the stored value. An
        /// earlier wording of criterion 5, and the XML doc written from it, said the opposite.
        ///
        /// <para>What this proves at THIS layer, which is the part that is this service's: the
        /// refusal is neither swallowed nor re-mapped. It arrives as the foundation's validation
        /// failure and leaves as <c>AssociationOrchestrationDependencyValidationException</c> —
        /// criterion 6's layer split, because the pin is composed from the stored row — with
        /// every offending field still named in <c>Data</c>. The mapping builds a NEW exception
        /// around the foundation's inner, so carrying that <c>Data</c> across is a property of
        /// this service and not of the one below it.</para>
        /// </summary>
        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnModifyIfAPinnedFieldIsChangedAsync()
        {
            // given: a caller who changes several pinned fields at once
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association callerSuppliedAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            callerSuppliedAssociation.EntityAType = EntityType.BibleReference;
            callerSuppliedAssociation.EntityAKeyId = Guid.NewGuid();
            callerSuppliedAssociation.EntityAScope = Scope.ThisVersionOnly;
            callerSuppliedAssociation.EntityAContentType = ContentType.Testimony;
            callerSuppliedAssociation.SortOrder = 999;
            callerSuppliedAssociation.ConfidenceScore = 0.99m;
            callerSuppliedAssociation.IsPublished = true;

            callerSuppliedAssociation.PublishDate =
                new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);

            string[] pinnedFieldNames =
            {
                nameof(Association.EntityAType),
                nameof(Association.EntityAKeyId),
                nameof(Association.EntityAScope),
                nameof(Association.EntityAContentType),
                nameof(Association.SortOrder),
                nameof(Association.ConfidenceScore),
                nameof(Association.IsPublished),
                nameof(Association.PublishDate),
            };

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            foreach (string pinnedFieldName in pinnedFieldNames)
            {
                invalidAssociationException.UpsertDataList(
                    key: pinnedFieldName,
                    value: "Value is not the same as the stored value");
            }

            var associationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationException);

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error " +
                        "occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.associationServiceMock.Setup(service =>
                service.ModifyAssociationAsync(
                    callerSuppliedAssociation,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(associationValidationException);

            // when
            ValueTask<Association> modifyTask =
                this.associationOrchestrationService.ModifyAssociationAsync(
                    callerSuppliedAssociation,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    modifyTask.AsTask);

            // then: refused, not absorbed
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            // and every offending field is still named, so a caller can see WHICH pin refused
            // them rather than only that something did
            foreach (string pinnedFieldName in pinnedFieldNames)
            {
                actualException.InnerException!.Data.Contains(pinnedFieldName)
                    .Should().BeTrue($"the {pinnedFieldName} pin must survive the layer mapping");
            }

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOnModifyIfTheStoredAssociationIsTerminalAsync()
        {
            // given: a stored Approved or Rejected row admits no in-place amendment from anyone —
            // the owner, the publisher tier and Administrators alike — and an association never
            // forks, so refusing the write IS the enforcement (§APR7.5.1 rule 3, §SEC14.7 posture
            // A′ rule 2). The status is read from the STORED row, so the refusal is the
            // foundation's and reaches the caller as a dependency VALIDATION failure: a routine
            // 4xx refusal, never a 424.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association callerSuppliedAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            callerSuppliedAssociation.ApprovalStatus = ApprovalStatus.Draft;

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            var associationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationException);

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error " +
                        "occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.associationServiceMock.Setup(service =>
                service.ModifyAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(associationValidationException);

            // when
            ValueTask<Association> modifyTask =
                this.associationOrchestrationService.ModifyAssociationAsync(
                    callerSuppliedAssociation,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    modifyTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnModifyBeforeReadingTheAssociationIfCallerIsGloballyBlockedAsync()
        {
            // given: the orchestration runs the half of the gate that needs no row —
            // authentication and the global ReadOnly block — and refuses UNAUTHORIZED, a write
            // denial rather than a not-found, with no read against the Associations table at all
            // (§SEC14.7 posture A′ rule 4).
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.ReadOnly);

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association callerSuppliedAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is blocked from contributing content item associations.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<Association> modifyTask =
                this.associationOrchestrationService.ModifyAssociationAsync(
                    callerSuppliedAssociation,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    modifyTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            // nothing at all was asked of the Associations table
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnModifyIfTheCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association callerSuppliedAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is not authenticated.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<Association> modifyTask =
                this.associationOrchestrationService.ModifyAssociationAsync(
                    callerSuppliedAssociation,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    modifyTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfAssociationIsNullAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var nullAssociationOrchestrationException =
                new NullAssociationOrchestrationException(
                    message: "Content item association is null.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: nullAssociationOrchestrationException);

            // when
            ValueTask<Association> modifyTask =
                this.associationOrchestrationService.ModifyAssociationAsync(
                    null,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    modifyTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
