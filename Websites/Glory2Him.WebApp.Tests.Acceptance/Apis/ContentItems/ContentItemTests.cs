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
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItems;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItems
{
    [Collection(nameof(ApiTestCollection))]
    public partial class ContentItemApiTests
    {
        private readonly ApiBroker apiBroker;

        public ContentItemApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last.
            this.apiBroker.ActAsSeededAdministrator();
        }

        private int GetRandomNumber() =>
            new IntRange(min: 2, max: 5).GetValue();

        private static DateTimeOffset GetRandomDateTime() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static string GetRandomStringWithLengthOf(int length)
        {
            string result = new MnemonicString(wordCount: 1, wordMinLength: length, wordMaxLength: length).GetValue();

            return result.Length > length ? result.Substring(0, length) : result;
        }

        private static ContentItem UpdateContentItemWithRandomValues(ContentItem inputContentItem)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var updatedContentItem = CreateRandomContentItem();
            updatedContentItem.Id = inputContentItem.Id;
            updatedContentItem.CreatedWhen = inputContentItem.CreatedWhen;
            updatedContentItem.CreatedBy = inputContentItem.CreatedBy;
            updatedContentItem.UpdatedWhen = now;
            updatedContentItem.IsDeleted = inputContentItem.IsDeleted;
            updatedContentItem.DeletionReason = inputContentItem.DeletionReason;
            updatedContentItem.ContentType = inputContentItem.ContentType;
            updatedContentItem.ContentHash = inputContentItem.ContentHash;
            updatedContentItem.GroupId = inputContentItem.GroupId;
            updatedContentItem.Version = inputContentItem.Version;
            updatedContentItem.IsPublished = inputContentItem.IsPublished;
            updatedContentItem.PublishDate = inputContentItem.PublishDate;
            updatedContentItem.ApprovalStatus = inputContentItem.ApprovalStatus;
            updatedContentItem.IsApprovedByBypass = inputContentItem.IsApprovedByBypass;
            updatedContentItem.ApprovedByBypassReason = inputContentItem.ApprovedByBypassReason;

            return updatedContentItem;
        }

        private async ValueTask<ContentItem> PostRandomContentItemAsync()
        {
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem createdContentItem = await this.apiBroker.PostContentItemAsync(randomContentItem);

            return createdContentItem;
        }

        private async ValueTask<List<ContentItem>> PostRandomContentItemsAsync()
        {
            int randomNumber = GetRandomNumber();
            var randomContentItems = new List<ContentItem>();

            for (int i = 0; i < randomNumber; i++)
            {
                randomContentItems.Add(await PostRandomContentItemAsync());
            }

            return randomContentItems;
        }

        private static ContentItem CreateRandomContentItem() =>
            CreateRandomContentItemFiller().Create();

        private static Filler<ContentItem> CreateRandomContentItemFiller()
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var filler = new Filler<ContentItem>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(now)
                .OnType<DateTimeOffset?>().Use(now)

                // The caller-editable content. A new item must arrive unpublished and in Draft
                // with no bypass - the processing service rejects anything else.
                .OnProperty(contentItem => contentItem.ContentType).Use(ContentType.Story)
                .OnProperty(contentItem => contentItem.Title)
                    .Use(new Func<string>(GetRandomTitle))
                .OnProperty(contentItem => contentItem.Author).Use("Acceptance suite")

                // Distinct per item, and that is load-bearing: 3.4.2 refuses a duplicate by
                // (ContentType, ContentHash) across non-deleted rows and the hash is derived from
                // this, so two fixtures sharing content would silently stop creating rows.
                .OnProperty(contentItem => contentItem.Content)
                    .Use(new Func<string>(GetRandomContent))

                // Control fields (12.4.1 rule 6) - never accepted from a caller. Sent as their
                // defaults so a request cannot be read as an attempt to set them.
                .OnProperty(contentItem => contentItem.ContentHash).Use((string)null)
                .OnProperty(contentItem => contentItem.GroupId).Use(Guid.Empty)
                .OnProperty(contentItem => contentItem.Version).Use(0)
                .OnProperty(contentItem => contentItem.IsPublished).Use(false)
                .OnProperty(contentItem => contentItem.PublishDate).Use((DateTimeOffset?)null)
                .OnProperty(contentItem => contentItem.ApprovalStatus).Use(ApprovalStatus.Draft)
                .OnProperty(contentItem => contentItem.IsApprovedByBypass).Use(false)
                .OnProperty(contentItem => contentItem.ApprovedByBypassReason).Use((string)null)
                .OnProperty(contentItem => contentItem.IsDeleted).Use(false)
                .OnProperty(contentItem => contentItem.DeletionReason).Use((string)null)
                .OnProperty(contentItem => contentItem.DeletedBy).Use((string)null)
                .OnProperty(contentItem => contentItem.DeletedWhen).Use((DateTimeOffset?)null)

                .OnProperty(contentItem => contentItem.CreatedWhen).Use(now)
                .OnProperty(contentItem => contentItem.CreatedBy).Use(user)
                .OnProperty(contentItem => contentItem.UpdatedWhen).Use(now)
                .OnProperty(contentItem => contentItem.UpdatedBy).Use(user);

            return filler;
        }

        private static string GetRandomTitle() =>
            $"Acceptance content item {Guid.NewGuid():N}";

        private static string GetRandomContent() =>
            $"Body written by the acceptance suite. {Guid.NewGuid():N}";
    }
}
