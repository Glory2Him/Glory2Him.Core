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

namespace Glory2Him.WebApp.Models.Foundations.Users
{
    /// <summary>
    /// The single place the "a username is not an email address" rule is spelled (design §18.3.1).
    /// A username may not contain '@' at all - not merely "may not equal this account's own
    /// email", because the narrow form still lets somebody register with a colleague's address
    /// and leak it just as effectively.
    ///
    /// <para>The rule exists because every display-name composition in the system ends
    /// PreferredName, then "Name Surname", then the username - so an account that has set no
    /// personal details publishes its username wherever the site names who submitted or reviewed
    /// something. Those fallbacks stay as they are; this rule is what makes them safe.</para>
    ///
    /// <para>Two enforcement layers read from here so they cannot drift: the services that mint a
    /// username call <see cref="IsAllowed"/> for a message a person can act on, and Identity's own
    /// <c>User.AllowedUserNameCharacters</c> is narrowed by <see cref="WithoutProhibitedCharacter"/>
    /// so nothing can write one past them.</para>
    /// </summary>
    public static class UserNameRule
    {
        public const char ProhibitedCharacter = '@';

        public const string RejectionMessage =
            "A username may not contain \"@\". Usernames and email addresses are separate values: "
                + "your username is shown to other people wherever the site names who submitted or "
                + "reviewed something, so an email address used as one becomes public.";

        // Null and empty are somebody else's answer - required-ness and minimum length are checked
        // where they belong. This method answers one question only.
        public static bool IsAllowed(string? userName) =>
            string.IsNullOrEmpty(userName)
                || userName.Contains(ProhibitedCharacter) is false;

        // Takes Identity's own default list and removes the one character, rather than restating
        // the whole set here - a framework update that adds a character keeps it, and this file
        // never becomes a stale copy of a list it does not own.
        public static string WithoutProhibitedCharacter(string allowedCharacters) =>
            string.IsNullOrEmpty(allowedCharacters)
                ? allowedCharacters
                : allowedCharacters.Replace(ProhibitedCharacter.ToString(), string.Empty);
    }
}
