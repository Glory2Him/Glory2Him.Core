// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using Xeptions;

namespace Glory2Him.Security.Client.Models.Foundations.Users.Exceptions
{
    internal class ClaimNotFoundUserException : Xeption
    {
        public ClaimNotFoundUserException(string message)
            : base(message)
        { }
    }
}