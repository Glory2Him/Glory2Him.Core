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
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Glory2Him.WebApp.Controllers.ApprovalReviewRequests;
using Moq;
using RESTFulSense.Controllers;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestsControllerTests : RESTFulController
    {
        private readonly Mock<IApprovalOrchestrationService> approvalOrchestrationServiceMock;
        private readonly ApprovalReviewRequestsController approvalReviewRequestsController;

        public ApprovalReviewRequestsControllerTests()
        {
            approvalOrchestrationServiceMock = new Mock<IApprovalOrchestrationService>();

            approvalReviewRequestsController =
                new ApprovalReviewRequestsController(approvalOrchestrationServiceMock.Object);
        }

        // Every refusal on this endpoint arrives as the SAME exception type and is told apart by
        // its inner one, so a ladder that reordered its clauses would keep compiling and keep
        // passing a test that only looked at the outer type. Each theory below therefore carries
        // the inner exception that decides the status.
        public static TheoryData<Xeption> NotFoundExceptions() =>
            new TheoryData<Xeption>
            {
                new ApprovalOrchestrationValidationException(
                    message: GetRandomString(),
                    innerException: new NotFoundApprovalOrchestrationException(
                        message: GetRandomString()))
            };

        public static TheoryData<Xeption> UnauthorizedExceptions() =>
            new TheoryData<Xeption>
            {
                new ApprovalOrchestrationValidationException(
                    message: GetRandomString(),
                    innerException: new UnauthorizedApprovalOrchestrationException(
                        message: GetRandomString()))
            };

        public static TheoryData<Xeption> BadRequestExceptions() =>
            new TheoryData<Xeption>
            {
                // The refusal a withdrawal gets once the invitation has been answered
                // (design 7.9 rule 5), and the shape every other plain validation failure takes.
                new ApprovalOrchestrationValidationException(
                    message: GetRandomString(),
                    innerException: new InvalidApprovalOrchestrationException(
                        message: GetRandomString())),

                new ApprovalOrchestrationDependencyValidationException(
                    message: GetRandomString(),
                    innerException: new Xeption())
            };

        public static TheoryData<Xeption> DependencyExceptions() =>
            new TheoryData<Xeption>
            {
                new ApprovalOrchestrationDependencyException(
                    message: GetRandomString(),
                    innerException: new Xeption())
            };

        public static TheoryData<Xeption> ServerExceptions() =>
            new TheoryData<Xeption>
            {
                new ApprovalOrchestrationServiceException(
                    message: GetRandomString(),
                    innerException: new Xeption())
            };

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();
    }
}
