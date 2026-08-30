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
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Clients;
using G2H.Security.Client.Clients.Access;
using G2H.Security.Client.Clients.Audits;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Attachments;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Securities;
using Moq;
using Tynamix.ObjectFiller;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        private readonly Mock<IStorageBroker> storageBrokerMock;
        private readonly Mock<ISecurityClient> securityClientMock;
        private readonly Mock<IAccessClient> accessClientMock;
        private readonly Mock<IAuditClient> auditClientMock;
        private readonly IAccessBroker accessBroker;

        // The id the audit surface resolves. Every SecurityContext this fixture builds carries a
        // DIFFERENT SubjectId, so a test asserting the actor id cannot pass on the wrong source.
        private readonly string auditResolvedUserId;

        // The request the mocked decision client was actually handed. Almost every assertion here
        // is about what crossed that boundary, so it is captured rather than matched inline.
        private DecideApprovalRequest capturedDecideApprovalRequest;
        private RecordReviewRequest capturedRecordReviewRequest;
        private AmendApprovalRequest capturedAmendApprovalRequest;
        private RecordApprovalCommentRequest capturedRecordApprovalCommentRequest;
        private AmendApprovalCommentRequest capturedAmendApprovalCommentRequest;
        private ResolveApprovalCommentRequest capturedResolveApprovalCommentRequest;

        // The principal the broker built from the envelope's SecurityContext. Rebuilt inside the
        // broker, so it can only be seen from the boundary it was handed to.
        private ClaimsPrincipal capturedActorPrincipal;

        public AccessBrokerTests()
        {
            this.storageBrokerMock = new Mock<IStorageBroker>();
            this.securityClientMock = new Mock<ISecurityClient>();
            this.accessClientMock = new Mock<IAccessClient>();
            this.auditClientMock = new Mock<IAuditClient>();
            this.auditResolvedUserId = "audit-resolved-" + GetRandomString();

            this.securityClientMock.SetupGet(client =>
                client.Access)
                    .Returns(this.accessClientMock.Object);

            this.securityClientMock.SetupGet(client =>
                client.Audits)
                    .Returns(this.auditClientMock.Object);

            this.auditClientMock.Setup(client =>
                client.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
                    .Callback((ClaimsPrincipal claimsPrincipal) =>
                        this.capturedActorPrincipal = claimsPrincipal)
                    .ReturnsAsync(this.auditResolvedUserId);

            // Empty by default so a test only states the rows its own subject depends on. Moq
            // takes the last matching setup, so a test's own SetupApprovals wins over this one.
            SetupApprovals();
            SetupApprovalReviews();
            SetupApprovalComments();
            SetupApprovalSettings();
            SetupAccessClientToReturn(CreatePermittedVerdict());

            // The internal constructor. The public one news up a real SecurityClient, which would
            // make every test here an integration test against the decision function.
            this.accessBroker = new AccessBroker(
                storageBroker: this.storageBrokerMock.Object,
                securityClient: this.securityClientMock.Object);
        }

        // The broker rebuilds the principal from the envelope's SecurityContext, so the call
        // cannot be matched by reference the way the SecurityContext once was. Matching on the
        // NameIdentifier claim keeps the verification tied to the context it was made for.
        private void VerifyTheActorWasResolvedFor(SecurityContext securityContext) =>
            this.auditClientMock.Verify(client =>
                client.GetUserIdAsync(It.Is<ClaimsPrincipal>(claimsPrincipal =>
                    claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value
                        == securityContext.SubjectId)),
                            Times.Once);

        private void SetupAccessClientToReturn(AccessVerdict accessVerdict)
        {
            this.accessClientMock.Setup(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()))
                    .Callback((DecideApprovalRequest decideApprovalRequest) =>
                        this.capturedDecideApprovalRequest = decideApprovalRequest)
                    .ReturnsAsync(accessVerdict);

            this.accessClientMock.Setup(client =>
                client.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()))
                    .Callback((RecordReviewRequest recordReviewRequest) =>
                        this.capturedRecordReviewRequest = recordReviewRequest)
                    .ReturnsAsync(accessVerdict);

            this.accessClientMock.Setup(client =>
                client.MayAmendApprovalAsync(It.IsAny<AmendApprovalRequest>()))
                    .Callback((AmendApprovalRequest amendApprovalRequest) =>
                        this.capturedAmendApprovalRequest = amendApprovalRequest)
                    .ReturnsAsync(accessVerdict);

            this.accessClientMock.Setup(client =>
                client.MayRecordApprovalCommentAsync(It.IsAny<RecordApprovalCommentRequest>()))
                    .Callback((RecordApprovalCommentRequest recordApprovalCommentRequest) =>
                        this.capturedRecordApprovalCommentRequest = recordApprovalCommentRequest)
                    .ReturnsAsync(accessVerdict);

            this.accessClientMock.Setup(client =>
                client.MayAmendApprovalCommentAsync(It.IsAny<AmendApprovalCommentRequest>()))
                    .Callback((AmendApprovalCommentRequest amendApprovalCommentRequest) =>
                        this.capturedAmendApprovalCommentRequest = amendApprovalCommentRequest)
                    .ReturnsAsync(accessVerdict);

            this.accessClientMock.Setup(client =>
                client.MayResolveApprovalCommentAsync(It.IsAny<ResolveApprovalCommentRequest>()))
                    .Callback((ResolveApprovalCommentRequest resolveApprovalCommentRequest) =>
                        this.capturedResolveApprovalCommentRequest = resolveApprovalCommentRequest)
                    .ReturnsAsync(accessVerdict);
        }

        private void SetupApprovals(params Approval[] approvals) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Approval>(approvals).AsQueryable());

        private void SetupApprovalById(Approval approval) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(approval.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(approval);

        private void SetupApprovalReviews(params ApprovalReview[] approvalReviews) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<ApprovalReview>(approvalReviews).AsQueryable());

        private void SetupApprovalComments(params ApprovalComment[] approvalComments) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<ApprovalComment>(approvalComments).AsQueryable());

        private void SetupApprovalSettings(params ApprovalSetting[] approvalSettings) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<ApprovalSetting>(approvalSettings).AsQueryable());

        // The traversal the broker performs for one entity type, and the read that proves it took
        // that branch. Paired so the theory over every EntityType member stays readable.
        private void SetupEntityAuthor(EntityType entityType, Guid entityId, string createdBy)
        {
            switch (entityType)
            {
                case EntityType.ContentItem:
                    this.storageBrokerMock.Setup(broker =>
                        broker.SelectContentItemByIdAsync(entityId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new ContentItem
                            {
                                Id = entityId,
                                CreatedBy = createdBy,
                                ContentType = ContentType.Testimony,
                            });

                    break;

                case EntityType.Tag:
                    this.storageBrokerMock.Setup(broker =>
                        broker.SelectTagByIdAsync(entityId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new Tag { Id = entityId, CreatedBy = createdBy });

                    break;

                case EntityType.Reaction:
                    this.storageBrokerMock.Setup(broker =>
                        broker.SelectReactionByIdAsync(entityId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new Reaction { Id = entityId, CreatedBy = createdBy });

                    break;

                case EntityType.BibleReference:
                    this.storageBrokerMock.Setup(broker =>
                        broker.SelectBibleReferenceByIdAsync(entityId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new BibleReference { Id = entityId, CreatedBy = createdBy });

                    break;

                case EntityType.Comment:
                    this.storageBrokerMock.Setup(broker =>
                        broker.SelectCommentByIdAsync(entityId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new Comment { Id = entityId, CreatedBy = createdBy });

                    break;

                case EntityType.Link:
                    this.storageBrokerMock.Setup(broker =>
                        broker.SelectLinkByIdAsync(entityId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new Link { Id = entityId, CreatedBy = createdBy });

                    break;

                case EntityType.Attachment:
                    this.storageBrokerMock.Setup(broker =>
                        broker.SelectAttachmentByIdAsync(entityId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new Attachment { Id = entityId, CreatedBy = createdBy });

                    break;

                case EntityType.Association:
                    // The endpoints are set deliberately rather than left at their defaults: an
                    // association is authorised from them and from nothing else (§14.7 posture
                    // A′ rule 2), so a row whose EntityAType and EntityBType both default to
                    // ContentItem would let a broken traversal pass. These are the two the
                    // design uses as its worked example, and they differ from each other and
                    // from Association itself.
                    this.storageBrokerMock.Setup(broker =>
                        broker.SelectAssociationByIdAsync(entityId, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new Association
                            {
                                Id = entityId,
                                CreatedBy = createdBy,
                                EntityAType = EntityType.ContentItem,
                                EntityAContentType = ContentType.Testimony,
                                EntityBType = EntityType.BibleReference,
                                EntityBContentType = null,
                            });

                    break;
            }
        }

        private void VerifyEntityAuthorRead(EntityType entityType, Guid entityId)
        {
            switch (entityType)
            {
                case EntityType.ContentItem:
                    this.storageBrokerMock.Verify(broker =>
                        broker.SelectContentItemByIdAsync(entityId, It.IsAny<CancellationToken>()),
                            Times.Once);

                    break;

                case EntityType.Tag:
                    this.storageBrokerMock.Verify(broker =>
                        broker.SelectTagByIdAsync(entityId, It.IsAny<CancellationToken>()),
                            Times.Once);

                    break;

                case EntityType.Reaction:
                    this.storageBrokerMock.Verify(broker =>
                        broker.SelectReactionByIdAsync(entityId, It.IsAny<CancellationToken>()),
                            Times.Once);

                    break;

                case EntityType.BibleReference:
                    this.storageBrokerMock.Verify(broker =>
                        broker.SelectBibleReferenceByIdAsync(entityId, It.IsAny<CancellationToken>()),
                            Times.Once);

                    break;

                case EntityType.Comment:
                    this.storageBrokerMock.Verify(broker =>
                        broker.SelectCommentByIdAsync(entityId, It.IsAny<CancellationToken>()),
                            Times.Once);

                    break;

                case EntityType.Link:
                    this.storageBrokerMock.Verify(broker =>
                        broker.SelectLinkByIdAsync(entityId, It.IsAny<CancellationToken>()),
                            Times.Once);

                    break;

                case EntityType.Attachment:
                    this.storageBrokerMock.Verify(broker =>
                        broker.SelectAttachmentByIdAsync(entityId, It.IsAny<CancellationToken>()),
                            Times.Once);

                    break;

                case EntityType.Association:
                    this.storageBrokerMock.Verify(broker =>
                        broker.SelectAssociationByIdAsync(entityId, It.IsAny<CancellationToken>()),
                            Times.Once);

                    break;
            }
        }

        private static AccessVerdict CreatePermittedVerdict() =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "permitted",
            };

        private static Approval CreateApproval(
            Guid approvalId,
            EntityType entityType,
            Guid entityId,
            ApprovalStatus approvalStatus,
            bool isDeleted = false) =>
            new Approval
            {
                Id = approvalId,
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = approvalStatus,
                IsDeleted = isDeleted,
            };

        private static ApprovalReview CreateApprovalReview(
            Guid approvalId,
            string createdBy,
            ApprovalStatus statusId,
            bool isDeleted = false) =>
            new ApprovalReview
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                CreatedBy = createdBy,
                StatusId = statusId,
                IsDeleted = isDeleted,
            };

        private static ApprovalComment CreateApprovalComment(
            Guid approvalId,
            bool isResolved,
            bool isDeleted = false) =>
            new ApprovalComment
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                IsResolved = isResolved,
                IsDeleted = isDeleted,
            };

        private static SecurityContext CreateSecurityContext(
            IReadOnlyList<string> roles,
            bool isAuthenticated) =>
            new SecurityContext
            {
                SubjectId = "context-subject-" + GetRandomString(),
                IsAuthenticated = isAuthenticated,
                Roles = roles,
            };

        private static SecurityContext CreateAuthenticatedSecurityContext() =>
            CreateSecurityContext(
                roles: new List<string> { Roles.Publishers },
                isAuthenticated: true);

        private static ApprovalDecisionQuery CreateApprovalDecisionQuery(
            EntityType entityType,
            Guid entityId,
            SecurityContext securityContext,
            ContentType? contentType = null,
            IReadOnlyList<RoleSubject> roleSubjects = null,
            string entityCreatedBy = null,
            decimal? confidenceScore = null,
            ApprovalDecision decision = ApprovalDecision.Approve,
            bool isBypassRequested = false,
            string bypassReason = null) =>
            new ApprovalDecisionQuery
            {
                EntityType = entityType,
                EntityId = entityId,
                ContentType = contentType,
                RoleSubjects = roleSubjects ?? new List<RoleSubject>
                {
                    new RoleSubject
                    {
                        EntityType = entityType.ToString(),
                        ContentType = contentType?.ToString(),
                    },
                },
                EntityCreatedBy = entityCreatedBy ?? "entity-author-" + GetRandomString(),
                ConfidenceScore = confidenceScore,
                Decision = decision,
                IsBypassRequested = isBypassRequested,
                BypassReason = bypassReason,
                SecurityContext = securityContext,
            };

        private static string GetRandomString() =>
            new MnemonicString(wordCount: 1).GetValue();
    }
}
