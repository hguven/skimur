﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Membership;

namespace Subs.Services
{
    public interface ISubModerationService
    {
        bool CanUserModerateSub(Guid userId, Guid subId);

        List<Guid> GetAllModsForSub(Guid subId);

        void AddModToSub(Guid userId, Guid subId, Guid? addedBy = null);

        void RemoveModFromSub(Guid userId, Guid subId);

        List<Guid> GetSubsModeratoredByUser(Guid userId);

        ModeratorPermissions? GetUserPermissionsForSub(User user, Guid subId);
    }
}
