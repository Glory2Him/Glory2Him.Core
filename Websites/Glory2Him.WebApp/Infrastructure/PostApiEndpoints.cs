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

using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Services.Views.Posts;

namespace Glory2Him.WebApp.Infrastructure
{
    // Cookie-authenticated JSON endpoints consumed by the React SPA
    // (Glory2Him.WebApp.React). Public reads mirror the Blazor blog pages
    // (home, categories, tag, search); the create/update/delete operations
    // mirror the Administrators/Posts pages and are Administrators-only — the server
    // stays the authority, not the SPA.
    public static class PostApiEndpoints
    {
        public static IEndpointRouteBuilder MapPostApiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            RouteGroupBuilder postsGroup = endpoints.MapGroup("/api/posts");

            // Same filtering the Blazor pages perform client-side over the full list:
            // q matches title / excerpt / category (SearchResult), category and tag
            // match the category (Categories / Tag), author is a contains-match on
            // the author name (Search), and page / pageSize page the result.
            postsGroup.MapGet("/", async (
                string? q,
                string? category,
                string? tag,
                string? author,
                IPostsViewService postsViewService,
                int page = 1,
                int pageSize = 0) =>
            {
                List<PostView> posts = await postsViewService.RetrieveAllPostsAsync();

                IEnumerable<PostView> filtered = posts;

                if (!string.IsNullOrWhiteSpace(q))
                {
                    filtered = filtered.Where(post =>
                        post.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                            || post.Excerpt.Contains(q, StringComparison.OrdinalIgnoreCase)
                            || post.Category.Contains(q, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    filtered = filtered.Where(post =>
                        string.Equals(post.Category, category, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    filtered = filtered.Where(post =>
                        string.Equals(post.Category, tag, StringComparison.OrdinalIgnoreCase)
                            || post.Tags.Any(postTag =>
                                string.Equals(postTag, tag, StringComparison.OrdinalIgnoreCase)));
                }

                if (!string.IsNullOrWhiteSpace(author))
                {
                    filtered = filtered.Where(post =>
                        post.AuthorName.Contains(author.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                List<PostView> results = filtered.ToList();
                int totalCount = results.Count;

                if (page < 1)
                {
                    page = 1;
                }

                // pageSize omitted (or 0) returns the full result set, which is what
                // the Blazor pages consume for their own client-side slicing.
                int totalPages = pageSize > 0
                    ? Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize))
                    : 1;

                List<PostView> items = pageSize > 0
                    ? results.Skip((page - 1) * pageSize).Take(pageSize).ToList()
                    : results;

                return Results.Ok(new
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                });
            });

            // Mirrors PostSingle: an unknown slug falls back to the first post
            // inside the view service, so this never 404s — same as the Blazor page.
            postsGroup.MapGet("/slug/{slug}", async (
                string slug,
                IPostsViewService postsViewService) =>
            {
                PostView post = await postsViewService.RetrievePostBySlugAsync(slug);

                return Results.Ok(post);
            });

            postsGroup.MapGet("/{id}", async (
                string id,
                IPostsViewService postsViewService) =>
            {
                try
                {
                    PostView post = await postsViewService.RetrievePostByIdAsync(id);

                    return Results.Ok(post);
                }
                catch
                {
                    return Results.NotFound();
                }
            });

            postsGroup.MapPost("/", async (
                PostView post,
                IPostsViewService postsViewService) =>
            {
                PostView addedPost = await postsViewService.AddPostAsync(post);

                return Results.Ok(addedPost);
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrators"));

            postsGroup.MapPut("/{id}", async (
                string id,
                PostView post,
                IPostsViewService postsViewService) =>
            {
                post.Id = id;

                try
                {
                    PostView modifiedPost = await postsViewService.ModifyPostAsync(post);

                    return Results.Ok(modifiedPost);
                }
                catch
                {
                    return Results.NotFound();
                }
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrators"));

            postsGroup.MapDelete("/{id}", async (
                string id,
                IPostsViewService postsViewService) =>
            {
                try
                {
                    await postsViewService.RemovePostAsync(id);

                    return Results.Ok();
                }
                catch
                {
                    return Results.NotFound();
                }
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrators"));

            return endpoints;
        }
    }
}
