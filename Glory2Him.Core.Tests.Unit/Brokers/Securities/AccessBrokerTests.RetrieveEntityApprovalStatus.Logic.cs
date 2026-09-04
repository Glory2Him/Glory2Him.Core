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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        /// <summary>
        /// What a round is opened AT (§9.2 rules 1–2): the status the entity's own row carries,
        /// read from storage rather than inferred.
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Approved)]
        public async Task ShouldRetrieveTheStatusTheEntityRowCarriesAsync(ApprovalStatus storedStatus)
        {
            // given
            var entityId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(entityId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ContentItem
                    {
                        Id = entityId,
                        CreatedBy = GetRandomString(),
                        ContentType = ContentType.Story,
                        ApprovalStatus = storedStatus,
                    });

            // when
            ApprovalStatus? actualStatus = await this.accessBroker.RetrieveEntityApprovalStatusAsync(
                EntityType.ContentItem,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            actualStatus.Should().Be(storedStatus);
        }

        [Fact]
        public async Task ShouldRetrieveTheStatusOffEveryEntityTypesOwnTableAsync()
        {
            // given: a tag, to prove the read is not ContentItem-shaped
            var entityId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(entityId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Tag
                    {
                        Id = entityId,
                        CreatedBy = GetRandomString(),
                        ApprovalStatus = ApprovalStatus.Submitted,
                    });

            // when
            ApprovalStatus? actualStatus = await this.accessBroker.RetrieveEntityApprovalStatusAsync(
                EntityType.Tag,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            actualStatus.Should().Be(ApprovalStatus.Submitted);
        }

        /// <summary>
        /// A row that cannot be read answers NULL, never Draft: the two are different facts, and
        /// the resolution treats the first as "open at Draft" only because nothing may enter
        /// review on a status nobody could see — the decision is the caller's, not this read's.
        /// </summary>
        [Fact]
        public async Task ShouldRetrieveNullWhenTheEntityRowCannotBeReadAsync()
        {
            // given: no row stubbed, so the store answers null
            var entityId = Guid.NewGuid();

            // when
            ApprovalStatus? actualStatus = await this.accessBroker.RetrieveEntityApprovalStatusAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            actualStatus.Should().BeNull();
        }
    }
}
