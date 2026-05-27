// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;

namespace Glory2Him.Security.Client.Models.Clients
{
    public class SecurityConfigurations
    {
        public string CreatedByPropertyName { get; set; } = "CreatedBy";
        public Type CreatedByPropertyType { get; set; } = typeof(string);
        public string CreatedDatePropertyName { get; set; } = "CreatedDate";
        public Type CreatedDatePropertyType { get; set; } = typeof(DateTimeOffset);
        public string UpdatedByPropertyName { get; set; } = "UpdatedBy";
        public Type UpdatedByPropertyType { get; set; } = typeof(string);
        public string UpdatedDatePropertyName { get; set; } = "UpdatedDate";
        public Type UpdatedDatePropertyType { get; set; } = typeof(DateTimeOffset);
    }
}
