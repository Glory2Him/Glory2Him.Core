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
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Glory2Him.Core.Services.Foundations.Comments;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Foundations.Reactions;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.Core.Services.Orchestrations.Associations;
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

        // ONE WRITE FLOW, BOTH DOORS — THE STRUCTURAL HALF (#631 criterion 4). The theory above
        // proves both doors refuse alike, which a faithful second COPY of the flow also satisfies:
        // two identical copies are indistinguishable by behaviour until one of them changes. So
        // this reads the compiled code. It walks the IL each entry path can reach — through the
        // compiler's async state machines and lambdas — and requires that each reaches the shared
        // flow, and that neither reaches any rule the shared flow holds by any other route: no
        // composition member of this service, no endpoint read, no derived-field write.
        //
        // The leaf primitives (Validate, IsInvalid) are exempt: they carry no operation's policy
        // and are shared freely, so the event path's own refusals may use them.
        [Fact]
        public void ShouldReachTheWriteFlowRulesOnlyThroughTheSharedFlowOnBothEntryPaths()
        {
            // given
            Type serviceType = typeof(AssociationOrchestrationService);

            MethodInfo sharedWriteFlow = serviceType.GetMethod(
                "DeriveAssociationToAddAsync",
                BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo methodPathEntry = serviceType.GetMethod(
                nameof(AssociationOrchestrationService.AddAssociationAsync));

            MethodInfo eventPathEntry = serviceType.GetMethod(
                nameof(AssociationOrchestrationService.OnAddingAssociationAsync));

            sharedWriteFlow.Should().NotBeNull();
            string[] sharedLeafPrimitives = { "Validate", "IsInvalid" };

            Type[] endpointServiceTypes =
            {
                typeof(IContentItemService),
                typeof(ILinkService),
                typeof(ITagService),
                typeof(IReactionService),
                typeof(IBibleReferenceService),
                typeof(ICommentService),
            };

            // when
            HashSet<MethodBase> writeFlowRules =
                GetReachableMethods(sharedWriteFlow, serviceType, notEntering: null)
                    .Where(method =>
                        (IsOwnedBy(method, serviceType)
                            && sharedLeafPrimitives.Contains(method.Name) is false)
                        || endpointServiceTypes.Contains(method.DeclaringType)
                        || (method.DeclaringType == typeof(Association)
                            && method.Name.StartsWith("set_", StringComparison.Ordinal)))
                    .ToHashSet();

            writeFlowRules.Remove(sharedWriteFlow);

            HashSet<MethodBase> methodPathReach =
                GetReachableMethods(methodPathEntry, serviceType, notEntering: sharedWriteFlow);

            HashSet<MethodBase> eventPathReach =
                GetReachableMethods(eventPathEntry, serviceType, notEntering: sharedWriteFlow);

            // then
            writeFlowRules.Select(method => method.Name).Should().Contain(new[]
            {
                "ValidateUserIsAllowedToContribute",
                "ValidateOnAddAssociation",
                "ResolveEndpointAsync",
                "ValidateUserIsNotBlockedFromEndpoints",
                "set_UserId",
            });

            methodPathReach.Intersect(writeFlowRules).Select(method => method.Name)
                .Should().BeEmpty(because: "the method path reaches the write flow's rules only through the shared flow");

            eventPathReach.Intersect(writeFlowRules).Select(method => method.Name)
                .Should().BeEmpty(because: "the event path reaches the write flow's rules only through the shared flow");

            methodPathReach.Contains(sharedWriteFlow).Should().BeTrue(
                because: "the method path runs the shared write flow");

            eventPathReach.Contains(sharedWriteFlow).Should().BeTrue(
                because: "the event path runs the shared write flow");
        }

        // Every method reachable from the root by a call, a callvirt, a newobj or an ldftn in its
        // IL, following the service's own members (and the compiler's state machines and lambdas
        // nested in it) but never into another type, and never INTO notEntering — which is still
        // recorded as reached.
        private static HashSet<MethodBase> GetReachableMethods(
            MethodBase root,
            Type serviceType,
            MethodBase notEntering)
        {
            var reached = new HashSet<MethodBase>();
            var pending = new Stack<MethodBase>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                MethodBase method = pending.Pop();

                if (reached.Add(method) is false
                    || method == notEntering
                    || IsOwnedBy(method, serviceType) is false)
                {
                    continue;
                }

                foreach (MethodBase callee in GetCalledMethods(method))
                {
                    pending.Push(callee);
                }

                Type stateMachineType =
                    method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;

                if (stateMachineType is not null)
                {
                    pending.Push(stateMachineType.GetMethod(
                        nameof(IAsyncStateMachine.MoveNext),
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance));
                }
            }

            return reached;
        }

        private static bool IsOwnedBy(MethodBase method, Type serviceType)
        {
            for (Type type = method.DeclaringType; type is not null; type = type.DeclaringType)
            {
                if ((type.IsGenericType ? type.GetGenericTypeDefinition() : type) == serviceType)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<MethodBase> GetCalledMethods(MethodBase method)
        {
            byte[] il = method.GetMethodBody()?.GetILAsByteArray() ?? Array.Empty<byte>();

            Type[] typeArguments =
                method.DeclaringType.IsGenericType ? method.DeclaringType.GetGenericArguments() : null;

            Type[] methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;

            for (int position = 0; position < il.Length;)
            {
                OpCode opCode = il[position] == 0xFE
                    ? TwoByteOpCodes[il[position + 1]]
                    : OneByteOpCodes[il[position]];

                position += opCode.Size;

                if (opCode.OperandType == OperandType.InlineMethod)
                {
                    int token = BitConverter.ToInt32(il, position);

                    yield return Normalize(method.Module.ResolveMethod(
                        token, typeArguments, methodArguments));
                }

                position += opCode.OperandType switch
                {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineBrTarget or OperandType.ShortInlineI
                        or OperandType.ShortInlineVar => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, position)),
                    _ => 4,
                };
            }
        }

        // A generic instantiation and its definition are the same member for this purpose.
        private static MethodBase Normalize(MethodBase method)
        {
            if (method is MethodInfo { IsGenericMethod: true } genericMethod)
            {
                method = genericMethod.GetGenericMethodDefinition();
            }

            return method.DeclaringType is { IsGenericType: true, IsGenericTypeDefinition: false }
                ? MethodBase.GetMethodFromHandle(
                    method.MethodHandle,
                    method.DeclaringType.GetGenericTypeDefinition().TypeHandle)
                : method;
        }

        private static readonly OpCode[] OneByteOpCodes = CreateOpCodeTable(twoByte: false);
        private static readonly OpCode[] TwoByteOpCodes = CreateOpCodeTable(twoByte: true);

        private static OpCode[] CreateOpCodeTable(bool twoByte)
        {
            var table = new OpCode[256];

            IEnumerable<OpCode> opCodes = typeof(OpCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => (OpCode)field.GetValue(null))
                .Where(opCode => (opCode.Size == 2) == twoByte);

            foreach (OpCode opCode in opCodes)
            {
                table[(ushort)opCode.Value & 0xFF] = opCode;
            }

            return table;
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
