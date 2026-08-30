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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldAddBibleReferenceAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            BibleReference randomBibleReference = CreateBibleReferenceFiller(randomDateTimeOffset).Create();
            BibleReference inputBibleReference = randomBibleReference;
            BibleReference auditAppliedBibleReference = inputBibleReference.DeepClone();
            BibleReference storageBibleReference = auditAppliedBibleReference.DeepClone();
            BibleReference expectedBibleReference = storageBibleReference.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(), BibleReferenceEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                        new EventPublishResult<BibleReference>()));

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.AddBibleReferenceAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAddIfApprovalStatusIsSubmittedAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            BibleReference randomBibleReference = CreateBibleReferenceFiller(randomDateTimeOffset).Create();
            randomBibleReference.ApprovalStatus = ApprovalStatus.Submitted;
            BibleReference inputBibleReference = randomBibleReference;
            BibleReference auditAppliedBibleReference = inputBibleReference.DeepClone();
            BibleReference storageBibleReference = auditAppliedBibleReference.DeepClone();
            BibleReference expectedBibleReference = storageBibleReference.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(), BibleReferenceEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                        new EventPublishResult<BibleReference>()));

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.AddBibleReferenceAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(
            "<script>alert(1)</script><p class=\"wj\">Jesus said</p>",
            "<p class=\"wj\">Jesus said</p>")]

        [InlineData(
            "<div onclick=\"steal()\">plain</div>",
            "plain")]

        [InlineData(
            "<span class=\"evil\">text</span>",
            "<span>text</span>")]

        // An attribute-stripping regression case for a tag that IS in the allow-list — the
        // gap #359's review flagged: dangerous attributes must be stripped even off a tag that
        // survives, not just off tags that are dropped outright.
        [InlineData(
            "<p onclick=\"alert(1)\">Jesus said</p>",
            "<p>Jesus said</p>")]

        // A hyphenated custom-element tag name — one of the two confirmed bypasses of the prior
        // regex-based sanitizer (it never matched the tag pattern at all, so the whole tag,
        // including onclick, shipped untouched). The parser-backed sanitizer strips it like any
        // other disallowed tag.
        [InlineData(
            "<x-evil onclick=\"alert(1)\">click</x-evil>",
            "click")]

        // No whitespace before the attribute — the second confirmed regex bypass (a real HTML5
        // parser tokenizes '/' between a tag name and an attribute as attribute-separator
        // whitespace, executing a live remote script; the old regex never matched this either).
        [InlineData(
            "<script/src=\"//evil.example/x.js\">",
            "")]

        // A red-letter passage that's also a deity-name reference — a plausible real combination.
        // Multi-value class attributes are filtered per token, not dropped wholesale.
        [InlineData(
            "<p class=\"wj nd\">Jesus said, \"God\"</p>",
            "<p class=\"wj nd\">Jesus said, \"God\"</p>")]

        // Unquoted attribute values are legal HTML5; the sanitizer still recognizes the class.
        [InlineData(
            "<p class=wj>unquoted</p>",
            "<p class=\"wj\">unquoted</p>")]
        public async Task ShouldSanitizeScriptureHtmlOnAddAsync(
            string rawScriptureHtml,
            string expectedSanitizedScriptureHtml)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            BibleReference randomBibleReference = CreateBibleReferenceFiller(randomDateTimeOffset).Create();
            randomBibleReference.ScriptureHtml = rawScriptureHtml;
            BibleReference inputBibleReference = randomBibleReference;
            BibleReference auditAppliedBibleReference = inputBibleReference.DeepClone();
            BibleReference storageBibleReference = auditAppliedBibleReference.DeepClone();
            storageBibleReference.ScriptureHtml = expectedSanitizedScriptureHtml;
            BibleReference expectedBibleReference = storageBibleReference.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(), BibleReferenceEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                        new EventPublishResult<BibleReference>()));

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.AddBibleReferenceAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);
            auditAppliedBibleReference.ScriptureHtml.Should().Be(expectedSanitizedScriptureHtml);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Regression test for the ReDoS the prior regex-based sanitizer had: an unclosed tag
        // with many quoted attributes measured at 5+ seconds (and climbing) against the old
        // ScriptureHtmlTagPattern. The AngleSharp-backed sanitizer parses HTML in linear time by
        // construction, so the same shape of input must complete near-instantly.
        [Fact]
        public async Task ShouldSanitizeAdversarialScriptureHtmlWithoutHangingOnAddAsync()
        {
            // given
            string adversarialScriptureHtml =
                "<span" + string.Concat(Enumerable.Repeat(" a=\"b\"", 200));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            BibleReference randomBibleReference = CreateBibleReferenceFiller(randomDateTimeOffset).Create();
            randomBibleReference.ScriptureHtml = adversarialScriptureHtml;
            BibleReference inputBibleReference = randomBibleReference;
            BibleReference auditAppliedBibleReference = inputBibleReference.DeepClone();
            BibleReference storageBibleReference = auditAppliedBibleReference.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(), BibleReferenceEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                        new EventPublishResult<BibleReference>()));

            // when
            var stopwatch = Stopwatch.StartNew();

            await this.bibleReferenceService.AddBibleReferenceAsync(
                inputBibleReference,
                TestContext.Current.CancellationToken);

            stopwatch.Stop();

            // then
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        }
    }
}
