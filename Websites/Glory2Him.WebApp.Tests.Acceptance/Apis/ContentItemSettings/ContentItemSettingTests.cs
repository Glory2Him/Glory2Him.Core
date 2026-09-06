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
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItems;
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItemSettings;
using Tynamix.ObjectFiller;
using CoreContentItemSetting = Glory2Him.Core.Models.Foundations.ContentItemSettings.ContentItemSetting;

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
            // whichever test ran last. Every write on this exposer is Administrators-only (§14.7 posture
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


        // ── A REAL CONTENT ITEM BEHIND EVERY OVERRIDE ───────────────────────────────
        //
        // The suite used to mint a fresh Guid per override and say so: "ContentItemId carries no
        // foreign key, so these ids name no real content item." That laxity was the bug — a
        // publisher could point an override at another content type's item and take its only
        // override scope. The orchestration now DERIVES an override's ContentType from the item
        // it names (#450), so an id that names nothing is refused, and these fixtures have to be
        // as real as the rule they exercise.
        //
        // The content type is chosen by the CALLER of this helper, because the derivation makes
        // the item's type the row's type: a test that wants a Devotional override must stand a
        // Devotional up first.
        private async ValueTask<ContentItem> PostRandomContentItemOfTypeAsync(ContentType contentType)
        {
            ContentItem randomContentItem = CreateRandomContentItemFiller(contentType).Create();

            return await this.apiBroker.PostContentItemAsync(randomContentItem);
        }

        // An override whose ContentItemId names a real item of the given type, and whose
        // ContentType already agrees with it — so the derivation confirms rather than corrects.
        private async ValueTask<ContentItemSetting> CreateRandomOverrideSettingAsync(
            ContentType contentType = ContentType.Story)
        {
            ContentItem contentItem = await PostRandomContentItemOfTypeAsync(contentType);
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            randomContentItemSetting.ContentType = contentItem.ContentType;
            randomContentItemSetting.ContentItemId = contentItem.Id;

            return randomContentItemSetting;
        }

        private static Filler<ContentItem> CreateRandomContentItemFiller(ContentType contentType)
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var filler = new Filler<ContentItem>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(now)
                .OnType<DateTimeOffset?>().Use(now)

                .OnProperty(contentItem => contentItem.ContentType).Use(contentType)
                .OnProperty(contentItem => contentItem.Title)
                    .Use(new Func<string>(() => $"Acceptance content item {Guid.NewGuid():N}"))
                .OnProperty(contentItem => contentItem.Author).Use("Acceptance suite")

                // Distinct per item: §3.4.2 refuses a duplicate by (ContentType, ContentHash)
                // across non-deleted rows, so two fixtures sharing content would silently stop
                // creating rows.
                .OnProperty(contentItem => contentItem.Content)
                    .Use(new Func<string>(() =>
                        $"Body written by the acceptance suite. {Guid.NewGuid():N}"))

                // Control fields (§12.4.1 rule 6) — never accepted from a caller. Sent as their
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

        private async ValueTask<ContentItemSetting> PostRandomContentItemSettingAsync()
        {
            ContentItemSetting randomContentItemSetting = await CreateRandomOverrideSettingAsync();

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

        /// <summary>
        /// A soft-deleted per-type DEFAULT, built as a Core row because it cannot be arranged
        /// through the API: the delete endpoint refuses a default outright, every content type
        /// having to keep one (#387, §12.5.2 business rule 5). The only caller is the test that
        /// asserts <c>UX_ContentItemSettings_DefaultPerType</c> still releases a scope held by
        /// nothing but a dead row.
        ///
        /// <para>Written by hand rather than through the filler because the filler mints
        /// OVERRIDES — a fresh <c>ContentItemId</c> on every row — and this is the one shape it
        /// deliberately never produces.</para>
        /// </summary>
        private static CoreContentItemSetting CreateSoftDeletedCoreDefaultContentItemSetting(
            ContentType contentType)
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new CoreContentItemSetting
            {
                Id = Guid.NewGuid(),
                ContentType = contentType,
                ContentItemId = null,
                CreatedBy = user,
                CreatedWhen = now,
                UpdatedBy = user,
                UpdatedWhen = now,
                IsDeleted = true,
                DeletedBy = user,
                DeletedWhen = now,
                DeletionReason = "Arranged beneath HTTP for the scope-release assertion."
            };
        }

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
