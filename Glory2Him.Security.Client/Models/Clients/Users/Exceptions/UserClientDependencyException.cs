// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Collections;
using Xeptions;

namespace Glory2Him.Security.Client.Models.Clients.Users.Exceptions
{
    public class UserClientDependencyException : Xeption
    {
        public UserClientDependencyException(string message, Xeption innerException, IDictionary data)
            : base(message, innerException, data)
        { }
    }
}