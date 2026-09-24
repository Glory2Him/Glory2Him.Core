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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    // THE ASSOCIATION-ADDING EVENT PATH (#631). The address binds this orchestration rather than
    // the foundation, because its handler derives Entity{A,B}ContentType — an authorization input
    // — from the endpoints it resolves. The foundation keeps everything else: these pin that the
    // envelope reaches it unchanged, and that nothing the method path gates is walked past.
    public partial class AssociationOrchestrationServiceTests
    {
        // THE SAME ENVELOPE, NOT A REBUILT ONE. The foundation's deduplication, audit stamping,
        // write, fact and reply all key on the identity and causation this envelope carries, and
        // its signature covers the content — so the reference handed down must be the one that
        // arrived, and its content must be exactly what the publisher signed.
        [Fact]
        public async Task ShouldDelegateTheUnchangedEnvelopeToTheFoundationOnTheEventPathAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            Association expectedContent = addRequest.DeepClone();
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            EventEnvelope<Association> expectedReplyEnvelope =
                CreateRequestEnvelope(addRequest.DeepClone());

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(expectedReplyEnvelope);

            // when
            EventEnvelope<Association> actualReplyEnvelope =
                await this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().BeSameAs(expectedReplyEnvelope);
            inputEnvelope.Content.Should().BeEquivalentTo(expectedContent);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    TestContext.Current.CancellationToken),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Association>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // THE DERIVATION RUNS ON THE EVENT PATH, and in the right place: after the duplicate
        // question, before the foundation is handed the envelope. Both orderings are SNAPSHOTTED
        // while the call is happening — Moq evaluates matchers at Verify time, so a read moved to
        // AFTER the delegation, which reopens the whole gap, would still satisfy a plain Verify.
        [Fact]
        public async Task ShouldDeriveEndpointsOnTheEventPathAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);
            bool wereEndpointsReadBeforeTheDuplicateQuestion = true;
            bool wereBothEndpointsReadBeforeDelegating = false;

            this.associationServiceMock.Setup(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .Callback<EventEnvelope<Association>, CancellationToken>((_, _) =>
                            wereEndpointsReadBeforeTheDuplicateQuestion =
                                this.contentItemServiceMock.Invocations.Count > 0
                                    || this.tagServiceMock.Invocations.Count > 0)
                        .ReturnsAsync(false);

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .Callback<EventEnvelope<Association>, CancellationToken>((_, _) =>
                            wereBothEndpointsReadBeforeDelegating =
                                this.contentItemServiceMock.Invocations.Count > 0
                                    && this.tagServiceMock.Invocations.Count > 0)
                        .ReturnsAsync(inputEnvelope);

            // when
            await this.associationOrchestrationService.OnAddingAssociationAsync(
                inputEnvelope,
                TestContext.Current.CancellationToken);

            // then
            wereEndpointsReadBeforeTheDuplicateQuestion.Should().BeFalse();
            wereBothEndpointsReadBeforeDelegating.Should().BeTrue();

            this.associationServiceMock.Verify(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    addRequest.EntityAKeyId,
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.tagServiceMock.Verify(service =>
                service.RetrieveTagByIdAsync(
                    addRequest.EntityBKeyId,
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Every endpoint type the resolver supports, on side B. Side A stays a ContentItem, so
        // each row also exercises the content-typed read alongside the one under test.
        public static TheoryData<EntityType> SupportedEndpointTypes() =>
            new TheoryData<EntityType>
            {
                EntityType.ContentItem,
                EntityType.Link,
                EntityType.Tag,
                EntityType.Reaction,
                EntityType.BibleReference,
                EntityType.Comment,
            };

        // THE READS ARE THE SIGNED CALLER'S (§ARC12.5.2, "a read whose answer depends on who is
        // asking is passed the envelope it is being made under"). Delivery is synchronous inside
        // a publish and HttpContextAccessor flows on an AsyncLocal, so an ambient read on this
        // path inherits whoever PUBLISHED — wrong in the permissive direction. Asserted per
        // endpoint type, and as "no ambient read happened at all", because one branch left on the
        // ambient overload is the whole defect for every association that names that type.
        [Theory]
        [MemberData(nameof(SupportedEndpointTypes))]
        public async Task ShouldCarryTheInboundEnvelopeIntoTheEndpointReadsOnTheEventPathAsync(
            EntityType endpointBType)
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            addRequest.EntityBType = endpointBType;

            addRequest.EntityBContentType =
                endpointBType == EntityType.ContentItem ? ContentType.Story : null;

            addRequest.EntityBGroupId =
                endpointBType is EntityType.ContentItem or EntityType.Link
                    ? Guid.NewGuid()
                    : Guid.Empty;

            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            SetupEventPathEndpointRead(
                endpointBType,
                addRequest.EntityBKeyId,
                addRequest.EntityBGroupId,
                inputEnvelope);

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(inputEnvelope);

            // when
            await this.associationOrchestrationService.OnAddingAssociationAsync(
                inputEnvelope,
                TestContext.Current.CancellationToken);

            // then
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    addRequest.EntityAKeyId,
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            VerifyEventPathEndpointRead(endpointBType, addRequest.EntityBKeyId, inputEnvelope);
            VerifyNoAmbientEndpointRead();

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A versioned endpoint resolves to the group it is handed; a non-versioned one has none.
        private void SetupEventPathEndpointRead(
            EntityType entityType,
            Guid keyId,
            Guid groupId,
            EventEnvelope<Association> inboundEnvelope)
        {
            switch (entityType)
            {
                case EntityType.ContentItem:
                    this.contentItemServiceMock.Setup(service =>
                        service.RetrieveContentItemByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken))
                                .ReturnsAsync(new ContentItem
                                {
                                    Id = keyId,
                                    GroupId = groupId,
                                    ContentType = ContentType.Story,
                                });

                    return;

                case EntityType.Link:
                    this.linkServiceMock.Setup(service =>
                        service.RetrieveLinkByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken))
                                .ReturnsAsync(new Link { Id = keyId, GroupId = groupId });

                    return;

                case EntityType.Tag:
                    this.tagServiceMock.Setup(service =>
                        service.RetrieveTagByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken))
                                .ReturnsAsync(new Tag { Id = keyId });

                    return;

                case EntityType.Reaction:
                    this.reactionServiceMock.Setup(service =>
                        service.RetrieveReactionByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken))
                                .ReturnsAsync(new Reaction { Id = keyId });

                    return;

                case EntityType.BibleReference:
                    this.bibleReferenceServiceMock.Setup(service =>
                        service.RetrieveBibleReferenceByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken))
                                .ReturnsAsync(new BibleReference { Id = keyId });

                    return;

                case EntityType.Comment:
                    this.commentServiceMock.Setup(service =>
                        service.RetrieveCommentByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken))
                                .ReturnsAsync(new Comment { Id = keyId });

                    return;
            }
        }

        private void VerifyEventPathEndpointRead(
            EntityType entityType,
            Guid keyId,
            EventEnvelope<Association> inboundEnvelope)
        {
            switch (entityType)
            {
                case EntityType.ContentItem:
                    this.contentItemServiceMock.Verify(service =>
                        service.RetrieveContentItemByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken),
                        Times.Once);

                    return;

                case EntityType.Link:
                    this.linkServiceMock.Verify(service =>
                        service.RetrieveLinkByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken),
                        Times.Once);

                    return;

                case EntityType.Tag:
                    this.tagServiceMock.Verify(service =>
                        service.RetrieveTagByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken),
                        Times.Once);

                    return;

                case EntityType.Reaction:
                    this.reactionServiceMock.Verify(service =>
                        service.RetrieveReactionByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken),
                        Times.Once);

                    return;

                case EntityType.BibleReference:
                    this.bibleReferenceServiceMock.Verify(service =>
                        service.RetrieveBibleReferenceByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken),
                        Times.Once);

                    return;

                case EntityType.Comment:
                    this.commentServiceMock.Verify(service =>
                        service.RetrieveCommentByIdAsync(
                            keyId, inboundEnvelope, TestContext.Current.CancellationToken),
                        Times.Once);

                    return;
            }
        }

        // The ambient overloads mint their own envelope; on the event path not one may be used.
        private void VerifyNoAmbientEndpointRead()
        {
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.linkServiceMock.Verify(service =>
                service.RetrieveLinkByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.tagServiceMock.Verify(service =>
                service.RetrieveTagByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.reactionServiceMock.Verify(service =>
                service.RetrieveReactionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.bibleReferenceServiceMock.Verify(service =>
                service.RetrieveBibleReferenceByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.commentServiceMock.Verify(service =>
                service.RetrieveCommentByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // A REPLAY DOES NO WORK. Deduplication belongs to the foundation, and this handler now
        // runs AHEAD of it — so without the early question a re-delivered envelope would read
        // both endpoint rows before anything noticed the event was already applied. If an
        // endpoint has since been soft-deleted, or stopped being visible to the signed caller,
        // that read fails and a settled write is recorded as a failed delivery and retried.
        //
        // Asserted as "no endpoint was read and the foundation handler was never called",
        // because a short-circuit that still pays for the reads is the bug half-fixed.
        [Fact]
        public async Task ShouldShortCircuitADuplicateBeforeResolvingEndpointsOnTheEventPathAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            this.associationServiceMock.Setup(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Association> actualReplyEnvelope =
                await this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().BeNull();

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "AssociationAdding",
                    EnvelopeDirection.Request),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            // the derivation never ran, so a since-deleted endpoint cannot fail a settled replay,
            // and the foundation handler was never reached
            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
            this.commentServiceMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
