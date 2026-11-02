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
using Glory2Him.Core.Models.Orchestrations.Associations;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnSuggestTagIfCancellationRequestedAsync()
        {
            // given: a caller who has already cancelled is answered with their own cancellation,
            // asked before the null check, as the upsert asks it (§1 rule 5)
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    cancellationTokenSource.Token);

            // then: nothing is minted, read or logged
            await Assert.ThrowsAsync<OperationCanceledException>(suggestTagTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
