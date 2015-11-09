﻿using System;
using System.Web;
using Membership;
using Membership.Services;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Skimur.Utils;

namespace Skimur.Web
{
    public class UserContext : IUserContext
    {
        private readonly IMembershipService _membershipService;
        private readonly IAuthenticationManager _authenticationManager;
        private User _currentUser;

        public UserContext(IMembershipService membershipService, IAuthenticationManager authenticationManager)
        {
            _membershipService = membershipService;
            _authenticationManager = authenticationManager;
        }

        public User CurrentUser
        {
            get
            {
                if (_currentUser != null) return _currentUser;

                if (!HttpContext.Current.Request.IsAuthenticated)
                    return null;

                _currentUser = _membershipService.GetUserById(HttpContext.Current.User.Identity.GetUserId().ParseGuid());

                if (_currentUser == null)
                {
                    _authenticationManager.SignOut();
                    throw new Exception("Auth cookie exists for an invalid user. UserId=" + HttpContext.Current.User.Identity.GetUserId());
                }

                return _currentUser;
            }
        }

        public bool? CurrentNsfw
        {
            get
            {
                // anonymous users don't see NSFW content.
                // logged in users only see NSFW if preferences say so.
                // If they want to see NSFW, they will see all content (SFW/NSFW).
                return CurrentUser == null
                    ? false
                    : (CurrentUser.ShowNsfw ? (bool?) null : false);
            }
        }
    }
}
