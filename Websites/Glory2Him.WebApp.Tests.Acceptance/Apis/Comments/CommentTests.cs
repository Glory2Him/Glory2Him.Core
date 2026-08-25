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
using Glory2Him.WebApp.Tests.Acceptance.Models.Comments;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Comments
{
    [Collection(nameof(ApiTestCollection))]
    public partial class CommentApiTests
    {
        private readonly ApiBroker apiBroker;

        public CommentApiTests(ApiBroker apiBroker)
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

        private static Comment UpdateCommentWithRandomValues(Comment inputComment)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var updatedComment = CreateRandomComment();
            updatedComment.Id = inputComment.Id;
            updatedComment.CreatedWhen = inputComment.CreatedWhen;
            updatedComment.CreatedBy = inputComment.CreatedBy;
            updatedComment.UpdatedWhen = now;
            updatedComment.IsDeleted = inputComment.IsDeleted;
            updatedComment.DeletionReason = inputComment.DeletionReason;
            updatedComment.IsPublished = inputComment.IsPublished;
            updatedComment.PublishDate = inputComment.PublishDate;
            updatedComment.ApprovalStatus = inputComment.ApprovalStatus;
            updatedComment.IsApprovedByBypass = inputComment.IsApprovedByBypass;
            updatedComment.ApprovedByBypassReason = inputComment.ApprovedByBypassReason;

            return updatedComment;
        }

        private async ValueTask<Comment> PostRandomCommentAsync()
        {
            Comment randomComment = CreateRandomComment();
            Comment createdComment = await this.apiBroker.PostCommentAsync(randomComment);

            return createdComment;
        }

        private async ValueTask<List<Comment>> PostRandomCommentsAsync()
        {
            int randomNumber = GetRandomNumber();
            var randomComments = new List<Comment>();

            for (int i = 0; i < randomNumber; i++)
            {
                randomComments.Add(await PostRandomCommentAsync());
            }

            return randomComments;
        }

        private static Comment CreateRandomComment() =>
            CreateRandomCommentFiller().Create();

        private static Filler<Comment> CreateRandomCommentFiller()
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var filler = new Filler<Comment>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(now)
                .OnType<DateTimeOffset?>().Use(now)

                // Content is required and uncapped, so the filler's own string would do — it is
                // named here only so the value is recognisable in a failure message. A new
                // comment must arrive unpublished and in Draft with no bypass; the foundation
                // rejects anything else.
                .OnProperty(comment => comment.Content).Use(new Func<string>(GetRandomContent))
                .OnProperty(comment => comment.IsPublished).Use(false)
                .OnProperty(comment => comment.PublishDate).Use((DateTimeOffset?)null)
                .OnProperty(comment => comment.ApprovalStatus).Use(ApprovalStatus.Draft)
                .OnProperty(comment => comment.IsApprovedByBypass).Use(false)
                .OnProperty(comment => comment.ApprovedByBypassReason).Use((string)null)
                .OnProperty(comment => comment.IsDeleted).Use(false)
                .OnProperty(comment => comment.DeletionReason).Use((string)null)
                .OnProperty(comment => comment.DeletedBy).Use((string)null)
                .OnProperty(comment => comment.DeletedWhen).Use((DateTimeOffset?)null)

                .OnProperty(comment => comment.CreatedWhen).Use(now)
                .OnProperty(comment => comment.CreatedBy).Use(user)
                .OnProperty(comment => comment.UpdatedWhen).Use(now)
                .OnProperty(comment => comment.UpdatedBy).Use(user);

            return filler;
        }

        private static string GetRandomContent() =>
            $"Acceptance comment {Guid.NewGuid():N}";
    }
}
