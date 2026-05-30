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

namespace G2H.Security.Client.Models.Clients
{
    public class SecurityConfigurations
    {
        public string CreatedByPropertyName { get; set; } = "CreatedBy";
        public Type CreatedByPropertyType { get; set; } = typeof(string);
        public string CreatedDatePropertyName { get; set; } = "CreatedWhen";
        public Type CreatedDatePropertyType { get; set; } = typeof(DateTimeOffset);
        public string UpdatedByPropertyName { get; set; } = "UpdatedBy";
        public Type UpdatedByPropertyType { get; set; } = typeof(string);
        public string UpdatedDatePropertyName { get; set; } = "UpdatedWhen";
        public Type UpdatedDatePropertyType { get; set; } = typeof(DateTimeOffset);
        public string DeletedByPropertyName { get; set; } = "DeletedBy";
        public Type DeletedByPropertyType { get; set; } = typeof(string);
        public string DeletedDatePropertyName { get; set; } = "DeletedWhen";
        public Type DeletedDatePropertyType { get; set; } = typeof(DateTimeOffset);
    }
}
