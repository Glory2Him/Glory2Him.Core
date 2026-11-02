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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Theory]
        [MemberData(nameof(RefusedContributorSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnFindOrAddIfUserMayNotContributeAndLogItAsync(
            SecurityContext refusedSecurityContext,
            string expectedRefusalMessage)
        {
            // given: the caller the envelope captures is anonymous or blocked from contributing
            // tags; the gate is asked before anything is read (§3 rule 2)
            this.ambientSecurityContext = refusedSecurityContext;
            Tag someTag = CreateRandomTag();
            someTag.Name = GetRandomTagName();

            var unauthorizedTagException =
                new UnauthorizedTagException(message: expectedRefusalMessage);

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedTagException);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    findOrAddTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(someTag),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindOrAddIfTagIsNullAndLogItAsync()
        {
            // given
            Tag nullTag = null;

            var nullTagException =
                new NullTagException(message: "Tag is null.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: nullTagException);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    nullTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    findOrAddTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnFindOrAddIfNameIsInvalidAndLogItAsync(
            string invalidName)
        {
            // given
            Tag invalidTag = CreateRandomTag();
            invalidTag.Name = invalidName;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.Name),
                values: "Text is required");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    findOrAddTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(invalidTag),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindOrAddIfNameExceedsMaxLengthAndLogItAsync()
        {
            // given: one character past the cap, so a cap of 31 or more passes it
            Tag invalidTag = CreateRandomTag();
            invalidTag.Name = GetRandomStringWithLengthOf(31);

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.Name),
                values: "Text exceed max length of 30 characters");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    findOrAddTagTask.AsTask);

            // then
            invalidTag.Name.Length.Should().Be(31);

            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(invalidTag),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(FieldsTheAddRefuses))]
        public async Task ShouldThrowValidationExceptionOnFindOrAddIfTheAddRefusesTheTagAndLogItAsync(
            string refusedField,
            string expectedRefusalMessage)
        {
            // given: no row holds the name, so the handed tag reaches the add, whose own
            // validations refuse a tag already published or already approved (§3 rule 6)
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag invalidTag = CreateTagFiller(randomDateTimeOffset).Create();
            invalidTag.Name = GetRandomTagName();

            if (refusedField == nameof(Tag.IsPublished))
            {
                invalidTag.IsPublished = true;
            }
            else
            {
                invalidTag.ApprovalStatus = ApprovalStatus.Approved;
            }

            Tag otherTag = CreateRandomTag();
            otherTag.Name = GetRandomTagName();
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: refusedField,
                values: expectedRefusalMessage);

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            SetupTagNameReadOver(new List<Tag> { otherTag }, inputCancellationToken);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidTag, this.ambientSecurityContext))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(invalidTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    invalidTag,
                    inputCancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    findOrAddTagTask.AsTask);

            // then: refused as the add refuses it, and nothing written or published
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            VerifyTagNameReadAsked(inputCancellationToken, Times.Once());

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // the fields of a handed tag the add refuses, with the refusal it names (§3 rule 6)
        public static TheoryData<string, string> FieldsTheAddRefuses() =>
            new TheoryData<string, string>
            {
                { nameof(Tag.IsPublished), "Value is not allowed on add" },
                { nameof(Tag.ApprovalStatus), "Value must be Draft or Submitted on add" }
            };
    }
}
