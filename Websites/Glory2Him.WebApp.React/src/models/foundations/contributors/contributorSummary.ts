// Wire shape of GET /api/contributors/{userId} — the public byline identity behind a
// ContentItem's CreatedBy, and NOTHING ELSE. The endpoint is anonymous, so the two members here
// are the whole of what it may carry: everything wider on the account (email, phone, roles,
// lockout) belongs to /api/admin/users and stays there.
export type ContributorSummary = {
    // The account id, echoed back. The same value ContentItem.CreatedBy holds, so a caller can
    // key a cache on it without keeping the request around.
    userId: string;

    // The friendly name AppUser composes: preferred name, else "Name Surname", else the username.
    // Never empty — the fallback chain ends at a value every account has.
    displayName: string;

    // Relative url of the stored avatar, carrying a content hash so the browser re-fetches it the
    // moment it changes. NULL when no image is set, which is the Avatar component's cue to draw
    // its deterministic initials circle instead.
    imageUrl: string | null;
};
