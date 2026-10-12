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

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.LiveUpdates;

namespace Glory2Him.Core.Services.Orchestrations.LiveUpdates
{
    internal partial class LiveUpdateOrchestrationService
    {
        // THE SETTINGS BODY, written once for every setting fact the forwarder hears
        // (LiveUpdateOrchestrationService.md, the settings body). It reads nothing first and names
        // no content item: every setting row is public-read (§SEC14.7 posture C rule 2), and the
        // page reads its settings again (LiveUpdates.md rule 5).
        private async ValueTask SendSettingsChangeAsync(CancellationToken cancellationToken)
        {
            await this.liveUpdateService.SendLiveUpdateAsync(
                new LiveUpdate
                {
                    Type = LiveUpdateType.ContentItemSettingChanged,
                },
                cancellationToken);
        }
    }
}
