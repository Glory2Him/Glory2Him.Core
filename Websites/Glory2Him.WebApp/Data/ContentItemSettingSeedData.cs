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

using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.WebApp.Data
{
    // Idempotent seed for the per-ContentType default ContentItemSetting rows — the sibling of
    // SeedData.cs, which does the same for roles and users. It runs on every startup rather than
    // only the first: a content type whose live default has gone missing gets it back here.
    //
    // WRITTEN DIRECTLY THROUGH IStorageBroker, bypassing ContentItemSettingService. The
    // foundation enforces its own Administrators gate (design §14.6) by reading the SecurityContext
    // an inbound EventEnvelope carries, and that envelope is populated from the ambient
    // HttpContext at the moment it is created (CoreRegistration.cs). There is no HttpContext
    // during host startup, so the audited Add path is unreachable here — the same reason
    // SeedData.cs writes roles through RoleManager directly rather than through a Core
    // service. These rows are system-authored configuration, not a user action, so bypassing
    // the audit trail here is correct rather than a workaround.
    public static class ContentItemSettingSeedData
    {
        private const string SeededBy = "system-seed";

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IServiceProvider services = scope.ServiceProvider;
            var storageBroker = services.GetRequiredService<IStorageBroker>();

            IQueryable<ContentItemSetting> existingSettings =
                await storageBroker.SelectAllContentItemSettingsAsync();

            foreach (ContentItemSetting defaultSetting in BuildDefaultContentItemSettings())
            {
                // The IsDeleted term is what makes this a REPAIR rather than a first-run-only
                // insert. Every content type must always have a LIVE default (design §12.5.2
                // business rule 5), and a soft-deleted row is not a setting — §14.5 hides it from
                // every caller and §6.6 excludes it from resolution — so counting one here left a
                // content type that had lost its default never getting it back.
                //
                // ContentItemSettingService now refuses to remove a default by either path, so
                // this should never fire through the API. It fires for the routes the service
                // does not own: a direct write, a restore, a database seeded before that refusal
                // existed. The insert is safe because UX_ContentItemSettings_DefaultPerType
                // carries its own IsDeleted term (#326) — without it the scope would still be
                // held by the dead row and this insert would take Core initialisation down.
                bool alreadySeeded = await existingSettings.AnyAsync(
                    contentItemSetting =>
                        contentItemSetting.ContentType == defaultSetting.ContentType
                        && contentItemSetting.ContentItemId == null
                        && contentItemSetting.IsDeleted == false);

                if (alreadySeeded is false)
                {
                    await storageBroker.InsertContentItemSettingAsync(defaultSetting);
                }
            }
        }

        // One default row per ContentType member, keyed on (ContentType, ContentItemId: null)
        // — the per-type default scope UX_ContentItemSettings_DefaultPerType enforces (§6.2).
        // Values below are the reviewed defaults; adjust here rather than by hand-editing rows,
        // so a fresh environment always comes up matching.
        //
        // SortOrder is the order a contributor meets the types in — the type picker sorts on it
        // rather than on whatever order the read answered with. The gap before Series and Topic
        // mirrors the gap ContentType itself leaves (design §3.6): the two grouping types are
        // numbered apart from the standalone ones, and they are presented apart for the same
        // reason. Every row here states its value, so none falls back to the entity's 1000.
        private static IEnumerable<ContentItemSetting> BuildDefaultContentItemSettings()
        {
            yield return BuildSetting(
                ContentType.Quote,
                sortOrder: 0,
                contentTypeName: "Quote",
                contentTypeDescription: "Words that stirred you",
                contentTypeIconCssClass: "bi-quote",
                hasTitle: false,
                hasAuthor: true,
                isAvailableAsGeneralUserContribution: true,
                tagsAllowed: true,
                showTags: true,
                reactionsAllowed: true,
                showReactions: true,
                linksAllowed: false,
                showLinks: true,
                attachmentsAllowed: false,
                showAttachments: true,
                commentsAllowed: true,
                showComments: true,
                bibleReferenceAllowed: true,
                showBibleReferences: true,
                limitReactionsToLoveOnly: false);

            yield return BuildSetting(
                ContentType.Story,
                sortOrder: 1,
                contentTypeName: "Story",
                contentTypeDescription: "Something He did",
                contentTypeIconCssClass: "bi-journal-text",
                hasTitle: true,
                hasAuthor: true,
                isAvailableAsGeneralUserContribution: true,
                tagsAllowed: true,
                showTags: true,
                reactionsAllowed: true,
                showReactions: true,
                linksAllowed: false,
                showLinks: true,
                attachmentsAllowed: true,
                showAttachments: true,
                commentsAllowed: true,
                showComments: true,
                bibleReferenceAllowed: true,
                showBibleReferences: true,
                limitReactionsToLoveOnly: false);

            yield return BuildSetting(
                ContentType.Testimony,
                sortOrder: 2,
                contentTypeName: "Testimony",
                contentTypeDescription: "Your walk with Him",
                contentTypeIconCssClass: "bi-chat-heart",
                hasTitle: true,
                hasAuthor: false,
                isAvailableAsGeneralUserContribution: true,
                tagsAllowed: true,
                showTags: true,
                reactionsAllowed: true,
                showReactions: true,
                linksAllowed: false,
                showLinks: true,
                attachmentsAllowed: true,
                showAttachments: true,
                commentsAllowed: true,
                showComments: true,
                bibleReferenceAllowed: true,
                showBibleReferences: true,
                limitReactionsToLoveOnly: false);

            yield return BuildSetting(
                ContentType.Devotional,
                sortOrder: 3,
                contentTypeName: "Devotional",
                contentTypeDescription: "A daily encouragement",
                contentTypeIconCssClass: "bi-brightness-high",
                hasTitle: true,
                hasAuthor: true,
                isAvailableAsGeneralUserContribution: true,
                tagsAllowed: true,
                showTags: true,
                reactionsAllowed: true,
                showReactions: true,
                linksAllowed: false,
                showLinks: true,
                attachmentsAllowed: false,
                showAttachments: true,
                commentsAllowed: true,
                showComments: true,
                bibleReferenceAllowed: true,
                showBibleReferences: true,
                limitReactionsToLoveOnly: false);

            yield return BuildSetting(
                ContentType.BibleStudy,
                sortOrder: 4,
                contentTypeName: "Bible Study",
                contentTypeDescription: "Digging into the Word",
                contentTypeIconCssClass: "bi-book",
                hasTitle: true,
                hasAuthor: true,
                isAvailableAsGeneralUserContribution: true,
                tagsAllowed: true,
                showTags: true,
                reactionsAllowed: true,
                showReactions: true,
                linksAllowed: true,
                showLinks: true,
                attachmentsAllowed: true,
                showAttachments: true,
                commentsAllowed: true,
                showComments: true,
                bibleReferenceAllowed: true,
                showBibleReferences: true,
                limitReactionsToLoveOnly: false);

            yield return BuildSetting(
                ContentType.BlogPost,
                sortOrder: 5,
                contentTypeName: "Blog Post",
                contentTypeDescription: "Articles and reflections exploring Christian faith, " +
                    "biblical teachings, spiritual life.",
                contentTypeIconCssClass: "bi-pencil-square",
                hasTitle: true,
                hasAuthor: true,
                isAvailableAsGeneralUserContribution: false,
                tagsAllowed: true,
                showTags: true,
                reactionsAllowed: true,
                showReactions: true,
                linksAllowed: true,
                showLinks: true,
                attachmentsAllowed: true,
                showAttachments: true,
                commentsAllowed: true,
                showComments: true,
                bibleReferenceAllowed: true,
                showBibleReferences: true,
                limitReactionsToLoveOnly: false);

            yield return BuildSetting(
                ContentType.Series,
                sortOrder: 100,
                contentTypeName: "Series",
                contentTypeDescription: "A collection of quotes, stories, testimonies, articles, " +
                    "and reflections exploring a specific topic.",
                contentTypeIconCssClass: "bi-collection",
                hasTitle: true,
                hasAuthor: false,
                isAvailableAsGeneralUserContribution: false,
                tagsAllowed: true,
                showTags: true,
                reactionsAllowed: false,
                showReactions: false,
                linksAllowed: false,
                showLinks: false,
                attachmentsAllowed: false,
                showAttachments: false,
                commentsAllowed: false,
                showComments: false,
                bibleReferenceAllowed: false,
                showBibleReferences: false,
                limitReactionsToLoveOnly: false);

            yield return BuildSetting(
                ContentType.Topic,
                sortOrder: 200,
                contentTypeName: "Topic",
                contentTypeDescription: "A collection of content related to a specific topic.",
                contentTypeIconCssClass: "bi-compass",
                hasTitle: true,
                hasAuthor: false,
                isAvailableAsGeneralUserContribution: false,
                tagsAllowed: true,
                showTags: true,
                reactionsAllowed: false,
                showReactions: false,
                linksAllowed: false,
                showLinks: false,
                attachmentsAllowed: false,
                showAttachments: false,
                commentsAllowed: false,
                showComments: false,
                bibleReferenceAllowed: false,
                showBibleReferences: false,
                limitReactionsToLoveOnly: false);
        }

        private static ContentItemSetting BuildSetting(
            ContentType contentType,
            string contentTypeName,
            string contentTypeDescription,
            string contentTypeIconCssClass,
            int sortOrder,
            bool hasTitle,
            bool hasAuthor,
            bool isAvailableAsGeneralUserContribution,
            bool tagsAllowed,
            bool showTags,
            bool reactionsAllowed,
            bool showReactions,
            bool linksAllowed,
            bool showLinks,
            bool attachmentsAllowed,
            bool showAttachments,
            bool commentsAllowed,
            bool showComments,
            bool bibleReferenceAllowed,
            bool showBibleReferences,
            bool limitReactionsToLoveOnly)
        {
            DateTimeOffset seededWhen = DateTimeOffset.UtcNow;

            return new ContentItemSetting
            {
                Id = Guid.NewGuid(),
                ContentType = contentType,
                ContentItemId = null,
                ContentTypeName = contentTypeName,
                ContentTypeDescription = contentTypeDescription,
                ContentTypeIconCssClass = contentTypeIconCssClass,
                SortOrder = sortOrder,
                HasTitle = hasTitle,
                HasAuthor = hasAuthor,
                IsAvailableAsGeneralUserContribution = isAvailableAsGeneralUserContribution,
                TagsAllowed = tagsAllowed,
                ShowTags = showTags,
                ReactionsAllowed = reactionsAllowed,
                ShowReactions = showReactions,
                LinksAllowed = linksAllowed,
                ShowLinks = showLinks,
                AttachmentsAllowed = attachmentsAllowed,
                ShowAttachments = showAttachments,
                CommentsAllowed = commentsAllowed,
                ShowComments = showComments,
                BibleReferenceAllowed = bibleReferenceAllowed,
                ShowBibleReferences = showBibleReferences,
                LimitReactionsToLoveOnly = limitReactionsToLoveOnly,
                CreatedBy = SeededBy,
                CreatedWhen = seededWhen,
                UpdatedBy = SeededBy,
                UpdatedWhen = seededWhen,
                IsDeleted = false
            };
        }
    }
}
