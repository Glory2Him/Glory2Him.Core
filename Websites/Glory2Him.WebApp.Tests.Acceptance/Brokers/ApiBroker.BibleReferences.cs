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
using Glory2Him.WebApp.Tests.Acceptance.Models.BibleReferences;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string bibleReferencesRelativeUrl = "api/bibleReferences";

        public async ValueTask<BibleReference> PostBibleReferenceAsync(BibleReference bibleReference) =>
            await this.apiFactoryClient.PostContentAsync(bibleReferencesRelativeUrl, bibleReference);

        public async ValueTask<List<BibleReference>> GetAllBibleReferencesAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<BibleReference>>($"{bibleReferencesRelativeUrl}/");

        public async ValueTask<List<BibleReference>> GetSpecificBibleReferenceByIdAsync(Guid bibleReferenceId) =>
            await this.apiFactoryClient.GetContentAsync<List<BibleReference>>(
                $"{bibleReferencesRelativeUrl}?$filter=Id eq {bibleReferenceId}");

        public async ValueTask<BibleReference> GetBibleReferenceByIdAsync(Guid bibleReferenceId) =>
            await this.apiFactoryClient.GetContentAsync<BibleReference>($"{bibleReferencesRelativeUrl}/{bibleReferenceId}");

        public async ValueTask<BibleReference> DeleteBibleReferenceByIdAsync(Guid bibleReferenceId) =>
            await this.apiFactoryClient.DeleteContentAsync<BibleReference>($"{bibleReferencesRelativeUrl}/{bibleReferenceId}");

        public async ValueTask<BibleReference> HardDeleteBibleReferenceByIdAsync(Guid bibleReferenceId) =>
            await this.apiFactoryClient.DeleteContentAsync<BibleReference>($"{bibleReferencesRelativeUrl}/{bibleReferenceId}/hard");

        public async ValueTask<BibleReference> TransitionBibleReferenceApprovalAsync(BibleReference bibleReference) =>
            await this.apiFactoryClient.PostContentAsync($"{bibleReferencesRelativeUrl}/approve", bibleReference);

        public async ValueTask<BibleReference> SubmitBibleReferenceByIdAsync(Guid bibleReferenceId) =>
            await this.apiFactoryClient.PostContentAsync<object, BibleReference>(
                $"{bibleReferencesRelativeUrl}/{bibleReferenceId}/submit",
                content: new object());

        public async ValueTask<BibleReference> PutBibleReferenceAsync(BibleReference bibleReference) =>
            await this.apiFactoryClient.PutContentAsync(bibleReferencesRelativeUrl, bibleReference);
    }
}
