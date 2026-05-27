// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Collections;
using Xeptions;

namespace Glory2Him.Security.Client.Models.Clients.Users.Exceptions
{
    public class UserClientServiceException : Xeption
    {
        public UserClientServiceException(string message, Exception innerException, IDictionary data)
            : base(message, innerException, data)
        { }
    }
}