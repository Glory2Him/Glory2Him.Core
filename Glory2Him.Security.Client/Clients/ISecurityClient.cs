// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using Glory2Him.Security.Client.Clients.Audits;
using Glory2Him.Security.Client.Clients.Users;

namespace Glory2Him.Security.Client.Clients
{
    public interface ISecurityClient
    {
        IUserClient Users { get; }
        IAuditClient Audits { get; }
    }
}
