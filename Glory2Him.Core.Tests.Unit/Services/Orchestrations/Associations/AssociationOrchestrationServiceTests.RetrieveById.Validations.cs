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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
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

        /// <summary>
        /// §SEC14.5 rule 1's test is what an unprivileged probe can tell apart, and a probe sees
        /// a status code and a body. All five misses therefore carry the SAME outward message,
        /// with no reason, no state and no identity in it.
        ///
        /// <para>The two exception families stay two and are asserted here as such: misses 1-3
        /// are the foundation's own not-found and leave as a dependency validation failure,
        /// misses 4-5 are raised locally and leave as a validation failure. Collapsing them would
        /// misreport which layer refused; #318 maps both to one status code and one body.</para>
        ///
        /// <para><b>What this pins and what it does not.</b> The foundation's message template is
        /// written out once below and used both to drive the simulated foundation misses and as
        /// the expected value of the two this service raises itself — so it pins THIS service's
        /// messages to the foundation's. It would not catch the foundation changing its own
        /// wording, which is not this service's to pin. Misses 1-3 are simulated as one shape
        /// because that is what they are by the time they reach this layer: the foundation
        /// converges its three reasons onto one message before throwing, which is the whole point
        /// of rule 1.</para>
        /// </summary>
        [Fact]
        public async Task ShouldCarryTheSameOutwardMessageForEveryMissOnRetrieveByIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var noRowId = Guid.NewGuid();
            var softDeletedRowId = Guid.NewGuid();
            var notVisibleToCallerId = Guid.NewGuid();

            Guid[] foundationMissIds = { noRowId, softDeletedRowId, notVisibleToCallerId };

            foreach (Guid foundationMissId in foundationMissIds)
            {
                Guid missId = foundationMissId;

                this.associationServiceMock.Setup(service =>
                    service.RetrieveAssociationByIdAsync(missId, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new AssociationValidationException(
                            message: "Content item association validation error occurred, " +
                                "fix the errors and try again.",
                            innerException: new NotFoundAssociationException(
                                message: FoundationNotFoundMessageFor(missId))));
            }

            // miss 4 — the row is visible, its endpoint is not
            ContentItem invisibleEndpointContentItem = CreateEndpointContentItem();
            Tag sharedTag = CreateEndpointTag();

            Association associationOnAnInvisibleEndpoint =
                CreateStoredAssociation(invisibleEndpointContentItem, sharedTag);

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    associationOnAnInvisibleEndpoint.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(associationOnAnInvisibleEndpoint);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemsByGroupIdAsync(
                    associationOnAnInvisibleEndpoint.EntityAGroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<ContentItem>());

            // miss 5 — the row is visible, its endpoint type has no foundation service
            var associationOnAnUnreadableType = new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = EntityType.Attachment,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = Guid.NewGuid(),
                EntityAScope = Scope.ThisVersionOnly,
                EntityBType = EntityType.Tag,
                EntityBKeyId = sharedTag.Id,
                EntityBGroupId = sharedTag.Id,
                EntityBScope = Scope.ThisVersionOnly,
            };

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    associationOnAnUnreadableType.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(associationOnAnUnreadableType);

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(sharedTag.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sharedTag);

            // when
            Xeption noRowMiss = await CaptureRetrieveByIdMissAsync(noRowId);
            Xeption softDeletedMiss = await CaptureRetrieveByIdMissAsync(softDeletedRowId);
            Xeption notVisibleMiss = await CaptureRetrieveByIdMissAsync(notVisibleToCallerId);

            Xeption invisibleEndpointMiss =
                await CaptureRetrieveByIdMissAsync(associationOnAnInvisibleEndpoint.Id);

            Xeption unreadableTypeMiss =
                await CaptureRetrieveByIdMissAsync(associationOnAnUnreadableType.Id);

            // then: one message, five misses
            noRowMiss.InnerException!.Message.Should().Be(FoundationNotFoundMessageFor(noRowId));

            softDeletedMiss.InnerException!.Message.Should()
                .Be(FoundationNotFoundMessageFor(softDeletedRowId));

            notVisibleMiss.InnerException!.Message.Should()
                .Be(FoundationNotFoundMessageFor(notVisibleToCallerId));

            invisibleEndpointMiss.InnerException!.Message.Should()
                .Be(FoundationNotFoundMessageFor(associationOnAnInvisibleEndpoint.Id));

            unreadableTypeMiss.InnerException!.Message.Should()
                .Be(FoundationNotFoundMessageFor(associationOnAnUnreadableType.Id));

            // the families stay two, and which layer refused is what they record
            noRowMiss.Should().BeOfType<AssociationOrchestrationDependencyValidationException>();
            softDeletedMiss.Should().BeOfType<AssociationOrchestrationDependencyValidationException>();
            notVisibleMiss.Should().BeOfType<AssociationOrchestrationDependencyValidationException>();
            invisibleEndpointMiss.Should().BeOfType<AssociationOrchestrationValidationException>();
            unreadableTypeMiss.Should().BeOfType<AssociationOrchestrationValidationException>();

            // nothing anywhere in any of them names a reason, a state or an identity
            Xeption[] allMisses =
            {
                noRowMiss, softDeletedMiss, notVisibleMiss, invisibleEndpointMiss, unreadableTypeMiss,
            };

            string[] disclosures =
            {
                "endpoint", "Entity type", "not supported", "visible", "deleted",
                "role", "authenticated", "owner",
            };

            foreach (Xeption miss in allMisses)
            {
                foreach (string disclosure in disclosures)
                {
                    miss.Message.Should().NotContain(disclosure);
                    miss.InnerException!.Message.Should().NotContain(disclosure);
                }

                miss.Data.Count.Should().Be(0);
                miss.InnerException!.Data.Count.Should().Be(0);
            }
        }

        // The foundation's own not-found wording, in one place. The id in it is the caller's own
        // input, which §SEC14.5 rule 2 does not bar — it is neither the reason, the state, nor an
        // identity.
        private static string FoundationNotFoundMessageFor(Guid associationId) =>
            $"Content item association not found with id: {associationId}.";

        private async ValueTask<Xeption> CaptureRetrieveByIdMissAsync(Guid associationId)
        {
            try
            {
                await this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    associationId,
                    TestContext.Current.CancellationToken);
            }
            catch (Xeption miss)
            {
                return miss;
            }

            throw new InvalidOperationException(
                $"Expected a miss for association {associationId} and the read returned a row.");
        }

        [Theory]
        [InlineData(EntityType.Attachment)]
        [InlineData(EntityType.Association)]
        public async Task ShouldThrowNotFoundOnRetrieveByIdIfAnEndpointTypeHasNoFoundationServiceAsync(
            EntityType unsupportedEndpointType)
        {
            // given: on the ADD the caller supplied the endpoint type, so refusing it by name
            // discloses only their own input and stays an ordinary validation failure. HERE the
            // caller supplied an association id and nothing else, so the same sentence — "Entity
            // type Attachment is not supported as an association endpoint" — confirms the row
            // exists and reports one of its columns: the entity's state under §SEC14.5 rule 2,
            // and a miss a probe can tell from the other four under rule 1.
            //
            // An endpoint type with no foundation service also cannot satisfy §SEC14.3 rule 4 —
            // there is no read that could show it visible — and an undecidable visibility input
            // fails closed. So the row is not visible, and this answers not-found on the same
            // terms as any other unresolvable endpoint, matching the collection read, which
            // already drops it.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Tag tagEndpoint = CreateEndpointTag();

            var storedAssociation = new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = unsupportedEndpointType,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = Guid.NewGuid(),
                EntityAScope = Scope.ThisVersionOnly,
                EntityBType = EntityType.Tag,
                EntityBKeyId = tagEndpoint.Id,
                EntityBGroupId = tagEndpoint.Id,
                EntityBScope = Scope.ThisVersionOnly,
            };

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storedAssociation);

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(
                    tagEndpoint.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(tagEndpoint);

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: FoundationNotFoundMessageFor(storedAssociation.Id));

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

            // nothing in the answer names the type, or says it is unsupported
            actualException.InnerException!.Message.Should()
                .NotContain(unsupportedEndpointType.ToString());

            actualException.InnerException.Message.Should().NotContain("not supported");
            actualException.InnerException.Message.Should().NotContain("Entity type");
            actualException.Data.Count.Should().Be(0);
            actualException.InnerException.Data.Count.Should().Be(0);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Scope.AllVersions)]
        [InlineData(Scope.ThisVersionOnly)]
        public async Task ShouldThrowNotFoundRatherThanDependencyOnRetrieveByIdIfAnEndpointIsNotVisibleAsync(
            Scope contentItemEndpointScope)
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

            storedAssociation.EntityAScope = contentItemEndpointScope;

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storedAssociation);

            // Each scope's own way of saying "not visible", in that foundation's own terms: an
            // empty group slice, which the group-keyed read returns when the group holds nothing
            // this caller may see, and a validation-shaped not-found from the by-id read.
            if (contentItemEndpointScope == Scope.AllVersions)
            {
                this.contentItemServiceMock.Setup(service =>
                    service.RetrieveContentItemsByGroupIdAsync(
                        storedAssociation.EntityAGroupId,
                        It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new List<ContentItem>());
            }
            else
            {
                this.contentItemServiceMock.Setup(service =>
                    service.RetrieveContentItemByIdAsync(
                        storedAssociation.EntityAKeyId,
                        It.IsAny<CancellationToken>()))
                            .ThrowsAsync(new ContentItemValidationException(
                                message: "not found",
                                innerException: new Xeption()));
            }

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: FoundationNotFoundMessageFor(storedAssociation.Id));

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
            // WHICH endpoint refused, which on this path the caller never supplied — and it is
            // word for word the answer the foundation gives its own three misses
            actualException.InnerException!.Message.Should()
                .Be(FoundationNotFoundMessageFor(storedAssociation.Id));

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
