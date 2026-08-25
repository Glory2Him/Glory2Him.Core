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
using Glory2Him.WebApp.Tests.Acceptance.Models.Links;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Links
{
    [Collection(nameof(ApiTestCollection))]
    public partial class LinkApiTests
    {
        private readonly ApiBroker apiBroker;

        public LinkApiTests(ApiBroker apiBroker)
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

        private static Link UpdateLinkWithRandomValues(Link inputLink)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var updatedLink = CreateRandomLink();
            updatedLink.Id = inputLink.Id;
            updatedLink.CreatedWhen = inputLink.CreatedWhen;
            updatedLink.CreatedBy = inputLink.CreatedBy;
            updatedLink.UpdatedWhen = now;
            updatedLink.IsDeleted = inputLink.IsDeleted;
            updatedLink.DeletionReason = inputLink.DeletionReason;
            updatedLink.GroupId = inputLink.GroupId;
            updatedLink.Version = inputLink.Version;
            updatedLink.IsPublished = inputLink.IsPublished;
            updatedLink.PublishDate = inputLink.PublishDate;
            updatedLink.ApprovalStatus = inputLink.ApprovalStatus;
            updatedLink.IsApprovedByBypass = inputLink.IsApprovedByBypass;
            updatedLink.ApprovedByBypassReason = inputLink.ApprovedByBypassReason;

            return updatedLink;
        }

        private async ValueTask<Link> PostRandomLinkAsync()
        {
            Link randomLink = CreateRandomLink();
            Link createdLink = await this.apiBroker.PostLinkAsync(randomLink);

            return createdLink;
        }

        private async ValueTask<List<Link>> PostRandomLinksAsync()
        {
            int randomNumber = GetRandomNumber();
            var randomLinks = new List<Link>();

            for (int i = 0; i < randomNumber; i++)
            {
                randomLinks.Add(await PostRandomLinkAsync());
            }

            return randomLinks;
        }

        private static Link CreateRandomLink() =>
            CreateRandomLinkFiller().Create();

        private static Filler<Link> CreateRandomLinkFiller()
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var filler = new Filler<Link>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(now)
                .OnType<DateTimeOffset?>().Use(now)

                // The caller-editable content. A new item must arrive unpublished and in Draft
                // with no bypass - the processing service rejects anything else.
                // The caller-editable content. A new link must arrive unpublished and in Draft
                // with no bypass - the processing service rejects anything else.
                .OnProperty(link => link.Name).Use(new Func<string>(GetRandomLinkName))
                .OnProperty(link => link.Url).Use(new Func<string>(GetRandomUrl))
                .OnProperty(link => link.LinkType).Use("External")

                // Control fields (12.4.2 business rule 6) - never accepted from a caller. Sent as
                // their defaults so a request cannot be read as an attempt to set them.
                //
                // Unlike the ContentItem fixture there is no ContentHash to zero: a link has no
                // content hash, because it has no duplicate-content rule to enforce.
                .OnProperty(link => link.GroupId).Use(Guid.Empty)
                .OnProperty(link => link.Version).Use(0)
                .OnProperty(link => link.IsPublished).Use(false)
                .OnProperty(link => link.PublishDate).Use((DateTimeOffset?)null)
                .OnProperty(link => link.ApprovalStatus).Use(ApprovalStatus.Draft)
                .OnProperty(link => link.IsApprovedByBypass).Use(false)
                .OnProperty(link => link.ApprovedByBypassReason).Use((string)null)
                .OnProperty(link => link.IsDeleted).Use(false)
                .OnProperty(link => link.DeletionReason).Use((string)null)
                .OnProperty(link => link.DeletedBy).Use((string)null)
                .OnProperty(link => link.DeletedWhen).Use((DateTimeOffset?)null)

                .OnProperty(link => link.CreatedWhen).Use(now)
                .OnProperty(link => link.CreatedBy).Use(user)
                .OnProperty(link => link.UpdatedWhen).Use(now)
                .OnProperty(link => link.UpdatedBy).Use(user);

            return filler;
        }

        private static string GetRandomLinkName() =>
            $"Acceptance link {Guid.NewGuid():N}";

        /// <summary>
        /// Distinct per link only so a failure names one row rather than several. Two links to
        /// the same URL are legitimate (§12.4.2) and nothing refuses them, so uniqueness here
        /// buys readability rather than correctness — the opposite of the ContentItem fixture,
        /// where distinct content is what keeps §3.4.2 from refusing the second write.
        /// </summary>
        private static string GetRandomUrl() =>
            $"https://example.org/{Guid.NewGuid():N}";
    }
}
