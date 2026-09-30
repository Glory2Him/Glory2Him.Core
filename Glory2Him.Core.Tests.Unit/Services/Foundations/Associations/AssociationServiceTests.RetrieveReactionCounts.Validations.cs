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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Theory]
        [MemberData(nameof(ReactionCountRequestsWithANullList))]
        public async Task ShouldThrowValidationExceptionOnRetrieveReactionCountsIfAListIsNullAndLogItAsync(
            IReadOnlyList<Guid> invalidContentItemGroupIds,
            IReadOnlyList<Guid> invalidReactionIds,
            string invalidParameterName)
        {
            // given
            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: invalidParameterName,
                value: "List is required");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidAssociationException);

            // when
            ValueTask<IReadOnlyList<AssociationPairCount>> retrieveReactionCountsTask =
                this.associationService.RetrieveContentItemReactionCountsAsync(
                    invalidContentItemGroupIds,
                    invalidReactionIds,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    retrieveReactionCountsTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        public static TheoryData<IReadOnlyList<Guid>, IReadOnlyList<Guid>, string>
            ReactionCountRequestsWithANullList() =>
            new TheoryData<IReadOnlyList<Guid>, IReadOnlyList<Guid>, string>
            {
                { null, new List<Guid> { Guid.NewGuid() }, "contentItemGroupIds" },
                { new List<Guid> { Guid.NewGuid() }, null, "reactionIds" }
            };
    }
}
