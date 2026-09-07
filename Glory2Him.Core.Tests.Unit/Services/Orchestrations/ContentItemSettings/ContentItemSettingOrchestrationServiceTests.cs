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
using System.Linq.Expressions;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;
using Glory2Him.Core.Services.Orchestrations.ContentItemSettings;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItemSettings
{
    // THE ONE FLOW THAT SPANS TWO ENTITY TYPES. This service exists to stop a row deciding who may
    // write it: the gate composes the publisher tier from an override's ContentType, so that value
    // is derived from the item the row names rather than accepted from the caller (#450, §12.5.2
    // business rule 6). Everything else is delegation, and the delegation is asserted too —
    // a pass-through that quietly drops an argument is the failure this layer could hide.
    public partial class ContentItemSettingOrchestrationServiceTests
    {
        private readonly Mock<IContentItemSettingService> contentItemSettingServiceMock;
        private readonly Mock<IContentItemService> contentItemServiceMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;

        private readonly IContentItemSettingOrchestrationService
            contentItemSettingOrchestrationService;

        // Two DIFFERENT members, pinned rather than drawn: a derivation test that let the filler
        // pick both types would pass whenever the draw happened to agree, proving nothing about
        // the overwrite it exists to exercise.
        //
        // BOTH ARE NON-ZERO, and that is load-bearing rather than incidental. ActualContentType was
        // Quote, which is 0, which is default(ContentType) — so a derivation that assigned nothing,
        // or assigned a constant zero, still produced the value the assertion looked for and the
        // test stayed green with the overwrite gone. The expected answer has to be a member no
        // uninitialised field can hold.
        private const ContentType CallerClaimedContentType = ContentType.Devotional;
        private const ContentType ActualContentType = ContentType.Story;

        public ContentItemSettingOrchestrationServiceTests()
        {
            this.contentItemSettingServiceMock = new Mock<IContentItemSettingService>();
            this.contentItemServiceMock = new Mock<IContentItemService>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // Valid by default. The verification tests override it; every other test on the event
            // path would otherwise be asserting the guard rather than its own subject.
            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            this.contentItemSettingOrchestrationService =
                new ContentItemSettingOrchestrationService(
                    contentItemSettingService: this.contentItemSettingServiceMock.Object,
                    contentItemService: this.contentItemServiceMock.Object,
                    envelopeIntegrityBroker: this.envelopeIntegrityBrokerMock.Object,
                    loggingBroker: this.loggingBrokerMock.Object);
        }

        // The foundation's answers that must stay validation-shaped through this layer, so the
        // exposer maps them to the status codes it always did.
        public static TheoryData<Xeption> ContentItemSettingValidationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions
                    .ContentItemSettingValidationException(
                        message: randomMessage, innerException: innerException),
            };
        }

        public static TheoryData<Xeption> ContentItemSettingDependencyValidationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions
                    .ContentItemSettingDependencyValidationException(
                        message: randomMessage, innerException: innerException),
            };
        }

        public static TheoryData<Xeption> ContentItemSettingDependencyExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions
                    .ContentItemSettingDependencyException(
                        message: randomMessage, innerException: innerException),
            };
        }

        // A SERVICE failure is its own category and answers 500. It sat in the dependency theory
        // above until review pointed out that routing it to the dependency wrapper had quietly
        // moved every endpoint from 500 to 424.
        public static TheoryData<Xeption> ContentItemSettingServiceExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions
                    .ContentItemSettingServiceException(
                        message: randomMessage, innerException: innerException),
            };
        }

        // THE OTHER FOUNDATION'S failures — raised while the content type is being derived. These
        // are what the `catch (Xeption)` clause exists for: a store that could not answer is a
        // dependency problem, not a bug in this service.
        public static TheoryData<Xeption> ContentItemDownstreamExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.ContentItems.Exceptions
                    .ContentItemDependencyException(
                        message: randomMessage, innerException: innerException),

                new Glory2Him.Core.Models.Foundations.ContentItems.Exceptions
                    .ContentItemDependencyValidationException(
                        message: randomMessage, innerException: innerException),

                new Glory2Him.Core.Models.Foundations.ContentItems.Exceptions
                    .ContentItemServiceException(
                        message: randomMessage, innerException: innerException),
            };
        }

        // An OVERRIDE as the caller sends it: it names an item, and it claims a content type that
        // the item will contradict.
        private static ContentItemSetting CreateRandomOverrideRequest() =>
            new ContentItemSetting
            {
                Id = Guid.NewGuid(),
                ContentItemId = Guid.NewGuid(),
                ContentType = CallerClaimedContentType,
                ContentTypeName = GetRandomString(),
            };

        private static ContentItem CreateContentItemOfType(Guid contentItemId, ContentType contentType) =>
            new ContentItem
            {
                Id = contentItemId,
                ContentType = contentType,
            };

        private static ContentItemSetting CreateRandomContentItemSetting() =>
            new ContentItemSetting
            {
                Id = Guid.NewGuid(),
                ContentType = CallerClaimedContentType,
            };

        // A request envelope as the substrate hands one over: content, an identity, and the event
        // id ProcessedEvents deduplicates on. Integrity is left to the broker mock, which answers
        // for the signature rather than recomputing one.
        private static EventEnvelope<ContentItemSetting> CreateRequestEnvelope(
            ContentItemSetting? contentItemSetting) =>
            new EventEnvelope<ContentItemSetting>
            {
                Content = contentItemSetting!,
                SecurityContext = new SecurityContext { IsAuthenticated = true },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },
            };

        // An envelope with no metadata: nothing to deduplicate on, and Metadata.EventId is
        // dereferenced further down, so the shape guard has to refuse it rather than let it reach
        // the read.
        private static EventEnvelope<ContentItemSetting> CreateRequestEnvelopeWithoutMetadata(
            ContentItemSetting contentItemSetting) =>
            new EventEnvelope<ContentItemSetting>
            {
                Content = contentItemSetting,
                SecurityContext = new SecurityContext { IsAuthenticated = true },
                Metadata = null!,
            };

        private static Guid GetRandomId() => Guid.NewGuid();

        private static string GetRandomString() =>
            new MnemonicString(wordCount: 1).GetValue();

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);
    }
}
