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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldOnlyMoveApprovalStatusBetweenDraftAndSubmittedOnModifyAsync()
        {
            // given: `Association` has no caller-editable content at all (§APR7.5.1 rule 4), so
            // the general modify's whole effective payload is the Draft ↔ Submitted carve-out
            // (§APR9.2 rules 4-6). There is no field map, and this member composes NOTHING: the
            // orchestration derives nothing onto the row and resolves no endpoint, because
            // authorization here is decided against the STORED endpoints and a caller-supplied
            // association puts no trustworthy endpoint in its hand (§SEC14.7 posture A′ rule 4).
            //
            // What is provable at THIS layer is exactly that: the caller's object reaches the
            // foundation untouched, no endpoint is consulted on the way, and the foundation's
            // answer is what the caller gets.
            //
            // THIS TEST PROVES NOTHING ABOUT THE PIN and must not be read as doing so. The pin
            // REFUSES a changed field rather than absorbing it, and what this service owes it is
            // that the refusal is neither swallowed nor re-mapped — which is
            // ShouldThrowDependencyValidationExceptionOnModifyIfAPinnedFieldIsChangedAsync's, in
            // the .Validations partial.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem storedContentItemEndpoint =
                CreateEndpointContentItem(ContentType.Story);

            Tag storedTagEndpoint = CreateEndpointTag();

            Association persistedAssociation =
                CreateStoredAssociation(storedContentItemEndpoint, storedTagEndpoint);

            persistedAssociation.ApprovalStatus = ApprovalStatus.Submitted;

            // the caller's copy: the status moves, and every other field is a claim the stored
            // row will refuse
            var callerSuppliedAssociation = new Association
            {
                Id = persistedAssociation.Id,
                ApprovalStatus = ApprovalStatus.Submitted,
                EntityAType = EntityType.BibleReference,
                EntityAKeyId = Guid.NewGuid(),
                EntityAGroupId = Guid.NewGuid(),
                EntityAScope = Scope.ThisVersionOnly,
                EntityAContentType = ContentType.Testimony,
                EntityBType = EntityType.Comment,
                EntityBKeyId = Guid.NewGuid(),
                EntityBGroupId = Guid.NewGuid(),
                EntityBScope = Scope.AllVersions,
                SortOrder = 999,
                ConfidenceScore = 0.99m,
                IsPublished = true,
                PublishDate = new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero),
            };

            this.associationServiceMock.Setup(service =>
                service.ModifyAssociationAsync(
                    callerSuppliedAssociation,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(persistedAssociation);

            // when
            Association actualAssociation =
                await this.associationOrchestrationService.ModifyAssociationAsync(
                    callerSuppliedAssociation,
                    TestContext.Current.CancellationToken);

            // then: the foundation's answer is what the caller gets
            actualAssociation.Should().BeSameAs(persistedAssociation);

            // the caller's object went down UNTOUCHED — nothing was derived onto it. This is the
            // whole difference between this member and the add, which overwrites all six derived
            // endpoint fields from storage before it writes.
            callerSuppliedAssociation.EntityAType.Should().Be(EntityType.BibleReference);
            callerSuppliedAssociation.EntityAKeyId.Should().NotBe(storedContentItemEndpoint.Id);
            callerSuppliedAssociation.EntityAContentType.Should().Be(ContentType.Testimony);
            callerSuppliedAssociation.EntityAScope.Should().Be(Scope.ThisVersionOnly);
            callerSuppliedAssociation.EntityBType.Should().Be(EntityType.Comment);

            this.associationServiceMock.Verify(service =>
                service.ModifyAssociationAsync(
                    callerSuppliedAssociation,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // not one endpoint was resolved, and no second read duplicated the foundation's half
            // of the gate. Adding endpoint resolution here is a finding, not an improvement.
            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
            this.commentServiceMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
