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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        public static TheoryData<SecurityContext?> UnauthenticatedSecurityContexts() =>
            new TheoryData<SecurityContext?>
            {
                null,
                new SecurityContext { IsAuthenticated = false, Roles = Array.Empty<string>() },
            };

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext!;

            Association rawRequest = CreateRawAddRequest();

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is not authenticated.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(addTask.AsTask);

            // then: refused before any endpoint is read or any row looked up
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfCallerIsBlockedFromContributingAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);

            Association rawRequest = CreateRawAddRequest();

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is blocked from contributing content item associations.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfAssociationIsNullAndLogItAsync()
        {
            // given
            Association nullAssociation = null;

            var nullAssociationOrchestrationException =
                new NullAssociationOrchestrationException(
                    message: "Content item association is null.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: nullAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    nullAssociation,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(addTask.AsTask);

            // then: null is caught before the envelope is even created
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfAnEndpointKeyIsEmptyAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();
            rawRequest.EntityBKeyId = Guid.Empty;

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: nameof(Association.EntityBKeyId),
                values: "Id is required");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(addTask.AsTask);

            // then: rejected before any endpoint is resolved
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfAnEndpointDoesNotExistAndLogItAsync()
        {
            // given: the endpoint's own service reports a missing/non-visible row as a validation
            // failure; the orchestration turns that into a not-found endpoint, never re-surfacing
            // the ContentItem's own exception type.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();

            var contentItemValidationException =
                new ContentItemValidationException(
                    message: "not found",
                    innerException: new Xeption());

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: "The A endpoint was not found.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAssociationOrchestrationException);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    rawRequest.EntityAKeyId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(contentItemValidationException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowNotFoundForTheBEndpointWhenTheSecondEndpointDoesNotExistAndLogItAsync()
        {
            // given: the A endpoint resolves but the B endpoint's service reports it missing. The
            // failure must be attributed to the B endpoint by name (not A), and reported as a
            // not-found endpoint rather than the Tag's own exception type.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    rawRequest.EntityAKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ContentItem
                        {
                            Id = rawRequest.EntityAKeyId,
                            ContentItemGroupId = Guid.NewGuid(),
                            ContentType = ContentType.Story,
                        });

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(
                    rawRequest.EntityBKeyId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new TagValidationException(
                            message: "not found",
                            innerException: new Xeption()));

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: "The B endpoint was not found.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfAnEndpointTypeIsUnsupportedAndLogItAsync()
        {
            // given: Attachment has no foundation service yet — it cannot be an endpoint
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association rawRequest = CreateRawAddRequest();
            rawRequest.EntityAType = EntityType.Attachment;

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Entity type Attachment is not supported as an association endpoint.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationSuggestionResult> addTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(addTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
