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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Glory2Him.WebApp.Brokers.Accounts;
using Glory2Him.WebApp.Brokers.Loggings;

namespace Glory2Him.WebApp.Services.Views.Registrations
{
    public partial class RegistrationViewService : IRegistrationViewService
    {
        private const int MinUsernameLength = 3;

        private readonly IAccountBroker accountBroker;
        private readonly ILoggingBroker loggingBroker;

        public RegistrationViewService(
            IAccountBroker accountBroker,
            ILoggingBroker loggingBroker)
        {
            this.accountBroker = accountBroker;
            this.loggingBroker = loggingBroker;
        }

        public int MinimumUsernameLength => MinUsernameLength;

        public ValueTask<bool> IsUsernameAvailableAsync(string userName) =>
            TryCatch(async () =>
            {
                if (string.IsNullOrWhiteSpace(userName)
                    || userName.Trim().Length < MinUsernameLength)
                {
                    return false;
                }

                return !await this.accountBroker.UsernameExistsAsync(userName.Trim());
            });

        public ValueTask<bool> IsEmailInUseAsync(string email) =>
            TryCatch(async () =>
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return false;
                }

                return await this.accountBroker.EmailExistsAsync(email.Trim());
            });

        public ValueTask<List<string>> SuggestUsernamesAsync(
            string name,
            string surname,
            string? preferredName,
            int count = 5) =>
            TryCatch(async () =>
            {
                var suggestions = new List<string>();

                foreach (string candidate in BuildCandidates(name, surname, preferredName))
                {
                    if (suggestions.Count >= count)
                    {
                        break;
                    }

                    if (candidate.Length < MinUsernameLength
                        || suggestions.Contains(candidate))
                    {
                        continue;
                    }

                    if (!await this.accountBroker.UsernameExistsAsync(candidate))
                    {
                        suggestions.Add(candidate);
                    }
                }

                return suggestions;
            });

        // Builds a stream of candidate usernames from the person's details: plain combinations
        // first (nicer), then numeric-suffixed variations for uniqueness.
        private static IEnumerable<string> BuildCandidates(
            string name,
            string surname,
            string? preferredName)
        {
            string cleanName = Clean(name);
            string cleanSurname = Clean(surname);
            string cleanPreferred = Clean(preferredName ?? string.Empty);

            var bases = new List<string>();

            void AddBase(string value)
            {
                if (!string.IsNullOrEmpty(value) && !bases.Contains(value))
                {
                    bases.Add(value);
                }
            }

            if (cleanName.Length > 0 && cleanSurname.Length > 0)
            {
                AddBase(cleanName + cleanSurname);
                AddBase($"{cleanName}.{cleanSurname}");
                AddBase(cleanName[0] + cleanSurname);
                AddBase(cleanName + cleanSurname[0]);
            }

            AddBase(cleanPreferred);
            AddBase(cleanName);

            // Plain bases first.
            foreach (string baseName in bases)
            {
                yield return baseName;
            }

            // Then deterministic numeric variations (year-like and small numbers) for uniqueness.
            int[] suffixes = { 1, 2, 7, 21, 99, 123, 2024 };

            foreach (int suffix in suffixes)
            {
                foreach (string baseName in bases)
                {
                    yield return baseName + suffix;
                }
            }
        }

        private static string Clean(string value) =>
            new string(value
                .Where(character => char.IsLetterOrDigit(character))
                .ToArray())
                .ToLowerInvariant();
    }
}
