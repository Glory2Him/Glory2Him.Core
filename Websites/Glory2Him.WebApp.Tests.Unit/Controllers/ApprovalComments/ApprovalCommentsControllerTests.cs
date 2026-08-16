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
using System.Linq;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.WebApp.Controllers.ApprovalComments;
using Moq;
using RESTFulSense.Controllers;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalComments
{
    public partial class ApprovalCommentsControllerTests : RESTFulController
    {
        private readonly Mock<IApprovalCommentService> approvalCommentServiceMock;
        private readonly ApprovalCommentsController approvalCommentsController;

        public ApprovalCommentsControllerTests()
        {
            approvalCommentServiceMock = new Mock<IApprovalCommentService>();
            approvalCommentsController = new ApprovalCommentsController(approvalCommentServiceMock.Object);
        }

        public static TheoryData<Xeption> ValidationExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ApprovalCommentValidationException(
                    message: someMessage,
                    innerException: someInnerException),

                new ApprovalCommentDependencyValidationException(
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
                new ApprovalCommentDependencyException(
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
                new ApprovalCommentServiceException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static bool GetRandomBoolean() =>
            Randomizer<bool>.Create();

        private static ApprovalComment CreateRandomApprovalComment() =>
            CreateApprovalCommentFiller().Create();

        private static IQueryable<ApprovalComment> CreateRandomApprovalComments()
        {
            return CreateApprovalCommentFiller()
                .Create(count: GetRandomNumber())
                    .AsQueryable();
        }

        private static Filler<ApprovalComment> CreateApprovalCommentFiller()
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            string user = Guid.NewGuid().ToString();
            var filler = new Filler<ApprovalComment>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)

                // Approval is a navigation back onto a graph that points at this entity again.
                // The controller never touches it, and filling it would have the object filler
                // walk the cycle, so it is left null here as it is in Core's own suite.
                .OnProperty(approvalComment => approvalComment.Approval).IgnoreIt()
                .OnProperty(approvalComment => approvalComment.CreatedBy).Use(user)
                .OnProperty(approvalComment => approvalComment.UpdatedBy).Use(user);

            return filler;
        }
    }
}
