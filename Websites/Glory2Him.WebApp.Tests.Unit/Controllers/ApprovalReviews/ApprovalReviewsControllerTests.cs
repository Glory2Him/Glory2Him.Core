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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.WebApp.Controllers.ApprovalReviews;
using Moq;
using RESTFulSense.Controllers;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalReviews
{
    public partial class ApprovalReviewsControllerTests : RESTFulController
    {
        private readonly Mock<IApprovalReviewService> approvalReviewServiceMock;
        private readonly ApprovalReviewsController approvalReviewsController;

        public ApprovalReviewsControllerTests()
        {
            approvalReviewServiceMock = new Mock<IApprovalReviewService>();
            approvalReviewsController = new ApprovalReviewsController(approvalReviewServiceMock.Object);
        }

        public static TheoryData<Xeption> ValidationExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ApprovalReviewValidationException(
                    message: someMessage,
                    innerException: someInnerException),

                new ApprovalReviewDependencyValidationException(
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
                new ApprovalReviewDependencyException(
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
                new ApprovalReviewServiceException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static ApprovalReview CreateRandomApprovalReview() =>
            CreateApprovalReviewFiller().Create();

        private static IQueryable<ApprovalReview> CreateRandomApprovalReviews()
        {
            return CreateApprovalReviewFiller()
                .Create(count: GetRandomNumber())
                    .AsQueryable();
        }

        private static Filler<ApprovalReview> CreateApprovalReviewFiller()
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            string user = Guid.NewGuid().ToString();
            var filler = new Filler<ApprovalReview>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)

                // Approval is a navigation back onto a graph that points at this entity again.
                // The controller never touches it, and filling it would have the object filler
                // walk the cycle, so it is left null here as it is in Core's own suite.
                .OnProperty(approvalReview => approvalReview.Approval).IgnoreIt()
                .OnProperty(approvalReview => approvalReview.CreatedBy).Use(user)
                .OnProperty(approvalReview => approvalReview.UpdatedBy).Use(user);

            return filler;
        }
    }
}
