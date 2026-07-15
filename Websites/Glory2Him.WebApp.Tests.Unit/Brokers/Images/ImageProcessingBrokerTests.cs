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

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Brokers.Images;
using SkiaSharp;

namespace Glory2Him.WebApp.Tests.Unit.Brokers.Images
{
    public class ImageProcessingBrokerTests
    {
        private readonly IImageProcessingBroker imageProcessingBroker;

        public ImageProcessingBrokerTests() =>
            this.imageProcessingBroker = new ImageProcessingBroker();

        private static byte[] CreatePng(int width, int height)
        {
            var info = new SKImageInfo(width, height);
            using var surface = SKSurface.Create(info);
            surface.Canvas.Clear(new SKColor(220, 40, 60));
            surface.Canvas.DrawCircle(width / 2f, height / 2f, width / 4f, new SKPaint { Color = SKColors.White });

            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            return data.ToArray();
        }

        [Fact]
        public async Task ShouldResizeToSquareWebp()
        {
            // given (a non-square source)
            byte[] sourcePng = CreatePng(width: 400, height: 300);
            using var stream = new MemoryStream(sourcePng);

            // when
            ProcessedImage result =
                await this.imageProcessingBroker.CreateSquareAvatarAsync(stream, squareSize: 256);

            // then
            result.ContentType.Should().Be("image/webp");
            result.Bytes.Should().NotBeNullOrEmpty();

            using SKBitmap decoded = SKBitmap.Decode(result.Bytes);
            decoded.Width.Should().Be(256);
            decoded.Height.Should().Be(256);
        }

        [Fact]
        public async Task ShouldProduceSmallFileForAvatar()
        {
            // given
            byte[] sourcePng = CreatePng(width: 1024, height: 1024);
            using var stream = new MemoryStream(sourcePng);

            // when
            ProcessedImage result =
                await this.imageProcessingBroker.CreateSquareAvatarAsync(stream, squareSize: 256);

            // then (a 256x256 WebP avatar should be well under 100 KB)
            result.Bytes.Length.Should().BeLessThan(100 * 1024);
        }

        [Fact]
        public async Task ShouldThrowWhenStreamIsNotAnImage()
        {
            // given
            using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

            // when
            Func<Task> processTask = async () =>
                await this.imageProcessingBroker.CreateSquareAvatarAsync(stream, squareSize: 256);

            // then
            await processTask.Should().ThrowAsync<System.InvalidOperationException>();
        }
    }
}
