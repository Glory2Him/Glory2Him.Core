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
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        // Every rule the method path's write flow holds ahead of the pair probe, each named by
        // what it refuses. A rule added to that flow later belongs in this list — and the point
        // of the seam is that it then holds on both doors without a line of the event handler
        // changing.
        public static TheoryData<string> WriteFlowRefusals() =>
            new TheoryData<string>
            {
                "an unauthenticated caller",
                "a globally blocked caller",
                "a caller blocked from an endpoint's content type",
                "a missing endpoint key",
                "an endpoint that does not resolve",
            };

        // ONE WRITE FLOW, BOTH DOORS (#631 criterion 4). The event handler carries no second copy
        // of endpoint resolution, of the UserId derivation, or of any rule the method path holds;
        // it reaches them through the same flow AddAssociationAsync uses. Proven by driving each
        // of that flow's refusals through BOTH entry points and requiring the SAME answer from
        // each — a rule present on one door and missing, or worded differently, on the other
        // fails here. The reaction-specific facet refusal that will ride this seam is #617's,
        // and is deliberately not written here.
        [Theory]
        [MemberData(nameof(WriteFlowRefusals))]
        public async Task ShouldRunTheSameWriteFlowOnBothEntryPathsAsync(string refusal)
        {
            // given
            Association methodRequest = CreateHonestAddRequest();
            Association eventRequest = methodRequest.DeepClone();
            SecurityContext callerContext = CreateAuthenticatedSecurityContext();

            switch (refusal)
            {
                case "an unauthenticated caller":
                    callerContext = new SecurityContext { IsAuthenticated = false };
                    break;

                case "a globally blocked caller":
                    callerContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);
                    break;

                case "a caller blocked from an endpoint's content type":
                    callerContext = CreateAuthenticatedSecurityContext(
                        Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Story));

                    break;

                case "a missing endpoint key":
                    methodRequest.EntityBKeyId = Guid.Empty;
                    eventRequest.EntityBKeyId = Guid.Empty;
                    break;
            }

            this.ambientSecurityContext = callerContext;
            EventEnvelope<Association> inputEnvelope =
                CreateRequestEnvelope(eventRequest, callerContext);

            SetupEndpointReads(methodRequest);
            SetupEventPathEndpointReads(eventRequest, inputEnvelope);

            if (refusal == "an endpoint that does not resolve")
            {
                var tagValidationException = new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: new Xeption(message: "Tag not found."));

                this.tagServiceMock.Setup(service =>
                    service.RetrieveTagByIdAsync(
                        methodRequest.EntityBKeyId,
                        It.IsAny<CancellationToken>()))
                            .ThrowsAsync(tagValidationException);

                this.tagServiceMock.Setup(service =>
                    service.RetrieveTagByIdAsync(
                        eventRequest.EntityBKeyId,
                        inputEnvelope,
                        It.IsAny<CancellationToken>()))
                            .ThrowsAsync(tagValidationException);
            }

            // when
            ValueTask<AssociationSuggestionResult> methodPathTask =
                this.associationOrchestrationService.AddAssociationAsync(
                    methodRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException methodPathException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    methodPathTask.AsTask);

            ValueTask<EventEnvelope<Association>> eventPathTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException eventPathException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    eventPathTask.AsTask);

            // then
            eventPathException.Should().BeEquivalentTo(methodPathException);

            // neither door reaches a write
            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.associationServiceMock.Verify(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // THE METHOD PATH'S ARM OF CRITERION 3, and the reason it is not the event path's. The
        // same contradictions the event path refuses are OVERWRITTEN here, because a method
        // caller hands over a loose object nobody attested to, and amending it costs nothing.
        // Pinned beside the write-flow seam because that seam is shared: a refusal that slipped
        // into the shared flow instead of the event handler would turn this door's overwrite
        // into a refusal, and this is the test that notices.
        [Theory]
        [MemberData(nameof(ContradictingContentTypeClaims))]
        public async Task ShouldOverwriteACallerSuppliedContentTypeOnTheMethodPathAsync(
            ContentType? claimedEntityAContentType,
            ContentType? claimedEntityBContentType,
            string contradictedParameter)
        {
            // given
            Association rawRequest = CreateRawAddRequest();
            rawRequest.EntityAContentType = claimedEntityAContentType;
            rawRequest.EntityBContentType = claimedEntityBContentType;
            ContentItem resolvedContentItem = SetupEndpointReads(rawRequest);
            Association capturedForLookup = null;

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Association, CancellationToken>(
                            (association, _) => capturedForLookup = association.DeepClone())
                        .ReturnsAsync(CreatePairMatch(ApprovalStatus.Approved, isDeleted: false));

            // when
            AssociationSuggestionResult actualResult =
                await this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Status.Should().Be(AssociationSuggestionStatus.AlreadyApproved);
            capturedForLookup.Should().NotBeNull(because: $"{contradictedParameter} is overwritten");
            capturedForLookup.EntityAContentType.Should().Be(resolvedContentItem.ContentType);
            capturedForLookup.EntityBContentType.Should().BeNull();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
