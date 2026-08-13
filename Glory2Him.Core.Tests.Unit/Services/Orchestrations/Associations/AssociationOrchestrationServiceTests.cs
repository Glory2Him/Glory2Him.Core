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
using System.Linq.Expressions;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Glory2Him.Core.Services.Foundations.Comments;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Foundations.Reactions;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.Core.Services.Orchestrations.Associations;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        private readonly Mock<IAssociationService> associationServiceMock;
        private readonly Mock<IContentItemService> contentItemServiceMock;
        private readonly Mock<ITagService> tagServiceMock;
        private readonly Mock<IReactionService> reactionServiceMock;
        private readonly Mock<IBibleReferenceService> bibleReferenceServiceMock;
        private readonly Mock<ICommentService> commentServiceMock;
        private readonly Mock<ILinkService> linkServiceMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IAssociationOrchestrationService associationOrchestrationService;
        private SecurityContext ambientSecurityContext;

        public AssociationOrchestrationServiceTests()
        {
            this.associationServiceMock = new Mock<IAssociationService>();
            this.contentItemServiceMock = new Mock<IContentItemService>();
            this.tagServiceMock = new Mock<ITagService>();
            this.reactionServiceMock = new Mock<IReactionService>();
            this.bibleReferenceServiceMock = new Mock<IBibleReferenceService>();
            this.commentServiceMock = new Mock<ICommentService>();
            this.linkServiceMock = new Mock<ILinkService>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<Association>()))
                    .Returns((Association content) =>
                        new ValueTask<EventEnvelope<Association>>(
                            new EventEnvelope<Association>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            this.associationOrchestrationService = new AssociationOrchestrationService(
                associationService: this.associationServiceMock.Object,
                contentItemService: this.contentItemServiceMock.Object,
                tagService: this.tagServiceMock.Object,
                reactionService: this.reactionServiceMock.Object,
                bibleReferenceService: this.bibleReferenceServiceMock.Object,
                commentService: this.commentServiceMock.Object,
                linkService: this.linkServiceMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        public static TheoryData<Xeption> AssociationDependencyValidationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                    .AssociationValidationException(message: randomMessage, innerException: innerException),

                new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                    .AssociationDependencyValidationException(message: randomMessage, innerException: innerException),
            };
        }

        public static TheoryData<Xeption> AssociationDependencyExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                    .AssociationDependencyException(message: randomMessage, innerException: innerException),

                new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                    .AssociationServiceException(message: randomMessage, innerException: innerException),
            };
        }

        // A raw add request: only the endpoint types and key ids the caller supplies. A ContentItem
        // on A (the versioned, content-typed side) and a Tag on B (a non-versioned side) exercise
        // both resolution shapes.
        private static Association CreateRawAddRequest()
        {
            return new Association
            {
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = Guid.NewGuid(),
                EntityBType = EntityType.Tag,
                EntityBKeyId = Guid.NewGuid(),
                UserId = null,
            };
        }

        private static AssociationPairMatch CreatePairMatch(
            ApprovalStatus approvalStatus,
            bool isDeleted)
        {
            return new AssociationPairMatch
            {
                Id = Guid.NewGuid(),
                ApprovalStatus = approvalStatus,
                IsDeleted = isDeleted,
                CreatedBy = $"author-{Guid.NewGuid()}",
                DeletedBy = isDeleted ? $"deleter-{Guid.NewGuid()}" : null,
            };
        }

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);
    }
}
