﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subs
{
    public interface ISubService
    {
        List<Sub> GetAllSubs(string searchText = null);

        List<Sub> GetDefaultSubs();

        List<Sub> GetSubscribedSubsForUser(string userName);

        Sub GetRandomSub();

        void InsertSub(Sub sub);

        void UpdateSub(Sub sub);

        void DeleteSub(Guid subId);

        void SubscribeToSub(string userName, string subName);

        void UnSubscribeToSub(string userName, string subName);

        Sub GetSubByName(string name);

        List<Sub> GetSubByNames(List<string> names);

        bool CanUserModerateSub(string userName, string subName);

        List<string> GetAllModsForSub(string subName);

        void AddModToSub(string userName, string subName, string addedBy = null);

        void RemoveModFromSub(string userName, string subName);
    }
}
