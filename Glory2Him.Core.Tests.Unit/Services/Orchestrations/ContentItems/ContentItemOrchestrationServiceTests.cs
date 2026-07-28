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
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Glory2Him.Core.Brokers.Hashes;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Orchestrations.ContentItems;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        private readonly Mock<IContentItemService> contentItemServiceMock;
        private readonly Mock<ISecurityBroker> securityBrokerMock;
        private readonly Mock<IHashBroker> hashBrokerMock;
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IContentItemOrchestrationService contentItemOrchestrationService;

        public ContentItemOrchestrationServiceTests()
        {
            this.contentItemServiceMock = new Mock<IContentItemService>();
            this.securityBrokerMock = new Mock<ISecurityBroker>();
            this.hashBrokerMock = new Mock<IHashBroker>();
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.contentItemOrchestrationService = new ContentItemOrchestrationService(
                contentItemService: this.contentItemServiceMock.Object,
                securityBroker: this.securityBrokerMock.Object,
                hashBroker: this.hashBrokerMock.Object,
                identifierBroker: this.identifierBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        public static TheoryData<Xeption> DependencyValidationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new ContentItemValidationException(
                    message: randomMessage,
                    innerException: innerException),

                new ContentItemDependencyValidationException(
                    message: randomMessage,
                    innerException: innerException)
            };
        }

        public static TheoryData<Xeption> DependencyExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new ContentItemDependencyException(
                    message: randomMessage,
                    innerException: innerException),

                new ContentItemServiceException(
                    message: randomMessage,
                    innerException: innerException)
            };
        }

        // Test-side twins of the frozen normalization + hashing contract (design §3.4.2):
        // any drift in the production implementation fails these tests.
        private static string NormalizeContent(string content) =>
            Regex.Replace(content.Trim(), pattern: @"\s+", replacement: " ")
                .ToLowerInvariant();

        private static string ComputeContentHash(string content)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeContent(content)));

            return Convert.ToHexStringLower(hashBytes);
        }

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static IQueryable<ContentItem> CreateRandomContentItems() =>
            CreateContentItemFiller(dateTimeOffset: GetRandomDateTimeOffset())
                .Create(count: GetRandomNumber())
                .AsQueryable();

        private static ContentItem CreateRandomContentItem() =>
            CreateContentItemFiller(dateTimeOffset: GetRandomDateTimeOffset()).Create();

        private static Filler<ContentItem> CreateContentItemFiller(DateTimeOffset dateTimeOffset)
        {
            var filler = new Filler<ContentItem>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                .OnProperty(contentItem => contentItem.ContentType).IgnoreIt();

            return filler;
        }
    }
}
