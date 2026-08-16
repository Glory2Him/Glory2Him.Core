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

using System.Collections.Generic;
using G2H.Security.Client.Clients.Access;
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Foundations.Access.Exceptions;
using G2H.Security.Client.Models.Securities;
using G2H.Security.Client.Services.Foundations.Access;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace G2H.Security.Client.Tests.Unit.Clients.Access
{
    public partial class AccessClientTests
    {
        private readonly Mock<IAccessService> accessServiceMock;
        private readonly IAccessClient accessClient;

        public AccessClientTests()
        {
            this.accessServiceMock = new Mock<IAccessService>();
            this.accessClient = new AccessClient(accessService: this.accessServiceMock.Object);
        }

        public static TheoryData<Xeption> ValidationExceptions()
        {
            string randomMessage = GetRandomString();
            string exceptionMessage = randomMessage;
            var innerException = new Xeption(exceptionMessage);

            return new TheoryData<Xeption>
            {
                new AccessValidationException(
                    message: "Access validation errors occurred, please try again.",
                    innerException),
            };
        }

        public static TheoryData<Xeption> DependencyExceptions()
        {
            string randomMessage = GetRandomString();
            string exceptionMessage = randomMessage;
            var innerException = new Xeption(exceptionMessage);

            return new TheoryData<Xeption>
            {
                new AccessServiceException(
                    message: "Access service error occurred, please contact support.",
                    innerException),
            };
        }

        private static AccessActor CreateRandomAccessActor() =>
            new AccessActor
            {
                UserId = GetRandomString(),
                Roles = new List<string> { RoleNames.Publisher },
                IsAuthenticated = true,
            };

        private static ApprovalConditionsRequest CreateRandomApprovalConditionsRequest() =>
            new ApprovalConditionsRequest
            {
                CandidatePolicies = new List<ApprovalPolicy>(),
                EntityType = GetRandomString(),
                ContentType = null,
                Reviews = new List<ReviewRecord>(),
                ApprovalComments = new List<ApprovalCommentRecord>(),
                ConfidenceScore = null,
            };

        private static RecordReviewRequest CreateRandomRecordReviewRequest() =>
            new RecordReviewRequest
            {
                Actor = CreateRandomAccessActor(),
                RoleSubjects = new List<RoleSubject>(),
                EntityCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                ExistingReviews = new List<ReviewRecord>(),
                IsAmendingOwnReview = false,
            };

        private static AmendApprovalRequest CreateRandomAmendApprovalRequest() =>
            new AmendApprovalRequest
            {
                Actor = CreateRandomAccessActor(),
                RoleSubjects = new List<RoleSubject>(),
                ApprovalCreatedBy = GetRandomString(),
            };

        private static DismissReviewRequest CreateRandomDismissReviewRequest() =>
            new DismissReviewRequest
            {
                Actor = CreateRandomAccessActor(),
                RoleSubjects = new List<RoleSubject>(),
            };

        private static DecideApprovalRequest CreateRandomDecideApprovalRequest() =>
            new DecideApprovalRequest
            {
                Actor = CreateRandomAccessActor(),
                Decision = ApprovalDecision.Approve,
                RoleSubjects = new List<RoleSubject>(),
                CandidatePolicies = new List<ApprovalPolicy>(),
                EntityType = GetRandomString(),
                ContentType = null,
                EntityCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                Reviews = new List<ReviewRecord>(),
                ApprovalComments = new List<ApprovalCommentRecord>(),
                ConfidenceScore = null,
                IsBypassRequested = false,
                BypassReason = null,
            };

        private static ApprovalConditionsVerdict CreateRandomApprovalConditionsVerdict() =>
            new ApprovalConditionsVerdict
            {
                AreConditionsMet = true,
                ShouldAutoApprove = false,
                BlockReason = AccessDenialReason.None,
                ApprovalCount = GetRandomNumber(),
                RequiredNumberOfApprovals = GetRandomNumber(),
                Explanation = GetRandomString(),
            };

        private static RecordApprovalCommentRequest CreateRandomRecordApprovalCommentRequest() =>
            new RecordApprovalCommentRequest
            {
                Actor = CreateRandomAccessActor(),
                ApprovalState = ApprovalState.Submitted,
                IsParentApprovalDeleted = false,
            };

        private static AmendApprovalCommentRequest CreateRandomAmendApprovalCommentRequest() =>
            new AmendApprovalCommentRequest
            {
                Actor = CreateRandomAccessActor(),
                CommentCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                IsParentApprovalDeleted = false,
            };

        private static ResolveApprovalCommentRequest CreateRandomResolveApprovalCommentRequest() =>
            new ResolveApprovalCommentRequest
            {
                Actor = CreateRandomAccessActor(),
                CommentCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                IsParentApprovalDeleted = false,
            };

        private static AccessVerdict CreateRandomAccessVerdict() =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = GetRandomString(),
            };

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();
    }
}
