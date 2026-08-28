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
using Glory2Him.Core.Models.Enums;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItemSettings;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItemSettings
{
    [Collection(nameof(ApiTestCollection))]
    public partial class ContentItemSettingApiTests
    {
        private readonly ApiBroker apiBroker;

        public ContentItemSettingApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last. Every write on this exposer is Admin-only (§14.7 posture
            // C), and the seeded administrator is the only caller who holds it.
            this.apiBroker.ActAsSeededAdministrator();
        }

        /// <summary>
        /// Spreads the suite's rows across the content types rather than piling them all on one.
        /// An interlocked counter rather than a random pick, because a random one repeats.
        ///
        /// <para>No longer load-bearing for uniqueness: the rows this suite creates are per-item
        /// OVERRIDES keyed on <c>ContentItemId</c>, so their content type need not be distinct.
        /// It stays because a suite that only ever exercised one content type would be a thinner
        /// test than one that walks them all.</para>
        /// </summary>
        private static int scopeCounter = -1;

        private static ContentType GetUnusedContentType()
        {
            ContentType[] contentTypes = Enum.GetValues<ContentType>();
            int next = Interlocked.Increment(ref scopeCounter);

            return contentTypes[next % contentTypes.Length];
        }

        private int GetRandomNumber() =>
            new IntRange(min: 2, max: 5).GetValue();

        private static ContentItemSetting UpdateContentItemSettingWithRandomValues(
            ContentItemSetting inputContentItemSetting)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var updatedContentItemSetting = CreateRandomContentItemSetting();
            updatedContentItemSetting.Id = inputContentItemSetting.Id;

            // The scope is carried forward. It is not caller-editable in any meaningful sense —
            // moving a row to another scope would be creating a different policy — and randomising
            // it would collide with whatever already occupies the target.
            updatedContentItemSetting.ContentType = inputContentItemSetting.ContentType;
            updatedContentItemSetting.ContentItemId = inputContentItemSetting.ContentItemId;

            updatedContentItemSetting.CreatedWhen = inputContentItemSetting.CreatedWhen;
            updatedContentItemSetting.CreatedBy = inputContentItemSetting.CreatedBy;
            updatedContentItemSetting.UpdatedWhen = now;
            updatedContentItemSetting.IsDeleted = inputContentItemSetting.IsDeleted;
            updatedContentItemSetting.DeletionReason = inputContentItemSetting.DeletionReason;

            return updatedContentItemSetting;
        }

        private async ValueTask<ContentItemSetting> PostRandomContentItemSettingAsync()
        {
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();

            ContentItemSetting createdContentItemSetting =
                await this.apiBroker.PostContentItemSettingAsync(randomContentItemSetting);

            return createdContentItemSetting;
        }

        private async ValueTask<List<ContentItemSetting>> PostRandomContentItemSettingsAsync()
        {
            int randomNumber = GetRandomNumber();
            var randomContentItemSettings = new List<ContentItemSetting>();

            for (int i = 0; i < randomNumber; i++)
            {
                randomContentItemSettings.Add(await PostRandomContentItemSettingAsync());
            }

            return randomContentItemSettings;
        }

        private static ContentItemSetting CreateRandomContentItemSetting() =>
            CreateRandomContentItemSettingFiller().Create();

        private static Filler<ContentItemSetting> CreateRandomContentItemSettingFiller()
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var filler = new Filler<ContentItemSetting>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(now)
                .OnType<DateTimeOffset?>().Use(now)

                // A per-ITEM OVERRIDE row, not a per-type default. The host seeds one default per
                // content type at startup (ContentItemSettingSeedData), so every slot under
                // UX_ContentItemSettings_DefaultPerType is already taken and a suite writing
                // defaults would 409 on its first post. UX_ContentItemSettings_OverridePerEntity
                // is keyed on ContentItemId instead, and a fresh Guid per row never collides —
                // an unlimited supply, unlike the content types.
                //
                // ContentItemId carries no foreign key, so these ids name no real content item.
                // That is the schema's choice rather than this suite's convenience.
                .OnProperty(contentItemSetting => contentItemSetting.ContentType)
                    .Use(new Func<ContentType>(GetUnusedContentType))
                .OnProperty(contentItemSetting => contentItemSetting.ContentItemId)
                    .Use(new Func<Guid?>(() => Guid.NewGuid()))

                .OnProperty(contentItemSetting => contentItemSetting.IsDeleted).Use(false)
                .OnProperty(contentItemSetting => contentItemSetting.DeletionReason).Use((string)null)
                .OnProperty(contentItemSetting => contentItemSetting.DeletedBy).Use((string)null)
                .OnProperty(contentItemSetting => contentItemSetting.DeletedWhen).Use((DateTimeOffset?)null)

                .OnProperty(contentItemSetting => contentItemSetting.CreatedWhen).Use(now)
                .OnProperty(contentItemSetting => contentItemSetting.CreatedBy).Use(user)
                .OnProperty(contentItemSetting => contentItemSetting.UpdatedWhen).Use(now)
                .OnProperty(contentItemSetting => contentItemSetting.UpdatedBy).Use(user);

            return filler;
        }
    }
}
