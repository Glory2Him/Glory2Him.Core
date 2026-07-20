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
