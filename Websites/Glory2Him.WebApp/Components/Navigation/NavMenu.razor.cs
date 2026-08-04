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

using Glory2Him.WebApp.Models.Views.Navigations;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.Navigation
{
    public partial class NavMenu : IDisposable
    {
        [Inject]
        public NavigationManager Navigation { get; set; } = default!;

        // Left null the menu follows the page being viewed; set it to pin the sidebar to a fixed
        // list.
        [Parameter]
        public IReadOnlyList<NavItem>? Items { get; set; }

        protected IReadOnlyList<NavItem> VisibleItems { get; private set; } =
            new List<NavItem>();

        protected override void OnInitialized()
        {
            Navigation.LocationChanged += OnLocationChanged;

            ResolveVisibleItems();
        }

        protected override void OnParametersSet() =>
            ResolveVisibleItems();

        private void ResolveVisibleItems() =>
            VisibleItems = Items ?? NavMenuProvider.GetNavMenu(
                Navigation.ToBaseRelativePath(Navigation.Uri));

        // Bootstrap needs a DOM id to collapse against, and it has to survive a re-render so an
        // open group stays open — derive it from the item's place in the tree rather than a Guid.
        private static string GetGroupId(NavItem item) =>
            "nav-group-" + Slugify(item.Href is { Length: > 0 } ? item.Href : item.Title);

        private static string Slugify(string value) =>
            new string(value
                .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
                .ToArray());

        // A group renders expanded when the page being viewed lives somewhere inside it, so landing
        // deep in the tree never leaves you looking at a collapsed menu.
        private bool IsBranchActive(NavItem item)
        {
            string currentPath = Navigation.ToBaseRelativePath(Navigation.Uri)
                .Split('?')[0]
                .Trim('/');

            return ContainsActiveLeaf(item, currentPath);
        }

        private static bool ContainsActiveLeaf(NavItem item, string currentPath)
        {
            if (item.HasChildren)
            {
                return item.Children!.Any(child => ContainsActiveLeaf(child, currentPath));
            }

            if (string.IsNullOrWhiteSpace(item.Href))
            {
                return false;
            }

            string href = item.Href.Trim('/');

            return item.ExactMatch
                ? string.Equals(currentPath, href, StringComparison.OrdinalIgnoreCase)
                : currentPath.StartsWith(href, StringComparison.OrdinalIgnoreCase);
        }

        private void OnLocationChanged(
            object? sender,
            Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args)
        {
            ResolveVisibleItems();

            StateHasChanged();
        }

        public void Dispose() =>
            Navigation.LocationChanged -= OnLocationChanged;
    }
}
