// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Collections;
using Xeptions;

namespace Glory2Him.Security.Client.Models.Clients.Audits.Exceptions
{
    public class AuditClientServiceException : Xeption
    {
        public AuditClientServiceException(string message, Exception innerException, IDictionary data)
            : base(message, innerException, data)
        { }
    }
}