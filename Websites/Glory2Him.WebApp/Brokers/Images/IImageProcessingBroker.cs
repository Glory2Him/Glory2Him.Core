// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using System.IO;
using System.Threading.Tasks;

namespace Glory2Him.WebApp.Brokers.Images
{
    // SkiaSharp is an external image library, so it is wrapped by this broker. Services depend on
    // the broker, never on SkiaSharp directly.
    public interface IImageProcessingBroker
    {
        // Center-crops the source image to a square, resizes it to squareSize x squareSize, and
        // encodes it as WebP. Returns the encoded bytes + content type.
        ValueTask<ProcessedImage> CreateSquareAvatarAsync(Stream imageStream, int squareSize);
    }
}
