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
using Glory2Him.Core.Models.Enums;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Glory2Him.WebApp.Tests.Acceptance.Models.Tags;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Tags
{
    [Collection(nameof(ApiTestCollection))]
    public partial class TagApiTests
    {
        private readonly ApiBroker apiBroker;

        public TagApiTests(ApiBroker apiBroker) =>
            this.apiBroker = apiBroker;

        private int GetRandomNumber() =>
            new IntRange(min: 2, max: 5).GetValue();

        private static DateTimeOffset GetRandomDateTime() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static string GetRandomStringWithLengthOf(int length)
        {
            string result = new MnemonicString(wordCount: 1, wordMinLength: length, wordMaxLength: length).GetValue();

            return result.Length > length ? result.Substring(0, length) : result;
        }

        private static Tag UpdateTagWithRandomValues(Tag inputTag)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var updatedTag = CreateRandomTag();
            updatedTag.Id = inputTag.Id;
            updatedTag.CreatedWhen = inputTag.CreatedWhen;
            updatedTag.CreatedBy = inputTag.CreatedBy;
            updatedTag.UpdatedWhen = now;
            updatedTag.IsDeleted = inputTag.IsDeleted;
            updatedTag.DeletionReason = inputTag.DeletionReason;
            updatedTag.IsPublished = inputTag.IsPublished;
            updatedTag.PublishDate = inputTag.PublishDate;
            updatedTag.ApprovalStatus = inputTag.ApprovalStatus;
            updatedTag.IsApprovedByBypass = inputTag.IsApprovedByBypass;
            updatedTag.ApprovedByBypassReason = inputTag.ApprovedByBypassReason;

            return updatedTag;
        }

        private async ValueTask<Tag> PostRandomTagAsync()
        {
            Tag randomTag = CreateRandomTag();
            Tag createdTag = await this.apiBroker.PostTagAsync(randomTag);

            return createdTag;
        }

        private async ValueTask<List<Tag>> PostRandomTagsAsync()
        {
            int randomNumber = GetRandomNumber();
            var randomTags = new List<Tag>();

            for (int i = 0; i < randomNumber; i++)
            {
                randomTags.Add(await PostRandomTagAsync());
            }

            return randomTags;
        }

        private static Tag CreateRandomTag() =>
            CreateRandomTagFiller().Create();

        private static Filler<Tag> CreateRandomTagFiller()
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var filler = new Filler<Tag>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(now)
                .OnType<DateTimeOffset?>().Use(now)

                // Name is unique-indexed and capped at 30 characters, and a new tag must arrive
                // unpublished and in Draft with no bypass — the foundation rejects anything else.
                .OnProperty(tag => tag.Name).Use(new Func<string>(GetRandomTagName))
                .OnProperty(tag => tag.IsPublished).Use(false)
                .OnProperty(tag => tag.PublishDate).Use((DateTimeOffset?)null)
                .OnProperty(tag => tag.ApprovalStatus).Use(ApprovalStatus.Draft)
                .OnProperty(tag => tag.IsApprovedByBypass).Use(false)
                .OnProperty(tag => tag.ApprovedByBypassReason).Use((string)null)
                .OnProperty(tag => tag.IsDeleted).Use(false)
                .OnProperty(tag => tag.DeletionReason).Use((string)null)
                .OnProperty(tag => tag.DeletedBy).Use((string)null)
                .OnProperty(tag => tag.DeletedWhen).Use((DateTimeOffset?)null)

                .OnProperty(tag => tag.CreatedWhen).Use(now)
                .OnProperty(tag => tag.CreatedBy).Use(user)
                .OnProperty(tag => tag.UpdatedWhen).Use(now)
                .OnProperty(tag => tag.UpdatedBy).Use(user);

            return filler;
        }

        private static string GetRandomTagName() =>
            Guid.NewGuid().ToString("N").Substring(0, 30);
    }
}
