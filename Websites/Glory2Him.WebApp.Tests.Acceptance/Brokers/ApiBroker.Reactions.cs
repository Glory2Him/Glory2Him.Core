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
using Glory2Him.WebApp.Tests.Acceptance.Models.Reactions;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string reactionsRelativeUrl = "api/reactions";

        public async ValueTask<Reaction> PostReactionAsync(Reaction reaction) =>
            await this.apiFactoryClient.PostContentAsync(reactionsRelativeUrl, reaction);

        public async ValueTask<List<Reaction>> GetAllReactionsAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<Reaction>>($"{reactionsRelativeUrl}/");

        public async ValueTask<List<Reaction>> GetSpecificReactionByIdAsync(Guid reactionId) =>
            await this.apiFactoryClient.GetContentAsync<List<Reaction>>(
                $"{reactionsRelativeUrl}?$filter=Id eq {reactionId}");

        public async ValueTask<Reaction> GetReactionByIdAsync(Guid reactionId) =>
            await this.apiFactoryClient.GetContentAsync<Reaction>($"{reactionsRelativeUrl}/{reactionId}");

        public async ValueTask<Reaction> DeleteReactionByIdAsync(Guid reactionId) =>
            await this.apiFactoryClient.DeleteContentAsync<Reaction>($"{reactionsRelativeUrl}/{reactionId}");

        public async ValueTask<Reaction> HardDeleteReactionByIdAsync(Guid reactionId) =>
            await this.apiFactoryClient.DeleteContentAsync<Reaction>($"{reactionsRelativeUrl}/{reactionId}/hard");

        public async ValueTask<Reaction> TransitionReactionApprovalAsync(Reaction reaction) =>
            await this.apiFactoryClient.PostContentAsync($"{reactionsRelativeUrl}/approve", reaction);

        public async ValueTask<Reaction> SubmitReactionByIdAsync(Guid reactionId) =>
            await this.apiFactoryClient.PostContentAsync<object, Reaction>(
                $"{reactionsRelativeUrl}/{reactionId}/submit",
                content: new object());

        public async ValueTask<Reaction> PutReactionAsync(Reaction reaction) =>
            await this.apiFactoryClient.PutContentAsync(reactionsRelativeUrl, reaction);
    }
}
