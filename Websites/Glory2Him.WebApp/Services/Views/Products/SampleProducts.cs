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

using System.Collections.Generic;
using Glory2Him.WebApp.Models.Views.Products;

namespace Glory2Him.WebApp.Services.Views.Products
{
    // Static sample shop catalogue mirroring the Blogzine shop demo. Placeholder content until a
    // real product domain replaces it.
    internal static class SampleProducts
    {
        public static readonly List<ProductView> All = new()
        {
            new ProductView
            {
                Id = "1",
                Name = "Study Bible (Hardcover)",
                Slug = "study-bible-hardcover",
                Description = "A durable hardcover study Bible with book introductions, cross "
                    + "references, and study notes to help you dig deeper into God's Word.",
                ImageUrl = "assets/images/shop/01.png",
                Price = 34.00m,
                Rating = 4.5,
                Badge = "New Arrival",
                BadgeCss = "text-bg-success",
            },
            new ProductView
            {
                Id = "2",
                Name = "Daily Devotional Journal",
                Slug = "daily-devotional-journal",
                Description = "A guided journal with space for reflection, prayer requests, and "
                    + "a verse of the day to keep you anchored throughout the week.",
                ImageUrl = "assets/images/shop/02.png",
                Price = 18.50m,
                Rating = 5.0,
                Badge = "Popular",
                BadgeCss = "text-bg-warning",
            },
            new ProductView
            {
                Id = "3",
                Name = "Scripture Wall Art Set",
                Slug = "scripture-wall-art-set",
                Description = "A set of framed prints featuring encouraging verses, ready to hang "
                    + "in your home as a daily reminder of hope.",
                ImageUrl = "assets/images/shop/03.png",
                Price = 42.00m,
                Rating = 4.0,
            },
            new ProductView
            {
                Id = "4",
                Name = "Worship Playlist Vinyl",
                Slug = "worship-playlist-vinyl",
                Description = "A collection of worship songs pressed on vinyl — perfect for a "
                    + "restful evening of praise.",
                ImageUrl = "assets/images/shop/04.png",
                Price = 27.00m,
                Rating = 4.5,
            },
            new ProductView
            {
                Id = "5",
                Name = "Gospel Tote Bag",
                Slug = "gospel-tote-bag",
                Description = "A sturdy canvas tote printed with Mark 16:15 — carry the good news "
                    + "wherever you go.",
                ImageUrl = "assets/images/shop/05.png",
                Price = 12.00m,
                Rating = 4.5,
                Badge = "Sale",
                BadgeCss = "text-bg-danger",
            },
            new ProductView
            {
                Id = "6",
                Name = "Prayer Cards Deck",
                Slug = "prayer-cards-deck",
                Description = "A deck of prayer prompt cards to encourage a consistent, "
                    + "Scripture-shaped prayer life.",
                ImageUrl = "assets/images/shop/06.png",
                Price = 9.50m,
                Rating = 5.0,
            },
        };
    }
}
