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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByPairIfAssociationIsNullAndLogItAsync()
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
            ValueTask<AssociationRemovalResult> removeTask =
                this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    nullAssociation,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(removeTask.AsTask);

            // then: null is caught before the envelope is even created
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByPairIfThePairIsEditorialAndLogItAsync()
        {
            // given: a suggested tag, from a signed-in caller. Neither endpoint's type is
            // personal, so the pair has no caller in its key, and a withdrawal is keyed on
            // (content item, reaction, caller) (§ARC16.8): it is refused as invalid, before any
            // endpoint or row is read.
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Tag);

            SetupInboundEnvelopeFor(removalRequest);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "An editorial content item association cannot be withdrawn by its pair.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationRemovalResult> removeTask =
                this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Every way a reaction's pair can name an invalid endpoint, each named by what is wrong
        // with it: an empty key on either side, a type outside the enum, and a type no endpoint
        // service resolves.
        public static TheoryData<string> InvalidWithdrawalEndpoints() =>
            new TheoryData<string>
            {
                "an empty A key",
                "an empty B key",
                "an unrecognized endpoint type",
                "an unsupported endpoint type",
            };

        [Theory]
        [MemberData(nameof(InvalidWithdrawalEndpoints))]
        public async Task ShouldThrowValidationExceptionOnRemoveByPairIfAnEndpointIsInvalidAndLogItAsync(
            string invalidEndpoint)
        {
            // given: the withdrawal takes the upsert's caller shape and its structural validation
            // and endpoint resolution, so a malformed pair is refused with the upsert's own
            // validation exception, and no row is looked up
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            switch (invalidEndpoint)
            {
                case "an empty A key":
                    removalRequest.EntityAKeyId = Guid.Empty;

                    invalidAssociationOrchestrationException.AddData(
                        key: nameof(Association.EntityAKeyId),
                        values: "Id is required");

                    break;

                case "an empty B key":
                    removalRequest.EntityBKeyId = Guid.Empty;

                    invalidAssociationOrchestrationException.AddData(
                        key: nameof(Association.EntityBKeyId),
                        values: "Id is required");

                    break;

                case "an unrecognized endpoint type":
                    removalRequest.EntityAType = (EntityType)int.MaxValue;

                    invalidAssociationOrchestrationException.AddData(
                        key: nameof(Association.EntityAType),
                        values: "Value is not a recognized entity type");

                    break;

                case "an unsupported endpoint type":
                    removalRequest.EntityAType = EntityType.Attachment;

                    invalidAssociationOrchestrationException =
                        new InvalidAssociationOrchestrationException(
                            message: "Entity type Attachment is not supported as an association endpoint.");

                    break;
            }

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationRemovalResult> removeTask =
                this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("A")]
        [InlineData("B")]
        public async Task ShouldThrowValidationExceptionOnRemoveByPairIfAnEndpointIsNotFoundAndLogItAsync(
            string missingEndpointName)
        {
            // given: an endpoint's own service reports a row that does not exist, or that the
            // caller may not see, as its validation failure. The resolution the withdrawal shares
            // with the upsert turns that into a not-found endpoint, named by its side, and never
            // re-surfaces the endpoint's own exception type; no row is looked up.
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            SetupMethodPathEndpointReads(removalRequest);

            if (missingEndpointName == "A")
            {
                this.contentItemServiceMock.Setup(service =>
                    service.RetrieveContentItemByIdAsync(
                        removalRequest.EntityAKeyId,
                        TestContext.Current.CancellationToken))
                            .ThrowsAsync(new ContentItemValidationException(
                                message: "Content item validation error occurred, fix the errors and try again.",
                                innerException: new Xeption(message: "Content item not found.")));
            }
            else
            {
                this.reactionServiceMock.Setup(service =>
                    service.RetrieveReactionByIdAsync(
                        removalRequest.EntityBKeyId,
                        TestContext.Current.CancellationToken))
                            .ThrowsAsync(new ReactionValidationException(
                                message: "Reaction validation error occurred, fix the errors and try again.",
                                innerException: new Xeption(message: "Reaction not found.")));
            }

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: $"The {missingEndpointName} endpoint was not found.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAssociationOrchestrationException);

            // when
            ValueTask<AssociationRemovalResult> removeTask =
                this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(removeTask.AsTask);

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
