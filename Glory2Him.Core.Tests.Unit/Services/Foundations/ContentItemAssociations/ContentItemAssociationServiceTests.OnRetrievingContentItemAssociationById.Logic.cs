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

using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldReplyWithContentItemAssociationOnRetrievingContentItemAssociationByIdEventAsync()
        {
            // given
            ContentItemAssociation randomContentItemAssociation = CreateRandomContentItemAssociation();
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation;
            ContentItemAssociation expectedContentItemAssociation = storageContentItemAssociation;

            var requestEnvelope = new EventEnvelope<ContentItemAssociation>
            {
                Content = new ContentItemAssociation { Id = randomContentItemAssociation.Id }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            // when
            EventEnvelope<ContentItemAssociation>? actualReplyEnvelope =
                await this.contentItemAssociationService.OnRetrievingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(requestEnvelope, storageContentItemAssociation),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
