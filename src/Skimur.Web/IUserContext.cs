﻿using Membership;

namespace Skimur.Web
{
    public interface IUserContext
    {
        User CurrentUser { get; }
    }
}
