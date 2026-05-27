// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Collections;
using Xeptions;

namespace Glory2Him.Security.Client.Models.Clients.Audits.Exceptions
{
    public class AuditClientDependencyException : Xeption
    {
        public AuditClientDependencyException(string message, Xeption innerException, IDictionary data)
            : base(message, innerException, data)
        { }
    }
}