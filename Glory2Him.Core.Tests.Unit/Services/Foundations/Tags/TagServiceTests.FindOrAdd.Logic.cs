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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Tags;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    /// <summary>
    /// The find-or-add's read by name authors its condition as a query-shaping function and
    /// hands it to the storage broker (§ARC12.2.1 rule 3). The mocked broker APPLIES the
    /// function it is handed to the in-memory set each test seeds (rule 5), so these tests
    /// execute the condition rather than merely observing that one was passed.
    /// </summary>
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldFindTheTagThatHoldsTheNameAsync()
        {
            // given: the named row beside a row of another name, stored ahead of it, so a read
            // that takes the first row rather than the named one answers the wrong tag
            string inputName = GetRandomTagName();
            Tag namedTag = CreateRandomTag();
            namedTag.Name = inputName;
            Tag otherTag = CreateRandomTag();
            otherTag.Name = GetRandomTagName();
            var storageTags = new List<Tag> { otherTag, namedTag };
            Tag inputTag = CreateRandomTag();
            inputTag.Name = inputName;
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            var expectedTagNameResult = new TagNameResult
            {
                Id = namedTag.Id,
                Outcome = TagNameOutcome.Found
            };

            SetupTagNameReadOver(storageTags, inputCancellationToken);

            // when
            TagNameResult actualTagNameResult =
                await this.tagService.FindOrAddTagAsync(
                    inputTag,
                    inputCancellationToken);

            // then
            actualTagNameResult.Should().BeEquivalentTo(expectedTagNameResult);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputTag),
                Times.Once);

            VerifyTagNameReadAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // a name the add accepts: at most 30 characters (§3 rule 3)
        private static string GetRandomTagName() =>
            GetRandomStringWithLengthOf(length: 20);

        private void SetupTagNameReadOver(
            IEnumerable<Tag> storageTags,
            CancellationToken cancellationToken) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagsAsync(
                    It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                    cancellationToken))
                        .ReturnsAsync((
                            Func<IQueryable<Tag>, IQueryable<TagNameMatch>> query,
                            CancellationToken _) =>
                                (IReadOnlyList<TagNameMatch>)
                                    query(storageTags.AsQueryable()).ToList());

        private void VerifyTagNameReadAsked(CancellationToken cancellationToken, Times times) =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagsAsync(
                    It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                    cancellationToken),
                times);
    }
}
