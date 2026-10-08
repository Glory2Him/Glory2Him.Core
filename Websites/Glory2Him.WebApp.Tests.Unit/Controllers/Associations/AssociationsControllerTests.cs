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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Services.Orchestrations.Associations;
using Glory2Him.WebApp.Controllers.Associations;
using Moq;
using RESTFulSense.Controllers;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests : RESTFulController
    {
        private readonly Mock<IAssociationOrchestrationService> associationOrchestrationServiceMock;
        private readonly AssociationsController associationsController;

        public AssociationsControllerTests()
        {
            associationOrchestrationServiceMock = new Mock<IAssociationOrchestrationService>();

            associationsController =
                new AssociationsController(associationOrchestrationServiceMock.Object);
        }

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        /// <summary>
        /// The caller's shape — two endpoints and nothing else (§ARC16.8.1). The orchestration
        /// derives every other member, so a test that filled them would be asserting a body the
        /// route never carries.
        /// </summary>
        private static Association CreateRandomAssociation() =>
            new Association
            {
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = Guid.NewGuid(),
                EntityBType = EntityType.Reaction,
                EntityBKeyId = Guid.NewGuid()
            };

        public static TheoryData<Xeption> ValidationExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new AssociationOrchestrationValidationException(
                    message: someMessage,
                    innerException: someInnerException),

                new AssociationOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        private static Guid[] CreateRandomContentItemIds() =>
            Enumerable.Range(start: 0, count: GetRandomNumber())
                .Select(_ => Guid.NewGuid())
                .ToArray();

        private static List<ContentItemReactionSummary> CreateRandomContentItemReactionSummaries(
            IEnumerable<Guid> contentItemIds) =>
            contentItemIds
                .Select(contentItemId => new ContentItemReactionSummary
                {
                    ContentItemId = contentItemId,

                    Reactions = new List<ContentItemReactionCount>
                    {
                        new ContentItemReactionCount
                        {
                            ReactionId = Guid.NewGuid(),
                            Name = GetRandomString(),
                            UnicodeEmoji = GetRandomString(),
                            Count = GetRandomNumber()
                        }
                    },

                    ViewerReactionId = Guid.NewGuid(),
                    ViewerReactionName = GetRandomString()
                })
                .ToList();

        private static AssociationRemovalResult CreateAssociationRemovalResult(
            AssociationRemovalStatus status) =>
            new AssociationRemovalResult
            {
                Status = status,
                AssociationId = status is AssociationRemovalStatus.Removed ? Guid.NewGuid() : null
            };

        private static AssociationSuggestionResult CreateAssociationSuggestionResult(
            AssociationSuggestionStatus status) =>
            new AssociationSuggestionResult
            {
                Status = status,
                AssociationId = Guid.NewGuid()
            };
    }
}
