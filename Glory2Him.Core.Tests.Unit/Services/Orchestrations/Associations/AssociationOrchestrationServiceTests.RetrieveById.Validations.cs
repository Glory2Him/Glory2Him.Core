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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsEmptyAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.UpsertDataList(
                key: nameof(Association.Id),
                value: "Id is required");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<Association> retrieveTask =
                this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.RetrieveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnRetrieveByIdIfTheAssociationIsNotVisibleToTheCallerAsync()
        {
            // given: the row is absent, soft-deleted or outside this caller's posture — the
            // foundation cannot say which to the caller and does not (§SEC14.5 rules 1 and 2). It
            // answers not-found, and the orchestration carries that answer up as a routine
            // refusal, never as a dependency error and never as unauthorized.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid someAssociationId = Guid.NewGuid();

            var notFoundAssociationException =
                new NotFoundAssociationException(
                    message: $"Content item association not found with id: {someAssociationId}.");

            var associationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAssociationException);

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error " +
                        "occurred, fix the errors and try again.",
                    innerException: notFoundAssociationException);

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(associationValidationException);

            // when
            ValueTask<Association> retrieveTask =
                this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    retrieveTask.AsTask);

            // then: the not-found is what reaches the caller, and no endpoint was ever consulted
            // for a row that was never handed over
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);
            actualException.InnerException.Should().BeOfType<NotFoundAssociationException>();

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowNotFoundRatherThanDependencyOnRetrieveByIdIfAnEndpointIsNotVisibleAsync()
        {
            // given: the association row is visible to this caller but one of its endpoints is
            // not. Answering that with a 424 would report a visibility rule as a failed
            // dependency and leak through the status code exactly what §SEC14.5 rule 2 keeps out
            // of the message — so the endpoint service's validation failure is converted at the
            // resolution site into a not-found, the conversion the add path already performs.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association storedAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storedAssociation);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    storedAssociation.EntityAKeyId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new ContentItemValidationException(
                            message: "not found",
                            innerException: new Xeption()));

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: "Content item association not found.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAssociationOrchestrationException);

            // when
            ValueTask<Association> retrieveTask =
                this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            // the caller-facing answer names no reason, no state and no identity — not even
            // WHICH endpoint refused, which on this path the caller never supplied
            actualException.InnerException!.Message.Should()
                .Be("Content item association not found.");

            actualException.InnerException.Message.Should().NotContain("endpoint");
            actualException.Data.Count.Should().Be(0);
            actualException.InnerException.Data.Count.Should().Be(0);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
