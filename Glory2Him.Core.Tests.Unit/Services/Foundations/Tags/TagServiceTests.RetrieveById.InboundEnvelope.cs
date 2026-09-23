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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        // THE ENDPOINT READ ON THE ASSOCIATION-ADDING EVENT PATH (#631). The ambient-context
        // overload mints its own envelope, so on a substrate delivery it would evaluate this read
        // as nobody, or as whoever published — never necessarily the subject the envelope was
        // signed for. This overload is handed the envelope and must decide as ITS caller.
        [Fact]
        public async Task ShouldRetrieveNonPublicTagByIdAsTheInboundEnvelopesCallerAsync()
        {
            // given
            Tag randomTag = CreateRandomTag();
            Tag storageTag = randomTag;
            storageTag.IsDeleted = false;
            storageTag.ApprovalStatus = ApprovalStatus.Draft;
            storageTag.IsPublished = false;
            Tag expectedTag = storageTag;

            // the row's owner, carried on the envelope rather than found in the ambient context
            SecurityContext ownerSecurityContext = CreateAuthenticatedSecurityContext();

            // An Association-sourced envelope, because that is what the only production caller
            // holds: the association orchestration passes the ADD REQUEST it is handling.
            var inboundEnvelope = new EventEnvelope<Association>
            {
                Content = new Association { Id = Guid.NewGuid() },
                SecurityContext = ownerSecurityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<Tag>()))
                        .Returns((EventEnvelope<Association> source, Tag content) =>
                            new ValueTask<EventEnvelope<Tag>>(
                                new EventEnvelope<Tag>
                                {
                                    Content = content,
                                    SecurityContext = source.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            // THE AMBIENT CALLER IS NOBODY. Had the overload minted its own context, the gate
            // would refuse before the owner test was reached.
            this.ambientSecurityContext = new SecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    randomTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(ownerSecurityContext))
                    .ReturnsAsync(storageTag.CreatedBy);

            // when
            Tag actualTag =
                await this.tagService.RetrieveTagByIdAsync(
                    randomTag.Id,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualTag.Should().BeEquivalentTo(expectedTag);

            // the gate was asked about the ENVELOPE's subject, never the ambient one
            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(ownerSecurityContext),
                Times.Once);

            // chained rather than minted, so causation stays linked and the context is copied
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(
                    inboundEnvelope,
                    It.Is<Tag>(tag => tag.Id == randomTag.Id)),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Tag>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    randomTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A MISS ON THIS OVERLOAD MUST READ AS A MISS. The association orchestration recognises an
        // unresolvable endpoint by its *ValidationException shape; a raw NotFoundTagException
        // escaping here would reach its dependency clause instead and report "this endpoint does
        // not exist" as a failed dependency.
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdAsTheInboundEnvelopesCallerIfTagNotFoundAndLogItAsync()
        {
            // given
            Guid someTagId = Guid.NewGuid();
            Tag nullTag = null;

            var inboundEnvelope = new EventEnvelope<Association>
            {
                Content = new Association { Id = Guid.NewGuid() },
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<Tag>()))
                        .Returns((EventEnvelope<Association> source, Tag content) =>
                            new ValueTask<EventEnvelope<Tag>>(
                                new EventEnvelope<Tag>
                                {
                                    Content = content,
                                    SecurityContext = source.SecurityContext,
                                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                                }));

            var notFoundTagException =
                new NotFoundTagException(
                    message: $"Tag not found with id: {someTagId}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: notFoundTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullTag);

            // when
            ValueTask<Tag> retrieveTagByIdTask =
                this.tagService.RetrieveTagByIdAsync(
                    someTagId,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    retrieveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
