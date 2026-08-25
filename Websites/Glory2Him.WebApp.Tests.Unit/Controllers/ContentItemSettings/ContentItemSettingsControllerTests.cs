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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;
using Glory2Him.WebApp.Controllers.ContentItemSettings;
using Moq;
using RESTFulSense.Controllers;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ContentItemSettings
{
    public partial class ContentItemSettingsControllerTests : RESTFulController
    {
        private readonly Mock<IContentItemSettingService> contentItemSettingServiceMock;
        private readonly ContentItemSettingsController contentItemSettingsController;

        public ContentItemSettingsControllerTests()
        {
            contentItemSettingServiceMock = new Mock<IContentItemSettingService>();
            contentItemSettingsController = new ContentItemSettingsController(contentItemSettingServiceMock.Object);
        }

        public static TheoryData<Xeption> ValidationExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ContentItemSettingValidationException(
                    message: someMessage,
                    innerException: someInnerException),

                new ContentItemSettingDependencyValidationException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        public static TheoryData<Xeption> DependencyExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ContentItemSettingDependencyException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        public static TheoryData<Xeption> ServerExceptions()
        {
            var someInnerException = new Xeption();
            string someMessage = GetRandomString();

            return new TheoryData<Xeption>
            {
                new ContentItemSettingServiceException(
                    message: someMessage,
                    innerException: someInnerException)
            };
        }

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static string GetRandomStringWithLengthOf(int length)
        {
            string result = new MnemonicString(wordCount: 1, wordMinLength: length, wordMaxLength: length).GetValue();

            return result.Length > length ? result.Substring(0, length) : result;
        }

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static ContentItemSetting CreateRandomContentItemSetting() =>
            CreateContentItemSettingFiller().Create();

        private static IQueryable<ContentItemSetting> CreateRandomContentItemSettings()
        {
            return CreateContentItemSettingFiller()
                .Create(count: GetRandomNumber())
                    .AsQueryable();
        }

        private static Filler<ContentItemSetting> CreateContentItemSettingFiller()
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            string user = Guid.NewGuid().ToString();
            var filler = new Filler<ContentItemSetting>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(dateTimeOffset)
                .OnType<DateTimeOffset?>().Use(dateTimeOffset)
                .OnProperty(contentItemSetting => contentItemSetting.CreatedBy).Use(user)
                .OnProperty(contentItemSetting => contentItemSetting.UpdatedBy).Use(user);

            return filler;
        }
    }
}
