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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Orchestrations.AIReviewers;
using Glory2Him.Core.Models.Orchestrations.AIReviewers.Exceptions;
using Glory2Him.Core.Services.Orchestrations.AIReviewers;
using Glory2Him.WebApp.Controllers.AIReviewers;
using Moq;
using RESTFulSense.Controllers;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.AIReviewers
{
    /// <summary>
    /// Berean's own exposer (design §8.6.2, issue #354 Track A) — three actions over one
    /// resource, delegating to one service.
    ///
    /// <para>ITS OWN SUITE, because these actions moved off <c>ApprovalsController</c>. They used
    /// to be asserted against a mock of <c>IApprovalOrchestrationService</c>, which is what made
    /// the exposer reach a second concern through the approval round's contract. The single mock
    /// below is the whole of what this controller may talk to, so an action that grew a second
    /// dependency could not be arranged here at all.</para>
    /// </summary>
    public partial class AIReviewersControllerTests : RESTFulController
    {
        private readonly Mock<IAIReviewerOrchestrationService> aiReviewerOrchestrationServiceMock;
        private readonly AIReviewersController aiReviewersController;

        public AIReviewersControllerTests()
        {
            this.aiReviewerOrchestrationServiceMock =
                new Mock<IAIReviewerOrchestrationService>();

            this.aiReviewersController =
                new AIReviewersController(this.aiReviewerOrchestrationServiceMock.Object);
        }

        public static TheoryData<Xeption> ValidationExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new AIReviewerOrchestrationValidationException(
                    message: someMessage,
                    innerException: someInnerException),

                new AIReviewerOrchestrationDependencyValidationException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        public static TheoryData<Xeption> DependencyExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new AIReviewerOrchestrationDependencyException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        public static TheoryData<Xeption> ServerExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new AIReviewerOrchestrationServiceException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        // Built by hand rather than by the object filler: every member is `required` and `init`,
        // so a filler would refuse the type.
        private static AIReviewerStatus CreateRandomAIReviewerStatus() =>
            new AIReviewerStatus
            {
                IsOffered = GetRandomBoolean(),
                IsRequested = GetRandomBoolean(),
                IsAIReviewCompleted = GetRandomBoolean(),
                IsAIReviewCommentsPresent = GetRandomBoolean(),
            };

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static bool GetRandomBoolean() =>
            Randomizer<bool>.Create();

        private static EntityType GetRandomEntityType()
        {
            EntityType[] values = Enum.GetValues<EntityType>();

            return values[new IntRange(min: 0, max: values.Length - 1).GetValue()];
        }
    }
}
