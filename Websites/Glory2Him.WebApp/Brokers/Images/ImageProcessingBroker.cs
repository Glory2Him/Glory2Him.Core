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

using SkiaSharp;

namespace Glory2Him.WebApp.Brokers.Images
{
    public sealed class ImageProcessingBroker : IImageProcessingBroker
    {
        // WebP quality: 80 is the sweet spot for small file size at high perceived quality, and
        // WebP carries alpha so it also covers images that need transparency.
        private const int WebpQuality = 80;
        private const string WebpContentType = "image/webp";

        public async ValueTask<ProcessedImage> CreateSquareAvatarAsync(
            Stream imageStream,
            int squareSize)
        {
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            byte[] sourceBytes = memoryStream.ToArray();

            SKBitmap? decoded;

            try
            {
                decoded = SKBitmap.Decode(sourceBytes);
            }
            catch
            {
                // SkiaSharp throws (rather than returning null) on unreadable/corrupt data.
                decoded = null;
            }

            using SKBitmap? source = decoded
                ?? throw new InvalidOperationException("The uploaded file is not a valid image.");

            using SKBitmap square = CropToSquare(source);

            var targetInfo = new SKImageInfo(squareSize, squareSize);
            var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);

            using SKBitmap resized = square.Resize(targetInfo, sampling)
                ?? throw new InvalidOperationException("The image could not be resized.");

            using SKImage image = SKImage.FromBitmap(resized);
            using SKData data = image.Encode(SKEncodedImageFormat.Webp, WebpQuality);

            return new ProcessedImage(data.ToArray(), WebpContentType);
        }

        // Center-crop to the largest square the image contains before resizing, so avatars are not
        // distorted by non-square source images.
        private static SKBitmap CropToSquare(SKBitmap source)
        {
            int side = Math.Min(source.Width, source.Height);
            int left = (source.Width - side) / 2;
            int top = (source.Height - side) / 2;

            var cropped = new SKBitmap(side, side);
            SKRectI cropRect = SKRectI.Create(left, top, side, side);

            if (source.ExtractSubset(cropped, cropRect))
            {
                return cropped;
            }

            cropped.Dispose();

            // ExtractSubset can fail on some colour types; fall back to a canvas copy.
            var fallback = new SKBitmap(side, side);
            using var canvas = new SKCanvas(fallback);
            canvas.DrawBitmap(source, SKRect.Create(left, top, side, side),
                SKRect.Create(0, 0, side, side));

            return fallback;
        }
    }
}
